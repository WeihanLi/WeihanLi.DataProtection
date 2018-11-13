using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WeihanLi.DataProtection
{
    public static class ServiceCollectionExtensions
    {
        public static IDataProtectionBuilder AddParamsProtection(this IDataProtectionBuilder builder)
        {
            builder.Services.AddParamsProtection(null);

            return builder;
        }

        public static IDataProtectionBuilder AddParamsProtection(this IDataProtectionBuilder builder, Action<ParamsProtectionOptions> optionsAction)
        {
            builder.Services.AddParamsProtection(optionsAction);

            return builder;
        }

        internal static IServiceCollection AddParamsProtection(this IServiceCollection serviceCollection, Action<ParamsProtectionOptions> optionsAction)
        {
            if (null == serviceCollection)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }
            if (null == optionsAction)
            {
                optionsAction = options => { };
            }

            serviceCollection.AddOptions();
            serviceCollection.Configure(optionsAction);

            serviceCollection.Configure<MvcOptions>(action =>
            {
                action.Filters.Add<ParamsProtectorResourceFilter>();
                action.Filters.Add<ParamsProtectorResultFilter>();
                action.InputFormatters.Insert(0, new ParamsProtectorInputFormatter());
            });

            return serviceCollection;
        }
    }
}
