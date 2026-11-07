// The framework's site in Azure (F61.4): everything in rg-louis-agent-za that isn't an app revision, as one
// deployment that creates what is missing and updates what exists. deploy.ps1 infra makes the resource group and
// runs it; this file is the record of what Azure holds. The site is static Razor pages (demos/louis-agent/web/louis-agent.site)
// that spend nothing and keep no data, so it needs no network, no database and no secret: a registry for its
// image, a workspace for its logs, an environment, and the app itself from site.yaml.

targetScope = 'resourceGroup'

@description('Where everything is; the resource group\'s own region.')
param location string = resourceGroup().location

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'acrlouisagentza'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'louis-agent-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: 'louis-agent-env'
  location: location
  identity: {
    // The environment pulls the site's image from the registry as itself (site.yaml's `identity: system-environment`).
    type: 'SystemAssigned'
  }
  properties: {
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

// AcrPull for the environment on the registry; the role's id is Azure's fixed one for AcrPull.
resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, environment.id, 'AcrPull')
  scope: registry
  properties: {
    principalId: environment.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

@description('The environment the site is created in.')
output environmentId string = environment.id

@description('The environment\'s public address: the A record for louis-agent.dev.')
output environmentStaticIp string = environment.properties.staticIp

@description('The registry the site\'s image is built in.')
output registryServer string = registry.properties.loginServer
