@description('Location for resources. Static Web Apps are available in a limited set of regions.')
param location string

@description('Tags to apply to resources.')
param tags object

@description('Stable token used to build unique resource names.')
param resourceToken string

var serviceName = 'web'

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'stapp-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': serviceName })
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: '/'
      outputLocation: 'dist'
    }
  }
}

output name string = staticWebApp.name
output uri string = 'https://${staticWebApp.properties.defaultHostname}'
