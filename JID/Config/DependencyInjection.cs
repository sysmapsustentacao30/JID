using JID.Extensions;
using KissLog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JID.Config
{
    public static class DependencyInjection
    {
        public static IServiceCollection ResolveDependencies(this IServiceCollection services)
        {
            services.AddTransient<IExcelRead, ExcelRead>();
            services.AddTransient<IJiraConn, JiraConn>();
            services.AddTransient<IUipathConn, UipathConn>();

            // register dependencies
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped((context) => Logger.Factory.Get());

            return services;
        }
    }
}
