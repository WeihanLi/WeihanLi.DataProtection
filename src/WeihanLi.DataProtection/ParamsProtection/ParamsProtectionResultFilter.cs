using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeihanLi.DataProtection.ParamsProtection
{
    public class ParamsProtectionResultFilter : IResultFilter
    {
        private readonly IDataProtector _protector;
        private readonly ParamsProtectionOptions _option;

        public ParamsProtectionResultFilter(IDataProtectionProvider protectionProvider, IOptions<ParamsProtectionOptions> options)
        {
            _option = options.Value;

            _protector = protectionProvider.CreateProtector(_option.ProtectorPurpose ?? ParamsProtectionHelper.DefaultPurpose);

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
                ParamsProtectionHelper.ProtectParams(obj, _protector, _option);
                result.Value = obj;
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
