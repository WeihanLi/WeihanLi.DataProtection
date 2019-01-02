# RegisterCustomTypes

It'll protect your object result value by default, if you wanna protect other types, you should register the type that you wanna protect.

api defines as follows:

``` csharp
void AddProtectValue<TResult>(Expression<Func<TResult, object>> valueExpression) where TResult : class, IActionResult
```

`TResult` is the type that implement the `IActionResult` interface and it should be a class, the valueExpression shuold point out the value of the type instance that should be used for protection.

For example, if you wanna protect `JsonResult`, you should register like this:

``` csharp
options.AddProtectValue<JsonResult>(r => r.Value);
```