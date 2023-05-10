variable "location" {
  description = "Azure location"
}

variable "project_name" {
  description = "Project name"
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
}

variable "administrator_login" {
  description = "The administrator login for the MSSQL server."
}

variable "administrator_login_password" {
  description = "The administrator login password for the MSSQL server."
}
