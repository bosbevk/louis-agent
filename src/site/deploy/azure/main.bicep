// What louis-agent.dev and framework.louis-agent.dev stand on in Azure, so it can be made again from code (F170).
// Every resource is one of Microsoft's Azure Verified Modules, pinned to a version, and every name is a parameter:
// our own instance's values are in main.bicepparam beside this file. The two apps and their domains are released by
// deploy.ps1, which reads this deployment's outputs. Each module is told what stands wherever its default differs, so
// a deploy over the running sites changes nothing; what-if shows it (deploy.ps1 infra).
//
//   az deployment group what-if -g <group> -f main.bicep -p main.bicepparam

@description('Where everything runs.')
param location string

@description('The log workspace the environment sends its app logs to.')
param workspaceName string

@description('How many days the workspace keeps its logs.')
param workspaceRetentionDays int

@description('Application Insights, where the environment\'s telemetry agent sends logs and traces.')
param insightsName string

@description('How many days Application Insights keeps its data.')
param insightsRetentionDays int

@description('The Container Apps environment the two apps run in.')
param environmentName string

@description('The registry the apps\' images are built in and pulled from.')
param registryName string

@description('The name of the environment\'s right to pull from the registry: a role assignment keeps its name for life, and Azure refuses a second one for the same right.')
param registryPullAssignmentName string

// Azure's own id for the AcrPull role, the same in every subscription.
var acrPullRole = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

// The environment pulls the apps' images as itself (the apps' yaml: identity: system-environment).
resource environment 'Microsoft.App/managedEnvironments@2025-07-01' existing = {
  name: environmentName
}

// The log workspace. Key-based sign-in stays on: the environment sends its logs with the workspace's shared key.
module workspace 'br/public:avm/res/operational-insights/workspace:0.16.2' = {
  name: 'workspace'
  params: {
    name: workspaceName
    location: location
    skuName: 'PerGB2018'
    dataRetention: workspaceRetentionDays
    forceCmkForQuery: false
    features: {
      disableLocalAuth: false
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    enableTelemetry: false
  }
}

// Application Insights on the workspace. Visitors' IP addresses stay masked, as they are today.
module insights 'br/public:avm/res/insights/component:0.8.0' = {
  name: 'insights'
  params: {
    name: insightsName
    location: location
    workspaceResourceId: workspace.outputs.resourceId
    kind: 'web'
    applicationType: 'web'
    retentionInDays: insightsRetentionDays
    disableIpMasking: false
    enableTelemetry: false
  }
}

// The registry, on its Basic tier, with the environment's right to pull. No admin user and no anonymous pull: the
// environment signs in as itself. No network rules: the module's default would deny every address, and with it the
// environment's pulls, so the default action is Allow, which sends no rule set at all.
module registry 'br/public:avm/res/container-registry/registry:0.13.2' = {
  name: 'registry'
  params: {
    name: registryName
    location: location
    acrSku: 'Basic'
    acrAdminUserEnabled: false
    anonymousPullEnabled: false
    publicNetworkAccess: 'Enabled'
    networkRuleSetDefaultAction: 'Allow'
    roleAssignmentMode: 'LegacyRegistryPermissions'
    zoneRedundancy: 'Disabled'
    exportPolicyStatus: 'enabled'
    azureADAuthenticationAsArmPolicyStatus: 'enabled'
    retentionPolicyStatus: 'disabled'
    retentionPolicyDays: 7
    roleAssignments: [
      {
        name: registryPullAssignmentName
        principalId: environment.identity.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: acrPullRole
      }
    ]
    enableTelemetry: false
  }
}
