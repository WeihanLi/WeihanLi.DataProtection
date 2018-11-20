using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeihanLi.DataProtection
{
    public class ParamsProtectorJsonInputFormatter : TextInputFormatter
    {
        public ParamsProtectorJsonInputFormatter()
        {
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("text/json");
            SupportedMediaTypes.Add("application/*+json");
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context,
            Encoding encoding)
        {
            var request = context.HttpContext.Request;
            var serviceProvider = context.HttpContext.RequestServices;
            var option = serviceProvider.GetRequiredService<IOptions<ParamsProtectionOptions>>().Value;
            var serializerSettings = serviceProvider.GetService<JsonSerializerSettings>();
            using (var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8))
            {
                var content = await reader.ReadToEndAsync();
                if (option.Enabled && option.ProtectParams.Length > 0)
                {
                    var protector =
                        context.HttpContext.RequestServices.GetDataProtector(
                            option.ProtectorPurpose ?? ProtectorHelper.DefaultPurpose);
                    if (option.ExpiresIn.GetValueOrDefault(0) > 0)
                    {
                        protector = protector.ToTimeLimitedDataProtector();
                    }
                    var obj = JsonConvert.DeserializeObject<JToken>(content, serializerSettings);
                    try
                    {
                        ProtectorHelper.UnprotectParams(obj, protector, option);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);

                        context.HttpContext.Response.StatusCode = option.InvalidRequestStatusCode;
                        context.HttpContext.RequestAborted = new CancellationToken(true);
                    }

                    content = JsonConvert.SerializeObject(obj);
                }
                return await InputFormatterResult.SuccessAsync(JsonConvert.DeserializeObject(content, context.ModelType));
            }
        }
    }

    public class ParamsProtectorResourceFilter : IResourceFilter
    {
        private readonly IDataProtector _protector;
        private readonly ParamsProtectionOptions _option;

        private readonly ILogger _logger;

        public ParamsProtectorResourceFilter(IDataProtectionProvider protectionProvider, ILogger<ParamsProtectorResourceFilter> logger, IOptions<ParamsProtectionOptions> options)
        {
            _option = options.Value;
            _protector = protectionProvider.CreateProtector(_option.ProtectorPurpose ?? ProtectorHelper.DefaultPurpose);
            if (_option.ExpiresIn.GetValueOrDefault(0) > 0)
            {
                _protector = _protector.ToTimeLimitedDataProtector();
            }
            _logger = logger;
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (_option.Enabled && _option.ProtectParams.Length > 0)
            {
                var queryDic = context.HttpContext.Request.Query.ToDictionary(query => query.Key, query => query.Value);

                foreach (var param in _option.ProtectParams)
                {
                    if (context.RouteData?.Values != null)
                    {
                        if (context.RouteData.Values.ContainsKey(param))
                        {
                            if (_protector.TryGetUnprotectedValue(_option, context.RouteData.Values[param].ToString(), out var val))
                            {
                                context.RouteData.Values[param] = val;
                            }
                            else
                            {
                                _logger.LogWarning($"Error in unprotect routeValue:{param}");

                                context.Result = new StatusCodeResult(_option.InvalidRequestStatusCode);

                                return;
                            }
                        }
                    }
                    if (queryDic.ContainsKey(param))
                    {
                        var vals = new List<string>();
                        for (var i = 0; i < queryDic[param].Count; i++)
                        {
                            if (_protector.TryGetUnprotectedValue(_option, queryDic[param][i], out var val))
                            {
                                vals.Add(val);
                            }
                            else
                            {
                                _logger.LogWarning($"Error in unprotect query value: param:{param}");
                                context.Result = new StatusCodeResult(_option.InvalidRequestStatusCode);

                                return;
                            }
                        }
                        queryDic[param] = new StringValues(vals.ToArray());
                    }
                }

                context.HttpContext.Request.Query = new QueryCollection(queryDic);
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }

    public class ParamsProtectorResultFilter : IResultFilter
    {
        private readonly IDataProtector _protector;
        private readonly ParamsProtectionOptions _option;

        public ParamsProtectorResultFilter(IDataProtectionProvider protectionProvider, IOptions<ParamsProtectionOptions> options)
        {
            _option = options.Value;

            _protector = protectionProvider.CreateProtector(_option.ProtectorPurpose ?? ProtectorHelper.DefaultPurpose);

            if (_option.ExpiresIn.GetValueOrDefault(0) > 0)
            {
                _protector = _protector.ToTimeLimitedDataProtector();
            }
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (_option.Enabled && _option.ProtectParams.Length > 0 && context.Result is OkObjectResult result && result.Value != null)
            {
                var obj = JsonConvert.DeserializeObject<JToken>(JsonConvert.SerializeObject(result.Value));
                ProtectorHelper.ProtectParams(obj, _protector, _option);
                result.Value = obj;
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }

    internal static class ProtectorHelper
    {
        public const string DefaultPurpose = "ParamsProtector";

        private static void ProtectParams(JToken token, ITimeLimitedDataProtector protector, ParamsProtectionOptions option)
        {
            if (token is JArray array)
            {
                foreach (var j in array)
                {
                    if (j is JValue val)
                    {
                        var strJ = val.Value.ToString();
                        if (option.NeedProtectFunc(strJ))
                        {
                            val.Value = protector.Protect(strJ, TimeSpan.FromMinutes(option.ExpiresIn.GetValueOrDefault(10)));
                        }
                    }
                    else
                    {
                        ProtectParams(j, protector, option);
                    }
                }
            }

            if (token is JObject obj)
            {
                foreach (var property in obj.Children<JProperty>())
                {
                    var val = property.Value.ToString();
                    if (option.ProtectParams.Any(p => p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) && option.NeedProtectFunc(val))
                    {
                        property.Value = protector.Protect(val, TimeSpan.FromMinutes(option.ExpiresIn.GetValueOrDefault(10)));
                    }
                    else
                    {
                        if (property.Value.HasValues)
                        {
                            ProtectParams(property.Value, protector, option);
                        }
                    }
                }
            }
        }

        public static bool TryGetUnprotectedValue(this IDataProtector protector, ParamsProtectionOptions option,
            string value, out string unprotectedValue)
        {
            if (option.AllowUnprotectedParams && option.NeedProtectFunc(value))
            {
                unprotectedValue = value;
            }
            else
            {
                try
                {
                    unprotectedValue = protector.Unprotect(value);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e, $"Error in unprotect value:{value}");
                    unprotectedValue = "";
                    return false;
                }
            }
            return true;
        }

        public static void ProtectParams(JToken token, IDataProtector protector, ParamsProtectionOptions option)
        {
            if (option.Enabled && option.ProtectParams?.Length > 0)
            {
                if (protector is ITimeLimitedDataProtector timeLimitedDataProtector)
                {
                    ProtectParams(token, timeLimitedDataProtector, option);
                    return;
                }

                if (token is JArray array)
                {
                    foreach (var j in array)
                    {
                        if (j is JValue val)
                        {
                            var strJ = val.Value.ToString();
                            if (option.NeedProtectFunc(strJ))
                            {
                                val.Value = protector.Protect(strJ);
                            }
                        }
                        else
                        {
                            ProtectParams(j, protector, option);
                        }
                    }
                }

                if (token is JObject obj)
                {
                    foreach (var property in obj.Children<JProperty>())
                    {
                        var val = property.Value.ToString();
                        if (option.ProtectParams.Any(p =>
                                p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) &&
                            option.NeedProtectFunc(val))
                        {
                            property.Value = protector.Protect(val);
                        }
                        else
                        {
                            if (property.Value.HasValues)
                            {
                                ProtectParams(property.Value, protector, option);
                            }
                        }
                    }
                }
            }
        }

        public static void UnprotectParams(JToken token, IDataProtector protector, ParamsProtectionOptions option)
        {
            if (option.Enabled && option.ProtectParams?.Length > 0)
            {
                if (token is JArray array)
                {
                    foreach (var j in array)
                    {
                        if (j is JValue val)
                        {
                            var strJ = val.Value.ToString();
                            if (!option.NeedProtectFunc(strJ))
                            {
                                try
                                {
                                    val.Value = protector.Unprotect(strJ);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e);
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            UnprotectParams(j, protector, option);
                        }
                    }
                }

                if (token is JObject obj)
                {
                    foreach (var property in obj.Children<JProperty>())
                    {
                        if (property.Value is JArray)
                        {
                            UnprotectParams(property.Value, protector, option);
                        }
                        else
                        {
                            var val = property.Value.ToString();
                            if (option.ProtectParams.Any(p =>
                                    p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) &&
                                !option.NeedProtectFunc(val))
                            {
                                try
                                {
                                    property.Value = protector.Unprotect(val);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e);
                                    throw;
                                }
                            }
                            else
                            {
                                if (property.Value.HasValues)
                                {
                                    UnprotectParams(property.Value, protector, option);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
