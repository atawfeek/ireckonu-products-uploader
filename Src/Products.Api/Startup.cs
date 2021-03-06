﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediatR;
using System.Reflection;
using Products.Api.Middleware;
using Products.Dto.Options;
using Products.Service.Interfaces.MultipleImplementation;
using Products.Service.Services;
using Products.Service.Interfaces;

namespace Products.Api
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
            //Register http context in DI container to be injected in constructors upnon need.
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //Register MediatR handler
            services.AddMediatR(typeof(Startup).GetTypeInfo().Assembly);
            //Register MediatR handler(s) located in our specific project
            var assembly = AppDomain.CurrentDomain.Load("Products.Commands");
            services.AddMediatR(assembly);

            //register product storages considering multiple implementations
            services.AddScoped<IProductStorageRepository, ProductStorageRepository>();

            services.AddScoped<ProductDbStorage>();
            services.AddScoped<ProductJsonStorage>();

            services.AddScoped<Func<string, IProductStorage>>(serviceProvider => key =>
            {
                switch (key)
                {
                    case "DB":
                        return serviceProvider.GetService<ProductDbStorage>();
                    case "JSON":
                        return serviceProvider.GetService<ProductJsonStorage>();
                    default:
                        return serviceProvider.GetService<ProductDbStorage>();
                }
            });

            //register Mvc
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //register swagger.
            services.ConfigureSwagger();

            //register options
            services.ConfigureOptions(Configuration);

            //register middleware services
            var appOptions = new ApplicationSettingsOptions();
            Configuration.GetSection(StaticData.ApplicationSettings).Bind(appOptions);

            services.ConfigureServices(Configuration, appOptions);
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            //use swagger in middleware pipeline
            app.ConfigureSwagger(env, Configuration.GetValue<bool>(StaticData.UseSwagger));

            app.UseMvc();
        }
    }
}
