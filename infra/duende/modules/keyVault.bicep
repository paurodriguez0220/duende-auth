param name string
param location string

@secure()
param scalarClientSecret string
@secure()
param adminClientSecret string
@secure()
param watcherClientSecret string
@secure()
param adminPassword string
@secure()
param identityConnectionString string
@secure()
param grantsConnectionString string
@secure()
param configConnectionString string

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

resource secretScalarClient 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(scalarClientSecret)) {
  parent: kv
  name: 'ScalarClientSecret'
  properties: { value: scalarClientSecret }
}

resource secretAdminClient 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(adminClientSecret)) {
  parent: kv
  name: 'AdminClientSecret'
  properties: { value: adminClientSecret }
}

resource secretWatcherClient 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(watcherClientSecret)) {
  parent: kv
  name: 'WatcherClientSecret'
  properties: { value: watcherClientSecret }
}

resource secretAdminPassword 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(adminPassword)) {
  parent: kv
  name: 'AdminPassword'
  properties: { value: adminPassword }
}

resource secretIdentityConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(identityConnectionString)) {
  parent: kv
  name: 'IdentityConnectionString'
  properties: { value: identityConnectionString }
}

resource secretGrantsConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(grantsConnectionString)) {
  parent: kv
  name: 'GrantsConnectionString'
  properties: { value: grantsConnectionString }
}

resource secretConfigConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(configConnectionString)) {
  parent: kv
  name: 'ConfigConnectionString'
  properties: { value: configConnectionString }
}

output id string = kv.id
output name string = kv.name
