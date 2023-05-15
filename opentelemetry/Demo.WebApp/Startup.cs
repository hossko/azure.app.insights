    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
    using Microsoft.Extensions.Azure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Logs;
    using Azure.Monitor.OpenTelemetry.Exporter;


    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System;



    namespace Demo.WebApp
    {
        public class Startup
        {
            private readonly IWebHostEnvironment _environment;
            private IConfigurationRoot _configuration;
            public Startup(IWebHostEnvironment environment, IConfiguration configuration)
            {
                _environment = environment;

                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(environment.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

                _configuration = configBuilder.Build();

                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();                
                });

                ILogger logger = loggerFactory.CreateLogger<Startup>();
                logger.LogInformation(_configuration["ServiceName"]);
                logger.LogWarning(_configuration.GetSection("ApplicationInsights")["ConnectionString"]);
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton(_configuration);
                services.AddControllersWithViews();

                // In production, the React files will be served from this directory
                services.AddSpaStaticFiles(configuration =>
                {
                    configuration.RootPath = "ClientApp/build";
                });

                services.AddHttpClient();
                services.AddAzureClients(builder =>
                {
                    builder.AddServiceBusClient(_configuration.GetConnectionString("ServiceBus"));
                });

                if (_environment.IsDevelopment())
                {
                    services.AddOpenTelemetry()
                            .WithTracing(builder =>
                            {
                                builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(_configuration["ServiceName"]));
                                builder.AddAspNetCoreInstrumentation();
                                builder.AddSqlClientInstrumentation(options => options.SetDbStatementForText = true);
                                builder.AddHttpClientInstrumentation();
                                builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = _configuration.GetSection("ApplicationInsights")["ConnectionString"]);
                            //  builder.AddConsoleExporter();
                            });


                    // Define and configure the resource builder
                    var resourceBuilder = ResourceBuilder.CreateDefault()
                            // add attributes for the name and version of the service
                            .AddService(_configuration["ServiceName"], serviceVersion: "1.0.0")
                            // add attributes for the OpenTelemetry SDK version
                            .AddTelemetrySdk()
                            // add custom attributes
                            .AddAttributes(new Dictionary<string, object>
                            {
                                ["host.name"] = Environment.MachineName,
                                ["os.description"] = RuntimeInformation.OSDescription,
                                ["deployment.environment"] = _environment.EnvironmentName.ToLowerInvariant(),
                            });
                    
                    services.AddLogging(loggingBuilder =>
                            {
                                // Configure OpenTelemetry logging
                                loggingBuilder.ClearProviders()
                                    .AddOpenTelemetry(loggerOptions =>
                                    {                          
                                                loggerOptions
                                                    // define the resource
                                                    .SetResourceBuilder(resourceBuilder)
                                                    // add custom processor
                                                    .AddProcessor(new CustomLogProcessor())
                                                    // send logs to the console using exporter
                                                    .AddConsoleExporter()
                                                    // send logs to Azure Monitor
                                                    .AddAzureMonitorLogExporter(options => options.ConnectionString =  _configuration.GetSection("ApplicationInsights")["ConnectionString"]);

                                                loggerOptions.IncludeFormattedMessage = true;
                                                loggerOptions.IncludeScopes = true;
                                                loggerOptions.ParseStateValues = true;
                                    });
                            });        


                            
                }
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();

                    // Use the appsettings.Development.json file
                    var configBuilder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

                    _configuration = configBuilder.Build();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseSpaStaticFiles();

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller}/{action=Index}/{id?}");
                });

                app.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "ClientApp";

                    if (env.IsDevelopment())
                    {
                        spa.UseReactDevelopmentServer(npmScript: "start");
                    }
                });
            }
        }
    }
