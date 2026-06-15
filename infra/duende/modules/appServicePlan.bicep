param name string
param location string
param sku string

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  sku: { name: sku }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

output id string = plan.id
