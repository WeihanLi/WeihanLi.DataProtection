using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json.Linq;

namespace WeihanLi.DataProtection.ParamsProtection
{
    internal static class ParamsProtectionHelper
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
                            if (array.Parent.Root is JProperty property && option.ProtectParams.Any(p =>
                                    p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
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
