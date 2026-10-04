targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the azd environment. Used to derive resource names.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('PostgreSQL connection string for the external/managed database (secret).')
@secure()
param databaseConnectionString string

@description('JWT signing secret key (secret).')
@secure()
param jwtSecretKey string

@description('SMTP password used for outbound email (secret). Optional.')
@secure()
param smtpPassword string = ''

@description('OpenRouteService API key (secret). Optional.')
@secure()
param openRouteServiceApiKey string = ''

@description('Traccar monitoring token (secret). Optional.')
@secure()
param traccarToken string = ''

@description('Google OAuth client ID (non-secret). Optional.')
param googleClientId string = ''

var abbrs = {
  resourceGroup: 'rg'
}

// Stable short hash used to make globally-unique resource names per environment.
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: '${abbrs.resourceGroup}-${environmentName}'
  location: location
  tags: tags
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
    databaseConnectionString: databaseConnectionString
    jwtSecretKey: jwtSecretKey
    smtpPassword: smtpPassword
    openRouteServiceApiKey: openRouteServiceApiKey
    traccarToken: traccarToken
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'web'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
  }
}

module appService 'modules/appservice.bicep' = {
  name: 'api'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
    keyVaultName: keyVault.outputs.keyVaultName
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    allowedCorsOrigin: staticWebApp.outputs.uri
    frontendBaseUrl: staticWebApp.outputs.uri
    googleClientId: googleClientId
  }
}

// Grant the API's managed identity permission to read secrets from Key Vault.
module keyVaultAccess 'modules/keyvault-access.bicep' = {
  name: 'keyvault-access'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    principalId: appService.outputs.identityPrincipalId
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.keyVaultName
output API_BASE_URL string = appService.outputs.uri
output WEB_BASE_URL string = staticWebApp.outputs.uri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
