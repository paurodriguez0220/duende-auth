using '../main.bicep'

param env = 'dev'
param location = 'australiasoutheast'
param appServicePlanSku = 'B1'
param pgSku = 'Standard_B1ms'
param pgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD', '')
param scalarClientSecret = readEnvironmentVariable('SCALAR_CLIENT_SECRET', '')
param adminClientSecret = readEnvironmentVariable('ADMIN_CLIENT_SECRET', '')
param watcherClientSecret = readEnvironmentVariable('WATCHER_CLIENT_SECRET', '')
param adminPassword = readEnvironmentVariable('ADMIN_PASSWORD', '')
