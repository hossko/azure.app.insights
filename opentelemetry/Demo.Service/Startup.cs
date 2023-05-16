using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

using Azure.Monitor.OpenTelemetry.Exporter;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;


namespace Demo.Service
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
        }











        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_configuration);
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo.Service", Version = "v1" });
            });
            services.AddDbContext<WeatherContext>(options =>
              options.UseSqlServer(_configuration.GetConnectionString("WeatherContext")));

            services.AddHttpClient();


        // OpenTelemetry Configuration
        
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
                                            //.AddConsoleExporter()
                                            // send logs to Azure Monitor
                                            .AddAzureMonitorLogExporter(options => options.ConnectionString =  _configuration.GetSection("ApplicationInsights")["ConnectionString"]);

                                        loggerOptions.IncludeFormattedMessage = true;
                                        loggerOptions.IncludeScopes = true;
                                        loggerOptions.ParseStateValues = true;
                            });
                    }); 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, WeatherContext dbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demo.Service v1"));
            }

        // Ensure the database table is created
            dbContext.Database.EnsureCreated();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}