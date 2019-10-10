using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeihanLi.DataProtection;

namespace Core3_0Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson();

            services.AddDataProtection()
                .AddParamsProtection(options =>
                {
                    options.AllowUnprotectedParams = true;
                    options.ExpiresIn = 10;

                    options.ParamValueProtectFuncEnabled = true;
                    options.ParamValueNeedProtectFunc = str => long.TryParse(str, out _);

                    options.ProtectParams = new[]
                    {
                        "id",
                        "val",
                    };

                    options.AddProtectValue<JsonResult>(r => r.Value);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
