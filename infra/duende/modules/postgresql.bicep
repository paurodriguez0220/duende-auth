param name string
param location string
param adminUser string
@secure()
param adminPassword string
param sku string
param databases array

var tier = startsWith(sku, 'Standard_B') ? 'Burstable' : 'GeneralPurpose'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01' = {
  name: name
  location: location
  sku: {
    name: sku
    tier: tier
  }
  properties: {
    administratorLogin: adminUser
    administratorLoginPassword: adminPassword
    version: '16'
    storage: { storageSizeGB: 32 }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: { mode: 'Disabled' }
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
  }
}

resource azureServicesFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01' = {
  parent: server
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01' = [for dbName in databases: {
  parent: server
  name: dbName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}]

output fqdn string = server.properties.fullyQualifiedDomainName
