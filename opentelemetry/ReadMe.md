**Dotnet Distributed Tracing Examples**

This deployment use doesnt include any instrumentation and only include raw services running on AppServices and FunctionApp
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
