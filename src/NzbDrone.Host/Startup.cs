using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Processes;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Host.AccessControl;
using NzbDrone.Http.Authentication;
using NzbDrone.SignalR;
using Prowlarr.Api.V1.System;
using Prowlarr.Http;
using Prowlarr.Http.Authentication;
using Prowlarr.Http.ErrorManagement;
using Prowlarr.Http.Frontend;
using Prowlarr.Http.Middleware;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace NzbDrone.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b =>
            {
                b.ClearProviders();
                b.SetMinimumLevel(LogLevel.Trace);
                b.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
                b.AddFilter("Prowlarr.Http.Authentication", LogLevel.Information);
                b.AddFilter("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.Error);
                b.AddNLog();
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddResponseCompression(options => options.EnableForHttps = true);

            services.AddCors(options =>
            {
                options.AddPolicy(VersionedApiControllerAttribute.API_CORS_POLICY,
                    builder =>
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());

                options.AddPolicy("AllowGet",
                    builder =>
                    builder.AllowAnyOrigin()
                    .WithMethods("GET", "OPTIONS")
                    .AllowAnyHeader());
            });

            services
            .AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
            })
            .AddApplicationPart(typeof(SystemController).Assembly)
            .AddApplicationPart(typeof(StaticResourceController).Assembly)
            .AddJsonOptions(options =>
            {
                STJson.ApplySerializerSettings(options.JsonSerializerOptions);
            })
            .AddControllersAsServices();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "Prowlarr",
                    Description = "Prowlarr API docs",
                    License = new OpenApiLicense
                    {
                        Name = "GPL-3.0",
                        Url = new Uri("https://github.com/Prowlarr/Prowlarr/blob/develop/LICENSE")
                    }
                });

                var apiKeyHeader = new OpenApiSecurityScheme
                {
                    Name = "X-Api-Key",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "apiKey",
                    Description = "Apikey passed as header",
                    In = ParameterLocation.Header,
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "X-Api-Key"
                    },
                };

                c.AddSecurityDefinition("X-Api-Key", apiKeyHeader);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { apiKeyHeader, new string[] { } }
                });

                var apikeyQuery = new OpenApiSecurityScheme
                {
                    Name = "apikey",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "apiKey",
                    Description = "Apikey passed as header",
                    In = ParameterLocation.Query,
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "apikey"
                    },
                };

                c.AddServer(new OpenApiServer
                {
                    Url = "{protocol}://{hostpath}",
                    Variables = new Dictionary<string, OpenApiServerVariable>
                    {
                        { "protocol", new OpenApiServerVariable { Default = "http", Enum = new List<string> { "http", "https" } } },
                        { "hostpath", new OpenApiServerVariable { Default = "localhost:9696" } }
                    }
                });

                c.AddSecurityDefinition("apikey", apikeyQuery);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { apikeyQuery, new string[] { } }
                });
            });

            services
            .AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = STJson.GetSerializerSettings();
            });

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Configuration["dataProtectionFolder"]));

            services.AddSingleton<IAuthorizationPolicyProvider, UiAuthorizationPolicyProvider>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("SignalR", policy =>
                {
                    policy.AuthenticationSchemes.Add("SignalR");
                    policy.RequireAuthenticatedUser();
                });

                // Require auth on everything except those marked [AllowAnonymous]
                options.FallbackPolicy = new AuthorizationPolicyBuilder("API")
                .RequireAuthenticatedUser()
                .Build();
            });

            services.AddAppAuthentication();

            services.PostConfigure<ApiBehaviorOptions>(options =>
            {
                var builtInFactory = options.InvalidModelStateResponseFactory;

                options.InvalidModelStateResponseFactory = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(context.ActionDescriptor.DisplayName);

                    logger.LogError(STJson.ToJson(context.ModelState));

                    return builtInFactory(context);
                };
            });
        }

        public void Configure(IApplicationBuilder app,
                              IStartupContext startupContext,
                              Lazy<IMainDatabase> mainDatabaseFactory,
                              Lazy<ILogDatabase> logDatabaseFactory,
                              DatabaseTarget dbTarget,
                              ISingleInstancePolicy singleInstancePolicy,
                              InitializeLogger initializeLogger,
                              ReconfigureLogging reconfigureLogging,
                              IAppFolderFactory appFolderFactory,
                              IProvidePidFile pidFileProvider,
                              IConfigFileProvider configFileProvider,
                              IRuntimeInfo runtimeInfo,
                              IFirewallAdapter firewallAdapter,
                              ProwlarrErrorPipeline errorHandler)
        {
            initializeLogger.Initialize();
            appFolderFactory.Register();
            pidFileProvider.Write();

            reconfigureLogging.Reconfigure();

            EnsureSingleInstance(false, startupContext, singleInstancePolicy);

            // instantiate the databases to initialize/migrate them
            _ = mainDatabaseFactory.Value;
            _ = logDatabaseFactory.Value;

            dbTarget.Register();

            if (OsInfo.IsNotWindows)
            {
                Console.CancelKeyPress += (sender, eventArgs) => NLog.LogManager.Configuration = null;
            }

            if (OsInfo.IsWindows && runtimeInfo.IsAdmin)
            {
                firewallAdapter.MakeAccessible();
            }

            app.UseForwardedHeaders();
            app.UseMiddleware<LoggingMiddleware>();
            app.UsePathBase(new PathString(configFileProvider.UrlBase));
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                AllowStatusCode404Response = true,
                ExceptionHandler = errorHandler.HandleException
            });

            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseResponseCompression();
            app.Properties["host.AppName"] = BuildInfo.AppName;

            app.UseMiddleware<VersionMiddleware>();
            app.UseMiddleware<UrlBaseMiddleware>(configFileProvider.UrlBase);
            app.UseMiddleware<CacheHeaderMiddleware>();
            app.UseMiddleware<IfModifiedMiddleware>();
            app.UseMiddleware<BufferingMiddleware>(new List<string> { "/api/v1/command" });

            app.UseWebSockets();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            if (BuildInfo.IsDebug)
            {
                app.UseSwagger(c =>
                {
                    c.RouteTemplate = "docs/{documentName}/openapi.json";
                });
            }

            app.UseEndpoints(x =>
            {
                x.MapHub<MessageHub>("/signalr/messages").RequireAuthorization("SignalR");
                x.MapControllers();
            });
        }

        private void EnsureSingleInstance(bool isService, IStartupContext startupContext, ISingleInstancePolicy instancePolicy)
        {
            if (startupContext.Flags.Contains(StartupContext.NO_SINGLE_INSTANCE_CHECK))
            {
                return;
            }

            if (startupContext.Flags.Contains(StartupContext.TERMINATE))
            {
                instancePolicy.KillAllOtherInstance();
            }
            else if (startupContext.Args.ContainsKey(StartupContext.APPDATA))
            {
                instancePolicy.WarnIfAlreadyRunning();
            }
            else if (isService)
            {
                instancePolicy.KillAllOtherInstance();
            }
            else
            {
                instancePolicy.PreventStartIfAlreadyRunning();
            }
        }
    }
}
