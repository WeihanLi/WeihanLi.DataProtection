using System;

namespace WeihanLi.DataProtection
{
    public class ParamsProtectionOptions
    {
        public string ProtectorPurpose { get; set; } = "ParamsProtector";

        /// <summary>
        /// ExpiresIn, minutes
        /// </summary>
        public int? ExpiresIn { get; set; }

        public int InvalidRequestStatusCode { get; set; } = 400;

        public string[] ProtectParams { get; set; } = new string[0];

        public Func<string, bool> NeedProtectFunc { get; set; } = str => long.TryParse(str, out _);
    }
}
