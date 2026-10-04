@description('Location for resources.')
param location string

@description('Tags to apply to resources.')
param tags object

@description('Stable token used to build unique resource names.')
param resourceToken string

@description('Name of the Key Vault holding application secrets.')
param keyVaultName string

@description('Application Insights connection string.')
param applicationInsightsConnectionString string

@description('Frontend origin allowed by CORS (the Static Web App URL).')
param allowedCorsOrigin string

@description('Frontend base URL used for invite / password-reset links.')
param frontendBaseUrl string

@description('Google OAuth client ID (non-secret).')
param googleClientId string = ''

var serviceName = 'api'

// .NET reads hierarchical configuration keys. On Linux App Service, nested keys
// use '__' as the separator (e.g. ConnectionStrings:DefaultConnection -> ConnectionStrings__DefaultConnection).
// Secret values are pulled from Key Vault at runtime via managed identity.
var keyVaultRef = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName='

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'plan-${resourceToken}'
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-${serviceName}-${resourceToken}'
  location: location
  kind: 'app,linux'
  tags: union(tags, { 'azd-service-name': serviceName })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '${keyVaultRef}database-connection-string)'
        }
        {
          name: 'Jwt__SecretKey'
          value: '${keyVaultRef}jwt-secret-key)'
        }
        {
          name: 'Smtp__Password'
          value: '${keyVaultRef}smtp-password)'
        }
        {
          name: 'OpenRouteService__ApiKey'
          value: '${keyVaultRef}openrouteservice-api-key)'
        }
        {
          name: 'Monitoring__TraccarToken'
          value: '${keyVaultRef}traccar-token)'
        }
        {
          name: 'GoogleAuth__ClientId'
          value: googleClientId
        }
        {
          name: 'Cors__AllowedOrigins__0'
          value: allowedCorsOrigin
        }
        {
          name: 'Invite__FrontendBaseUrl'
          value: '${frontendBaseUrl}/convite'
        }
        {
          name: 'PasswordReset__FrontendBaseUrl'
          value: '${frontendBaseUrl}/reset-password'
        }
      ]
    }
  }
}

output name string = webApp.name
output uri string = 'https://${webApp.properties.defaultHostName}'
output identityPrincipalId string = webApp.identity.principalId
