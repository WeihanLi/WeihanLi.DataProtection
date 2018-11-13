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
    public class ParamsProtectorInputFormatter : TextInputFormatter
    {
        public ParamsProtectorInputFormatter()
        {
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("text/json");
            SupportedMediaTypes.Add("application/*+json");
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var option = context.HttpContext.RequestServices.GetRequiredService<IOptions<ParamsProtectionOptions>>().Value;

            var protector = context.HttpContext.RequestServices.GetDataProtector(option.ProtectorPurpose ?? ProtectorHelper.DefaultPurpose)
                .ToTimeLimitedDataProtector();

            var request = context.HttpContext.Request;
            using (var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8))
            {
                var content = await reader.ReadToEndAsync();
                var obj = JsonConvert.DeserializeObject<JObject>(content);

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

                var requestModel = JsonConvert.DeserializeObject(content, context.ModelType);

                return await InputFormatterResult.SuccessAsync(requestModel);
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
            var queryDic = context.HttpContext.Request.Query.ToDictionary(query => query.Key, query => query.Value);

            foreach (var param in _option.ProtectParams)
            {
                if (context.RouteData.Values.ContainsKey(param))
                {
                    try
                    {
                        context.RouteData.Values[param] = _protector.Unprotect(context.RouteData.Values[param].ToString());
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Error in unprotect routeValue:{param}");

                        context.Result = new BadRequestResult();

                        return;
                    }
                }

                if (queryDic.ContainsKey(param))
                {
                    var vals = new List<string>();
                    for (int i = 0; i < queryDic[param].Count; i++)
                    {
                        try
                        {
                            vals.Add(_protector.Unprotect(queryDic[param][i]));
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, $"Error in unprotect query value:{param}");

                            context.Result = new BadRequestResult();

                            return;
                        }
                    }
                    queryDic[param] = new StringValues(vals.ToArray());
                }
            }

            context.HttpContext.Request.Query = new QueryCollection(queryDic);
            //
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
            if (context.Result is OkObjectResult result)
            {
                var strObj = JsonConvert.SerializeObject(result.Value);
                //
                var obj = JsonConvert.DeserializeObject<JToken>(strObj);
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

        public static void ProtectParams(JToken token, ITimeLimitedDataProtector protector, ParamsProtectionOptions option)
        {
            if (token is JArray array)
            {
                foreach (var j in array)
                {
                    if (j is JValue val)
                    {
                        var strJ = val.Value.ToString();
                        if (long.TryParse(strJ, out _))
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
                    if (option.ProtectParams.Any(p => p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) && long.TryParse(val, out _))
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

        public static void ProtectParams(JToken token, IDataProtector protector, ParamsProtectionOptions option)
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
                    if (option.ProtectParams.Any(p => p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) && option.NeedProtectFunc(val))
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

        public static void UnprotectParams(JToken token, IDataProtector protector, ParamsProtectionOptions option)
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
                        if (option.ProtectParams.Any(p => p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)) && !option.NeedProtectFunc(val))
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
