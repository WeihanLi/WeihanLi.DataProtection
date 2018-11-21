using System;

namespace WeihanLi.DataProtection
{
    public class ParamsProtectionOptions
    {
        private string[] _protectParams = new string[0];
        
        /// <summary>
        /// ProtectorPurpose
        /// </summary>
        public string ProtectorPurpose { get; set; } = "ParamsProtection";

        /// <summary>
        /// ExpiresIn, minutes
        /// </summary>
        public int? ExpiresIn { get; set; }

        /// <summary>
        /// Enabled for paramsProtection
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Allow unprotected params
        /// </summary>
        public bool AllowUnprotectedParams { get; set; }

        /// <summary>
        /// Invalid request response http status code
        /// refer to https://restfulapi.net/http-status-codes/
        /// </summary>
        public int InvalidRequestStatusCode { get; set; } = 412;

        /// <summary>
        /// the params to protect
        /// </summary>
        public string[] ProtectParams
        {
            get => _protectParams;
            set
            {
                if (value != null)
                {
                    _protectParams = value;
                }
            }
        }

        /// <summary>
        /// the parameter whether should be protected condition
        /// </summary>
        public Func<string, bool> NeedProtectFunc { get; set; } = str => true;
    }
}
