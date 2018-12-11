using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeihanLi.DataProtection.ParamsProtection
{
    public class ParamsProtectionResultFilter : IResultFilter
    {
        private readonly IDataProtector _protector;
        private readonly ParamsProtectionOptions _option;
        private readonly ILogger _logger;

        public ParamsProtectionResultFilter(IDataProtectionProvider protectionProvider, IOptions<ParamsProtectionOptions> options, ILogger<ParamsProtectionResultFilter> logger)
        {
            _logger = logger;
            _option = options.Value;

            _protector = protectionProvider.CreateProtector(_option.ProtectorPurpose ?? ParamsProtectionHelper.DefaultPurpose);

            if (_option.ExpiresIn.GetValueOrDefault(0) > 0)
            {
                _protector = _protector.ToTimeLimitedDataProtector();
            }
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (_option.Enabled && _option.ProtectParams.Length > 0)
            {
                foreach (var pair in _option.NeedProtectResponseValues)
                {
                    if (pair.Key.IsInstanceOfType(context.Result))
                    {
                        var prop = pair.Key.GetProperty(pair.Value);
                        var val = prop?.GetValue(context.Result);
                        if (val != null)
                        {
                            var obj = JsonConvert.DeserializeObject<JToken>(JsonConvert.SerializeObject(val));
                            ParamsProtectionHelper.ProtectParams(obj, _protector, _option);

                            prop.SetValue(context.Result, obj);
                        }
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
