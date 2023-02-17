# Creation of resource group 
resource "azurerm_resource_group" "rg_gaming_1" {
  location = var.resource_group_location
  name     = var.resource_group_name_prefix
}

# Creation of service plan, with windows machine
resource "azurerm_service_plan" "asp_gaming_1" {
  name                = var.service_plan_name
  resource_group_name = azurerm_resource_group.rg_gaming_1.name
  location            = azurerm_resource_group.rg_gaming_1.location
  os_type             = "Windows"
  sku_name            = "P1v2"
}

# Creation of web application
resource "azurerm_windows_web_app" "web_gaming_2" {
  name                = var.windows_web_name
  resource_group_name = azurerm_resource_group.rg_gaming_1.name
  location            = azurerm_service_plan.asp_gaming_1.location
  service_plan_id     = azurerm_service_plan.asp_gaming_1.id

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v7.0"
    }
  }
}

# Creation of mysql server
resource "azurerm_mssql_server" "db_server_1" {
  name                         = var.db_server_name
  resource_group_name          = azurerm_resource_group.rg_gaming_1.name
  location                     = azurerm_resource_group.rg_gaming_1.location
  version                      = "12.0"
  administrator_login          = var.db_login
  administrator_login_password = var.db_password

  tags = {
    environment = "production"
  }
}

# Add firewall rule to the database
resource "azurerm_mssql_firewall_rule" "gaming_1" {
  name                = "FirewallRule1"
  server_id           = azurerm_mssql_server.db_server_1.id
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"
}

# Creation of storage account
resource "azurerm_storage_account" "db_account_1" {
  name                     = var.db_account_name
  resource_group_name      = azurerm_resource_group.rg_gaming_1.name
  location                 = azurerm_resource_group.rg_gaming_1.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# Creation of mysql database
resource "azurerm_mssql_database" "db_vm" {
  name           = var.db_name
  server_id      = azurerm_mssql_server.db_server_1.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 1
  read_scale     = false
  sku_name       = "S0"
  zone_redundant = false
}