param name string
param suffix string
param location string = resourceGroup().location

param storageContainerName string
param apimApiPath string

param translatorApiEndpoint string
@secure()
param translatorApiKey string

var shortname = '${name}${replace(suffix, '-', '')}'
var longname = '${name}-${suffix}'

module st './storageAccount.bicep' = {
  name: 'StorageAccount_FunctionApp_${suffix}'
  params: {
    name: shortname
    location: location
    storageContainerName: storageContainerName
  }
}

module wrkspc './logAnalyticsWorkspace.bicep' = {
  name: 'LogAnalyticsWorkspace_FunctionApp_${suffix}'
  params: {
    name: longname
    location: location
  }
}

module appins './applicationInsights.bicep' = {
  name: 'ApplicationInsights_FunctionApp_${suffix}'
  params: {
    name: longname
    location: location
    workspaceId: wrkspc.outputs.id
  }
}

module csplan './consumptionPlan.bicep' = {
  name: 'ConsumptionPlan_FunctionApp_${suffix}'
  params: {
    name: longname
    location: location
  }
}

module fncapp './functionApp.bicep' = {
  name: 'FunctionApp_FunctionApp_${suffix}'
  params: {
    name: name
    suffix: suffix
    location: location
    apimApiPath: apimApiPath
    storageAccountConnectionString: st.outputs.connectionString
    appInsightsInstrumentationKey: appins.outputs.instrumentationKey
    appInsightsConnectionString: appins.outputs.connectionString
    translatorApiEndpoint: translatorApiEndpoint
    translatorApiKey: translatorApiKey
    consumptionPlanId: csplan.outputs.id
  }
}
