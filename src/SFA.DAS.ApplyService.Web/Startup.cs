﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using SFA.DAS.ApplyService.Application.Apply.Validation;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.DfeSignIn;
using SFA.DAS.ApplyService.Session;
using SFA.DAS.ApplyService.Web.Infrastructure;
using SFA.DAS.ApplyService.Web.Validators;
using StructureMap;
using StackExchange.Redis;

namespace SFA.DAS.ApplyService.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Startup> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IHostingEnvironment _env;
        private readonly IApplyConfig _configService;
        private const string ServiceName = "SFA.DAS.ApplyService";
        private const string Version = "1.0";

        public Startup(IConfiguration configuration, ILogger<Startup> logger, IHostingEnvironment hostingEnvironment, IHostingEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _env = env;
            _configService =  new ConfigurationService(env, _configuration["EnvironmentName"], _configuration["ConfigurationStorageConnectionString"], Version, ServiceName).GetConfig().GetAwaiter().GetResult();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;
        
            ConfigureAuth(services);
            
            services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en-GB");
                options.SupportedCultures = new List<CultureInfo> { new CultureInfo("en-GB") };
                options.SupportedUICultures = new List<CultureInfo> { new CultureInfo("en-GB") };
                options.RequestCultureProviders.Clear();
            });
            
            services.AddMvc(options => { options.Filters.Add<PerformValidationFilter>(); })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            
            if (_env.IsDevelopment())
            {
                var redis = ConnectionMultiplexer.Connect(
                    $"{_configService.SessionRedisConnectionString}");

                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, "AssessorApply-DataProtectionKeys")
                    .SetApplicationName("AssessorApply");

                services.AddDistributedMemoryCache();
            }
            else
            {
                try
                {
                    var redis = ConnectionMultiplexer.Connect(
                        $"{_configService.SessionRedisConnectionString},DefaultDatabase=1");

                    services.AddDataProtection()
                        .PersistKeysToStackExchangeRedis(redis, "AssessorApply-DataProtectionKeys")
                        .SetApplicationName("AssessorApply");
                    services.AddDistributedRedisCache(options =>
                    {
                        options.Configuration = $"{_configService.SessionRedisConnectionString},DefaultDatabase=0";
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        $"Error setting redis for session.  Conn: {_configService.SessionRedisConnectionString}");
                    throw;
                }
            }

            services.AddSession(opt =>
            {
                opt.IdleTimeout = TimeSpan.FromHours(1);
                opt.Cookie = new CookieBuilder() {Name = ".Apply.Session", HttpOnly = true};
            });
            
            services.AddAntiforgery(options => options.Cookie = new CookieBuilder() { Name = ".Apply.AntiForgery", HttpOnly = true });


            return ConfigureIOC(services);
        }
        
        private IServiceProvider ConfigureIOC(IServiceCollection services)
        {
            var container = new Container();

            container.Configure(config =>
            {
                config.Scan(_ =>
                {
                    _.AssembliesFromApplicationBaseDirectory(c => c.FullName.StartsWith("SFA"));
                    _.WithDefaultConventions();

                    _.AddAllTypesOf<IValidator>();
                });

                config.For<IHttpContextAccessor>().Use<HttpContextAccessor>();
                
                config.For<IConfigurationService>()
                    .Use<ConfigurationService>().Singleton()
                    .Ctor<string>("environment").Is(_configuration["EnvironmentName"])
                    .Ctor<string>("storageConnectionString").Is(_configuration["ConfigurationStorageConnectionString"])
                    .Ctor<string>("version").Is("1.0")
                    .Ctor<string>("serviceName").Is("SFA.DAS.ApplyService");
                
                config.For<ISessionService>().Use<SessionService>().Ctor<string>("environment")
                    .Is(_configuration["EnvironmentName"]);
                config.For<IDfeSignInService>().Use<DfeSignInService>();

                config.For<IUsersApiClient>().Use<UsersApiClient>();
                config.For<IApplicationApiClient>().Use<ApplicationApiClient>();
                config.For<OrganisationApiClient>().Use<OrganisationApiClient>();
                config.For<OrganisationSearchApiClient>().Use<OrganisationSearchApiClient>();
                config.For<CreateAccountValidator>().Use<CreateAccountValidator>();
                config.For<UserService>().Use<UserService>();
                config.Populate(services);
            });

            return container.GetInstance<IServiceProvider>();
        }

        protected virtual void ConfigureAuth(IServiceCollection services)
        {

            var configService = new ConfigurationService(_hostingEnvironment, _configuration["EnvironmentName"],
                _configuration["ConfigurationStorageConnectionString"], "1.0", "SFA.DAS.ApplyService");
            
            services.AddDfeSignInAuthorization(configService.GetConfig().Result, _logger);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            
            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();
            app.UseRequestLocalization();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}