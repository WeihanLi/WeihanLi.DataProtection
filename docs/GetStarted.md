# GetStarted

install the package `WeihanLi.DataProtection` in your asp.net core project, register `DataProtection` service in your `startUp` file as follows:

``` csharp
services.AddDataProtection()
            .AddParamsProtection(options =>
            {
                options.ProtectParams = new[]
                {
                    "id"
                };

                // options.AddProtectValue<JsonResult>(r => r.Value); // uncomment to proetct JsonResult value
            });
```

you can do these things by change the option value:

- disable param protection by setting the `Enabled` property value to `false`
- change the protector purpose by change the `ProtectorPurpose` property value
- allow the origin param by setting the `AllowUnprotectedParams` to `true`
- change the status code when the params are not allowed to access
- change the protected params expiry(in minutes) by setting the `ExpiresIn` property value
- set the params to be protected by setting the `ProtectParams`property value(non caseSensitive)
