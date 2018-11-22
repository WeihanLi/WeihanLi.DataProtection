# WeihanLi.DataProtection [![WeihanLi.DataProtection](https://img.shields.io/nuget/v/WeihanLi.DataProtection.svg)](https://www.nuget.org/packages/WeihanLi.DataProtection/)

## Intro

asp.net core data protection extensions

## Install

install the package `WeihanLi.DataProtection`

## ParamsProtection

`ParamsProtection` is designed to protect the response specific param info for asp.net core web api projects, and it can be used for anti-network-spider.

### GetStarted

Look at the sample for more details.

``` csharp
services.AddDataProtection()
            .AddParamsProtection(options =>
            {
                options.ProtectParams = new[]
                {
                    "id"
                };
            });
```

run the [sample](https://github.com/WeihanLi/WeihanLi.DataProtection/blob/master/samples/DataProtectionSample), and access `/api/values` path, you will get something like this

``` json
[
  {
    "id": "CfDJ8MvS3iyCJCJCrNda10tFrJu_HXavFbumMGxov9ly0XkFRG6O-HxgLwoqTnc4GQ27Zpby4kNOZBNlNK-1ctAWfuuBkkfoG96szEHXixZvUl6b2JlV1yt1MVUq5MHSOeYOGw",
    "val": "value1"
  },
  {
    "id": "CfDJ8MvS3iyCJCJCrNda10tFrJv9haZxFcv9bx2V3ZUKAMxGVD5aQzdzHfqB3XPfpZvQfzPHqxacA2i--hVnXAqzIBJ9ytQ72alekFFqzSFHjZwOTVwr4SMwOlfqm1zkMqFSUg",
    "val": "value2"
  }
]
```

while in my code it returns

``` csharp
return Ok(new[] {
            new
            {
                id = 1,
                val = "value1"
            },
            new
            {
                id =2,
                val ="value2"
            } });
```

because I've set the "id" param should be propected, and when you access the `/api/values/{id}` path use integer id directly you will get a 4xx (412 by default) response if `AllowUnprotectedParams` is false,
while if you use the id returned from the result from `api/values`,it will return the result succssfully, and you can even set expiresIn when the protected value will be expired.

You can also use the protected values in your post or put request, if the protected values are expired, you will get a 4xx(412 by default) response.

### More

There are some other options for you, look at the `ParamsProtectionOptions` file for details:

``` csharp
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
public string[] ProtectParams { get;set; }
```

you can do these things by change the option value:

- disable param protection by setting the `Enabled` property value to `false`
- change the protector purpose by change the `ProtectorPurpose` property value
- allow the origin param by setting the `AllowUnprotectedParams` to `true`
- change the status code when the params are not allowed to access
- change the protected params expiry(in minutes) by setting the `ExpiresIn` property value
- set the params to be protected by setting the `ProtectParams`property value

## Contact

Contact me: <weihanli@outlook.com>