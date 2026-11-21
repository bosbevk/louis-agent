// Our own instance of main.bicep: the names the two sites' resources already have in rg-puls-za, which were made
// beside Puls and keep its name, since Azure can't rename a resource (F170, Louis's pick 2). Another business or a
// spawned site writes a file like this one with names of its own.
using 'main.bicep'

param location = 'southafricanorth'

param workspaceName = 'puls-logs'
param workspaceRetentionDays = 30

param insightsName = 'puls-insights'
param insightsRetentionDays = 90

param environmentName = 'puls-env'

param registryName = 'acrpulsza'
param registryPullAssignmentName = '3b785df2-c303-5865-adb0-94ee624325e1'
