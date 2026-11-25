// The apps' environment (FX249), in the apps' own resource group, deployed by main.bicep with that group as its scope.
// It sits on no network of ours: an environment on one is billed a load balancer and its public IP, and an
// environment's network can't be changed once it is made. Everything the apps share (the registry, the vault, the
// storage, the identities, Application Insights) stays in main.bicep's group; what this file needs of them comes in as
// parameters, the keys as secure ones.

@description('Where it runs.')
param location string

@description('The Container Apps environment the two apps run in.')
param environmentName string

@description('The log workspace the environment sends its apps\' console logs to.')
param workspaceCustomerId string

@secure()
@description('The log workspace\'s key.')
param workspaceSharedKey string

@secure()
@description('Application Insights\' connection string, where the telemetry agent sends logs and traces.')
param insightsConnectionString string

@description('The name the environment knows the apps\' share by, which each app\'s yaml mounts.')
param shareLinkName string

@description('The storage account that holds the apps\' share.')
param storageName string

@secure()
@description('The storage account\'s key, the share is mounted with.')
param storageKey string

@description('The file share the apps keep their logs and saved content on, a folder each.')
param shareName string

// The environment, open to the internet, on the Consumption profile alone, on no network of ours. A plain resource:
// its module has no setting for the telemetry agent, which sends the apps' logs and traces to Application Insights, so
// a deploy through it would switch the agent off. It pulls the apps' images as itself (their yaml:
// system-environment).
resource environment 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: environmentName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    peerAuthentication: {
      mtls: {
        enabled: false
      }
    }
    peerTrafficConfiguration: {
      encryption: {
        enabled: false
      }
    }
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspaceCustomerId
        sharedKey: workspaceSharedKey
      }
    }
    // Bicep's bundled types for this version leave out the two settings below, but the API has them: a read of the
    // environment at 2025-07-01, 2026-01-01 and 2026-07-01 returns both. The connection string is never read back.
    #disable-next-line BCP037
    appInsightsConfiguration: {
      connectionString: insightsConnectionString
    }
    #disable-next-line BCP037
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: ['appInsights']
        includeDapr: false
      }
      logsConfiguration: {
        destinations: ['appInsights']
      }
    }
  }
}

// The environment's link to the apps' share, by the name each app's yaml mounts, read and write, with the account's
// key. A plain resource beside the environment, since it is the environment's own.
resource shareLink 'Microsoft.App/managedEnvironments/storages@2026-01-01' = {
  parent: environment
  name: shareLinkName
  properties: {
    azureFile: {
      accountName: storageName
      accountKey: storageKey
      shareName: shareName
      accessMode: 'ReadWrite'
    }
  }
}

// What main.bicep passes on to deploy.ps1, and the identity the registry gives the right to pull.
output environmentId string = environment.id
output staticIp string = environment.properties.staticIp
output principalId string = environment.identity.principalId
