param name string
param location string
param planId string
param appInsightsConnectionString string
param keyVaultName string

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'ApplicationInsightsAgent_EXTENSION_VERSION', value: '~3' }
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'Database__Provider', value: 'postgres' }
        { name: 'Auth__Authority', value: 'https://${name}.azurewebsites.net' }
        { name: 'ConnectionStrings__IdentityConnection', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=IdentityConnectionString)' }
        { name: 'ConnectionStrings__GrantsConnection', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=GrantsConnectionString)' }
        { name: 'ConnectionStrings__ConfigConnection', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ConfigConnectionString)' }
        { name: 'Clients__ScalarClient__Secret', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ScalarClientSecret)' }
        { name: 'Clients__AdminClient__Secret', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=AdminClientSecret)' }
        { name: 'Clients__WatcherClient__Secret', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=WatcherClientSecret)' }
        { name: 'SeedUsers__AdminPassword', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=AdminPassword)' }
      ]
    }
  }
}

output principalId string = webApp.identity.principalId
output defaultHostname string = webApp.properties.defaultHostName
