using '../main.bicep'

param env = 'prod'
param location = 'australiasoutheast'
param appServicePlanSku = 'P1v3'
param pgSku = 'Standard_D2s_v3'
param pgAdminPassword = readEnvironmentVariable('PG_ADMIN_PASSWORD', '')
param scalarClientSecret = readEnvironmentVariable('SCALAR_CLIENT_SECRET', '')
param adminClientSecret = readEnvironmentVariable('ADMIN_CLIENT_SECRET', '')
param watcherClientSecret = readEnvironmentVariable('WATCHER_CLIENT_SECRET', '')
param adminPassword = readEnvironmentVariable('ADMIN_PASSWORD', '')
