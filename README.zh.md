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

因为在启动的时候已经设置了 `id` 参数应该被保护，所以当你访问 `/api/values/{id}` 这个地址的时候，如果没有设置 `AllowUnprotectedParams` 为 `true` 的话，直接使用原始的 `int` 类型的 id 去访问就会得到一个 4xx(默认是412) 状态码的响应，如果用从 `/api/values` 返回的 id 的值去访问就会正常的拿到响应。

除此之外你可以设置被保护的值的过期时间，通过设置一个比较短的过期时间来一定程度上的反爬虫，有个不太友好的地方就是可能会一定程序上的影响用户体检，如果用户打开一个页面长期没有操作就可能会导致某些操作可能会失败，需要用户重新操作。

你也可以是 `POST` 或 `PUT` 请求中使用被保护的值，如果被保护的值已经过期，你会从服务得到一个 4xx(默认 412) 的响应。

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