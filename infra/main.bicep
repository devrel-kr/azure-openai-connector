targetScope = 'subscription'

param name string
param location string = 'Korea Central'

param apiManagementPublisherName string = 'OpenAI Power Platform Connectors'
param apiManagementPublisherEmail string = 'enquiry@devrelkorea.org'

@secure()
param gitHubUsername string
param gitHubRepositoryName string
param gitHubBranchName string = 'main'

var app = {
  apiName: 'OPENAI'
  apiPath: 'openai'
  apiFormat: 'openapi-link'
  apiExtension: 'yaml'
  apiSubscription: true
}
var storageContainerName = 'openapis'

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${name}'
  location: location
}

module aoai './openAi.bicep' = {
  name: 'OpenAI'
  scope: rg
  params: {
    name: name
    location: 'southcentralus'
  }
}

module apim './provision-apiManagement.bicep' = {
  name: 'ApiManagement'
  scope: rg
  dependsOn: [
    aoai
  ]
  params: {
    name: name
    location: location
    storageContainerName: storageContainerName
    gitHubUsername: gitHubUsername
    gitHubRepositoryName: gitHubRepositoryName
    openaiApiEndpoint: aoai.outputs.endpoint
    openaiApiKey: aoai.outputs.apiKey
    apiManagementPublisherName: apiManagementPublisherName
    apiManagementPublisherEmail: apiManagementPublisherEmail
    apiManagementPolicyFormat: 'xml-link'
    apiManagementPolicyValue: 'https://raw.githubusercontent.com/${gitHubUsername}/${gitHubRepositoryName}/${gitHubBranchName}/infra/apim-global-policy.xml'
  }
}

module api './provision-apiManagementApi.bicep' = {
  name: 'ApiManagementApi_${app.apiName}'
  scope: rg
  dependsOn: [
    apim
  ]
  params: {
    name: name
    location: location
    apiManagementApiName: app.apiName
    apiManagementApiDisplayName: app.apiName
    apiManagementApiDescription: app.apiName
    apiManagementApiSubscriptionRequired: app.apiSubscription
    apiManagementApiServiceUrl: 'https://api.openai.com/v1'
    apiManagementApiPath: app.apiPath
    apiManagementApiFormat: app.apiFormat
    apiManagementApiValue: 'https://raw.githubusercontent.com/justinyoo/openai-openapi/master/openapi.${app.apiExtension}'
    apiManagementApiPolicyFormat: 'xml-link'
    apiManagementApiPolicyValue: 'https://raw.githubusercontent.com/${gitHubUsername}/${gitHubRepositoryName}/${gitHubBranchName}/infra/apim-api-policy-${replace(toLower(app.apiName), '-', '')}.xml'
  }
}
