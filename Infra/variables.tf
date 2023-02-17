variable "resource_group_location" {
  type        = string
  description = "Location of the resource group."
}

variable "resource_group_name_prefix" {
  type        = string
  description = "Name of the ressource group"
}

variable "service_plan_name" {
  type        = string
  description = "Name of service plan"
}

variable "windows_web_name" {
  type        = string
  description = "Name of web app"
}

variable "db_server_name" {
  type        = string
  description = "The name of the database server."
}

variable "db_login" {
  type        = string
  description = "The login of the database server."
}

variable "db_password" {
  type        = string
  description = "The password of the database server."
}

variable "db_account_name" {
  type        = string
  description = "The name of the database account."
}

variable "db_name" {
  type        = string
  description = "The name of the database."
}