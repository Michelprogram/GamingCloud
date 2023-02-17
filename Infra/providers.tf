terraform {
  required_version = "1.3.8"

  # Package used by terraform, sdk azure
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "3.43.0"
    }
  }
}

# Credentials
provider "azurerm" {
  features {}
  subscription_id = "0e84bdb3-b9a6-4029-904f-b8bdaf0c8e19"
  tenant_id       = "b7b023b8-7c32-4c02-92a6-c8cdaa1d189c"
}