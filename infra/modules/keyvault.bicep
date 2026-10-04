@description('Location for resources.')
param location string

@description('Tags to apply to resources.')
param tags object

@description('Stable token used to build unique resource names.')
param resourceToken string

@secure()
param databaseConnectionString string

@secure()
param jwtSecretKey string

@secure()
param smtpPassword string = ''

@secure()
param openRouteServiceApiKey string = ''

@secure()
param traccarToken string = ''

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${resourceToken}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
  }
}

resource databaseSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'database-connection-string'
  properties: {
    value: databaseConnectionString
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

resource smtpSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(smtpPassword)) {
  parent: keyVault
  name: 'smtp-password'
  properties: {
    value: smtpPassword
  }
}

resource orsSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(openRouteServiceApiKey)) {
  parent: keyVault
  name: 'openrouteservice-api-key'
  properties: {
    value: openRouteServiceApiKey
  }
}

resource traccarSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(traccarToken)) {
  parent: keyVault
  name: 'traccar-token'
  properties: {
    value: traccarToken
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
