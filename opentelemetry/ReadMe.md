**Dotnet Distributed Tracing Examples**

This deployment utilizer OpenTelemetry SDK for Traces, Logs and Metric instrumentation and export to Azure Applicaiton Insights
Components used:
* Azure App Services (WebApp, ServiceApp)
* Azure Function App (WorkerApp)
* Azure SQL
* Azure ServiceBus
* Azure Application Insights
* Azure Log Analytics WorkSpace

Set up Azure Environment using Terraform IaC
-------------------------------------------------------

Log in to your Azure resources if necessary

```powershell
az login
```

Then use the script to create the required resources, which will also output the required connection string.

```shell
terraform plan
terraform apply
```

This will create an Azure Monitor Log Analytics Workspace, and then an Application Insights instance connected to it.

You can log in to the Azure portal to check the logging components were created at `https://portal.azure.com`


Add configuration
-----------------

The script will output the connection string that you need to use. Add the connection string to an ApplicationInsights section in `appsettings.Development.json` for all three projects (WebApp, Service, Worker). Alternatively, you can specify it via the command line or environment variables when running (see below).

```json
  "ApplicationInsights": {
    "ConnectionString" : ""
  },
```

Also, by default only Warning logs and above are collected. In the same three `appsettings.Development.json` files change the configuration to add configuration for the ApplicationInsights logger to include Information level and above:

```json
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    },
```

OpenTelemetry Logging with iLogger
----------------------------------

```dotnet
//
using OpenTelemetry.Logs;


// OPTIONAL FOR LOG ENRICHMENT, add the CustomLogProcessor.cs Class and include it in the OpenTelemetry initialization

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

// Then, add the following code after the previous code to define the logging pipeline:
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




```