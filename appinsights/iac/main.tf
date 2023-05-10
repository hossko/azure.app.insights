resource "azurerm_resource_group" "resource_group" {
  location = var.location
  name     = "${var.project_name}-rg"
  tags     = var.tags
}

resource "azurerm_application_insights" "application_insights" {
  application_type    = "web"
  location            = var.location
  name                = "${var.project_name}-appi"
  resource_group_name = azurerm_resource_group.resource_group.name
  sampling_percentage = 0
  tags                = var.tags

  workspace_id = azurerm_log_analytics_workspace.log_analytics_workspace.id
  depends_on   = [azurerm_log_analytics_workspace.log_analytics_workspace]
}

output "app_insights_connection_string" {
  sensitive = true
  value = azurerm_application_insights.application_insights.connection_string
}

output "app_insights_instrumentation_key" {
  sensitive = true
  value = azurerm_application_insights.application_insights.instrumentation_key
}


resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  location            = var.location
  name                = "${var.project_name}-log"
  resource_group_name = azurerm_resource_group.resource_group.name
  tags                = var.tags
}
#########################################################################

resource "azurerm_servicebus_namespace" "servicebus_namespace" {
  location            = var.location
  name                = "${var.project_name}-sb-namespace"
  resource_group_name = azurerm_resource_group.resource_group.name
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_servicebus_namespace_network_rule_set" "network_rule_set" {
  namespace_id = azurerm_servicebus_namespace.servicebus_namespace.id
}

resource "azurerm_servicebus_namespace_authorization_rule" "authorization_rule" {
  listen       = true
  manage       = true
  name         = "${var.project_name}-sb-auth-rule"
  namespace_id = azurerm_servicebus_namespace.servicebus_namespace.id
  send         = true
}

output "servicebus_connection_string" {
  sensitive = true
  value = azurerm_servicebus_namespace_authorization_rule.authorization_rule.primary_connection_string
}

resource "azurerm_servicebus_queue" "servicebus_queue" {
  name         = "sbq-demo"
  namespace_id = azurerm_servicebus_namespace.servicebus_namespace.id
}
########################################################################
resource "azurerm_mssql_server" "mssql_server" {
  administrator_login          = var.administrator_login
  administrator_login_password = var.administrator_login_password
  location            = var.location
  name                = "${var.project_name}-mssql-server"
  resource_group_name = azurerm_resource_group.resource_group.name
  version             = "12.0"
}

resource "azurerm_mssql_firewall_rule" "Demo_Service_IP" {
  end_ip_address   = "52.154.243.213"
  name             = "Demo_Service_IP"
  server_id        = azurerm_mssql_server.mssql_server.id
  start_ip_address = "52.154.243.213"
  depends_on = [
    azurerm_mssql_server.mssql_server,
  ]
}

resource "azurerm_mssql_database" "mssql_database" {
  name                 = "${var.project_name}-mssql-db"
  server_id            = azurerm_mssql_server.mssql_server.id
  storage_account_type = "Local"
}

output "mssql_connection_string" {
  sensitive = true
  value = "Server=tcp:${azurerm_mssql_server.mssql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.mssql_database.name};Persist Security Info=False;User ID=${azurerm_mssql_server.mssql_server.administrator_login};Password=${azurerm_mssql_server.mssql_server.administrator_login_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

##############################################################################################

resource "azurerm_service_plan" "worker_service_plan" {
  name                = "${var.project_name}-worker-plan"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "Y1"
  depends_on = [azurerm_resource_group.resource_group]
}

resource "azurerm_service_plan" "service_service_plan" {
  location            = var.location
  name                = "${var.project_name}-service-plan"
  os_type             = "Linux"
  resource_group_name = azurerm_resource_group.resource_group.name
  sku_name            = "B1"
  depends_on = [azurerm_resource_group.resource_group]
}

resource "azurerm_service_plan" "webapp_service_plan" {
  location            = var.location
  name                = "${var.project_name}-webapp-plan"
  os_type             = "Linux"
  resource_group_name = azurerm_resource_group.resource_group.name
  sku_name            = "B1"
  depends_on = [azurerm_resource_group.resource_group]
}

resource "azurerm_storage_account" "worker_storage" {
  name                     = "workerstorage123123"
  resource_group_name      = azurerm_resource_group.resource_group.name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}


resource "azurerm_linux_function_app" "worker_function" {
  app_settings = {
    ServiceBus = azurerm_servicebus_namespace_authorization_rule.authorization_rule.primary_connection_string
  }
  location                   = var.location
  name                       = "${var.project_name}-worker-function"
  resource_group_name        = azurerm_resource_group.resource_group.name
  service_plan_id            = azurerm_service_plan.worker_service_plan.id
  

  site_config {
    application_insights_connection_string = azurerm_application_insights.application_insights.connection_string
    application_insights_key               = azurerm_application_insights.application_insights.instrumentation_key
    ftps_state                             = "FtpsOnly"
    application_stack {
      dotnet_version = "6.0"
    }
  }
  storage_account_name       = azurerm_storage_account.worker_storage.name
  storage_account_access_key = azurerm_storage_account.worker_storage.primary_access_key

  depends_on = [azurerm_service_plan.worker_service_plan]
}

resource "azurerm_linux_web_app" "service_app" {
  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY             = azurerm_application_insights.application_insights.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING      = azurerm_application_insights.application_insights.connection_string
  }
  location            = var.location
  name                = "${var.project_name}-service-app"
  resource_group_name = azurerm_resource_group.resource_group.name
  service_plan_id     = azurerm_service_plan.service_service_plan.id
  logs {
    http_logs {
      file_system {
        retention_in_days = 0
        retention_in_mb   = 35
      }
    }
  }
  site_config {
    always_on  = false
    ftps_state = "FtpsOnly"
    application_stack {
      dotnet_version = "6.0"
    }
  }
  depends_on = [
    azurerm_service_plan.service_service_plan,
  ]
}

resource "azurerm_linux_web_app" "web_app" {
  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY             = azurerm_application_insights.application_insights.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING      = azurerm_application_insights.application_insights.connection_string
  }
  location            = var.location
  name                = "${var.project_name}-web-app"
  resource_group_name = azurerm_resource_group.resource_group.name
  service_plan_id     = azurerm_service_plan.webapp_service_plan.id
  logs {
    http_logs {
      file_system {
        retention_in_days = 0
        retention_in_mb   = 35
      }
    }
  }
  site_config {
    always_on  = false
    ftps_state = "FtpsOnly"
    application_stack {
      dotnet_version = "6.0"
    }
  }
  depends_on = [
    azurerm_service_plan.webapp_service_plan,
  ]
}

