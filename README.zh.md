# WeihanLi.DataProtection [![WeihanLi.DataProtection](https://img.shields.io/nuget/v/WeihanLi.DataProtection.svg)](https://www.nuget.org/packages/WeihanLi.DataProtection/)

## Intro

asp.net core data protection 扩展

## Install

安装 nuget 包 `WeihanLi.DataProtection`

## ParamsProtection

`ParamsProtection` 是为了保护 asp.net core webapi 项目的某些参数而设计的，也可以用来做一定程度上的反爬虫。

### GetStarted

通过示例项目查看更多详细信息

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

跑起来示例项目，你可以直接在 sample 项目下运行 `dotnet run` 命令，在浏览器中访问 `/api/values` 路径，你会得到类似以下的响应结果

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

然而在代码里你什么都不需要做，还是直接返回原来的内容即可，原来的返回内容如下：

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

because I've set the "id" param should be propected, and when you access the `/api/values/{id}` path use integer id directly you will get a 4xx (412 by default) response,
while if you use the id returned from the result from `api/values`,it will return the result succssfully, and you can even set expiresIn when the protected value will be expired.

### More

你可以设置更多参数来更适合你的使用

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

你可以改变一些值来改变参数保护模式:

- 设置 `Enabled` 为 `false` 以禁用参数保护
- 修改 `ProtectorPurpose` 的值以改变 `DataProtector` 的 purpose
- 设置 `AllowUnprotectedParams` 为 `true` 以允许原始参数的访问
- 设置 `InvalidRequestStatusCode` 的值来改变不合法参数访问时响应的 Status Code
- 修改 `ExpiresIn` 的值以改变已经保护的参数的值的过期时间
- 设置 `ProtectParams` 的值来指定要进行参数保护的参数名称

## Contact

Contact me: <weihanli@outlook.com>