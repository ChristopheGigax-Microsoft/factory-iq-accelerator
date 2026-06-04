param capacityName string
param location string
param capacitySku string = 'F2'

resource capacity 'Microsoft.Fabric/capacities@2023-11-01' = {
  name: capacityName
  location: location
  sku: {
    name: capacitySku
    tier: 'Fabric'
  }
}

output capacityId string = capacity.id
