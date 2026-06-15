targetScope = 'subscription'

param appName string = 'duende'
param env string
param location string
param appServicePlanSku string
param pgSku string
param pgAdminUser string = 'duendeadmin'

@secure()
param pgAdminPassword string

@secure()
param scalarClientSecret string

@secure()
param adminClientSecret string

@secure()
param watcherClientSecret string

@secure()
param adminPassword string

// Region abbreviation is hardcoded — this app always deploys to Australia Southeast.
var region = 'ase'
var prefix = '${appName}-${env}-${region}'
var pgDatabases = ['duende-identity', 'duende-grants', 'duende-config']

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${prefix}'
  location: location
}

module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'logAnalytics'
  scope: rg
  params: {
    name: 'log-${prefix}'
    location: location
  }
}

module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights'
  scope: rg
  params: {
    name: 'appi-${prefix}'
    location: location
    logAnalyticsId: logAnalytics.outputs.id
  }
}

module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql'
  scope: rg
  params: {
    name: 'psqlfs-${prefix}'
    location: location
    adminUser: pgAdminUser
    adminPassword: pgAdminPassword
    sku: pgSku
    databases: pgDatabases
  }
}

var pgBase = 'Host=${postgresql.outputs.fqdn};Username=${pgAdminUser};Password=${pgAdminPassword};SslMode=Require;'

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  scope: rg
  params: {
    name: 'kv-${prefix}'
    location: location
    scalarClientSecret: scalarClientSecret
    adminClientSecret: adminClientSecret
    watcherClientSecret: watcherClientSecret
    adminPassword: adminPassword
    identityConnectionString: '${pgBase}Database=duende-identity;'
    grantsConnectionString: '${pgBase}Database=duende-grants;'
    configConnectionString: '${pgBase}Database=duende-config;'
  }
}

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  scope: rg
  params: {
    name: 'asp-${prefix}'
    location: location
    sku: appServicePlanSku
  }
}

module webApp 'modules/webApp.bicep' = {
  name: 'webApp'
  scope: rg
  params: {
    name: 'app-${prefix}'
    location: location
    planId: appServicePlan.outputs.id
    appInsightsConnectionString: appInsights.outputs.connectionString
    keyVaultName: keyVault.outputs.name
  }
}

module keyVaultRbac 'modules/keyVault.rbac.bicep' = {
  name: 'keyVaultRbac'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: webApp.outputs.principalId
  }
}

output appUrl string = 'https://${webApp.outputs.defaultHostname}'
output resourceGroup string = rg.name
