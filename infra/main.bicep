targetScope = 'subscription'

param name string
param location string = 'Korea Central'

param apiManagementPublisherName string = 'OpenAI Power Platform Connectors'
param apiManagementPublisherEmail string = 'enquiry@devrelkorea.org'

@secure()
param gitHubUsername string
param gitHubRepositoryName string
param gitHubBranchName string = 'main'

var apps = [
  {
    isFunctionApp: false
    functionAppSuffix: ''
    apiName: 'OPENAI'
    apiPath: 'openai'
    apiServiceUrl: 'https://api.openai.com/v1'
    apiReferenceUrl: 'https://raw.githubusercontent.com/justinyoo/openai-openapi/master/openapi.{{EXTENSION}}'
    apiFormat: 'openapi-link'
    apiExtension: 'yaml'
    apiSubscription: true
    apiProduct: 'openai'
  }
  {
    isFunctionApp: false
    functionAppSuffix: ''
    apiName: 'AOAI-AUTHORING'
    apiPath: 'aoai/authoring'
    apiServiceUrl: 'https://aoai-{{AZURE_ENV_NAME}}.openai.azure.com/openai'
    apiReferenceUrl: 'https://raw.githubusercontent.com/Azure/azure-rest-api-specs/main/specification/cognitiveservices/data-plane/AzureOpenAI/authoring/stable/2022-12-01/azureopenai.{{EXTENSION}}'
    apiFormat: 'swagger-link-json'
    apiExtension: 'json'
    apiSubscription: true
    apiProduct: 'aoai'
  }
  {
    isFunctionApp: false
    functionAppSuffix: ''
    apiName: 'AOAI-COMPLETION'
    apiPath: 'aoai/completion'
    apiServiceUrl: 'https://aoai-{{AZURE_ENV_NAME}}.openai.azure.com/openai'
    apiReferenceUrl: 'https://raw.githubusercontent.com/Azure/azure-rest-api-specs/main/specification/cognitiveservices/data-plane/AzureOpenAI/inference/stable/2022-12-01/inference.{{EXTENSION}}'
    apiFormat: 'openapi+json-link'
    apiExtension: 'json'
    apiSubscription: true
    apiProduct: 'aoai'
  }
  {
    isFunctionApp: true
    functionAppSuffix: 'helper'
    apiName: 'HELPER'
    apiPath: 'helper'
    apiServiceUrl: 'https://fncapp-{{AZURE_ENV_NAME}}-{{SUFFIX}}.azurewebsites.net/api'
    apiReferenceUrl: 'https://raw.githubusercontent.com/${gitHubUsername}/${gitHubRepositoryName}/${gitHubBranchName}/infra/openapi-{{SUFFIX}}.{{EXTENSION}}'
    apiFormat: 'openapi-link'
    apiExtension: 'yaml'
    apiSubscription: true
    apiProduct: 'default'
  }
]
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

module trnsltr './translator.bicep' = {
  name: 'Translator'
  scope: rg
  params: {
    name: name
    location: 'global'
  }
}

module apim './provision-apiManagement.bicep' = {
  name: 'ApiManagement'
  scope: rg
  params: {
    name: name
    location: location
    storageContainerName: storageContainerName
    gitHubUsername: gitHubUsername
    gitHubRepositoryName: gitHubRepositoryName
    openaiApiEndpoint: aoai.outputs.endpoint
    openaiApiKey: aoai.outputs.apiKey
    translatorApiEndpoint: trnsltr.outputs.endpoint
    translatorApiKey: trnsltr.outputs.apiKey
    apiManagementPublisherName: apiManagementPublisherName
    apiManagementPublisherEmail: apiManagementPublisherEmail
    apiManagementPolicyFormat: 'xml-link'
    apiManagementPolicyValue: 'https://raw.githubusercontent.com/${gitHubUsername}/${gitHubRepositoryName}/${gitHubBranchName}/infra/apim-policy-global.xml'
  }
}

module fncapp './provision-functionApp.bicep' = [for (app, index) in apps: if (app.isFunctionApp == true) {
  name: 'FunctionApp_${app.apiName}'
  scope: rg
  dependsOn: [
    apim
  ]
  params: {
    name: name
    suffix: app.functionAppSuffix
    location: location
    storageContainerName: storageContainerName
    apimApiPath: app.apiPath
    translatorApiEndpoint: trnsltr.outputs.endpoint
    translatorApiKey: trnsltr.outputs.apiKey
  }
}]

module apis './provision-apiManagementApi.bicep' = [for (app, index) in apps: {
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
    apiManagementApiServiceUrl: replace(replace(app.apiServiceUrl, '{{AZURE_ENV_NAME}}', name), '{{SUFFIX}}', app.functionAppSuffix)
    apiManagementApiPath: app.apiPath
    apiManagementApiFormat: app.apiFormat
    apiManagementApiValue: replace(replace(app.apiReferenceUrl, '{{SUFFIX}}', app.functionAppSuffix), '{{EXTENSION}}', app.apiExtension)
    apiManagementApiPolicyFormat: 'xml-link'
    apiManagementApiPolicyValue: 'https://raw.githubusercontent.com/${gitHubUsername}/${gitHubRepositoryName}/${gitHubBranchName}/infra/apim-policy-api-${replace(toLower(app.apiName), '-', '')}.xml'
    apiManagementProductName: app.apiProduct
  }
}]
