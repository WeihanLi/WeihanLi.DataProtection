# WeihanLi.DataProtection

## Intro

asp.net core data protection extensions

## Install

install the package `WeihanLi.DataProtection`

## ParamsProtection

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

because I've set the "id" param should be propected, and when you access the `/api/values/{id}` path use integer id directly you will get a 400 response,
while if you use the id returned from the result from `api/values`,it will return the result succssfully, and you can even set expiresIn when the protected value will be expired.

## Contact

Contact me: <weihanli@outlook.com>