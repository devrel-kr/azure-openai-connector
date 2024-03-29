param name string
param suffix string
param location string = resourceGroup().location

@secure()
param storageAccountConnectionString string

@secure()
param appInsightsInstrumentationKey string
@secure()
param appInsightsConnectionString string

@secure()
param translatorApiKey string
param translatorApiEndpoint string

@secure()
param speechApiKey string
param speechRegion string

param apimApiPath string

param consumptionPlanId string

var storage = {
  connectionString: storageAccountConnectionString
}

var appInsights = {
  instrumentationKey: appInsightsInstrumentationKey
  connectionString: appInsightsConnectionString
}

var cognitiveservices = {
  translator: {
    apiKey: translatorApiKey
    endpoint: translatorApiEndpoint
  }
  speech: {
    apiKey: speechApiKey
    region: speechRegion
  }
}

var apim = {
  name: 'apim-${name}'
  apiPath: apimApiPath
}

var consumption = {
  id: consumptionPlanId
}

var functionApp = {
  name: 'fncapp-${name}-${suffix}'
  location: location
}

resource fncapp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionApp.name
  location: functionApp.location
  kind: 'functionapp'
  properties: {
    serverFarmId: consumption.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        // Common Settings
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.instrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.connectionString
        }
        {
          name: 'AzureWebJobsStorage'
          value: storage.connectionString
        }
        {
          name: 'FUNCTION_APP_EDIT_MODE'
          value: 'readonly'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storage.connectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: functionApp.name
        }
        // OpenAPI
        {
          name: 'OpenApi__HostNames'
          // value: 'https://${apim.name}.azure-api.net/${apim.apiPath},https://${functionApp.name}.azurewebsites.net/api'
          value: 'https://${functionApp.name}.azurewebsites.net/api'
        }
        // Cognitive Services
        {
          name: 'CognitiveServices__Translator__Endpoint'
          value: cognitiveservices.translator.endpoint
        }
        {
          name: 'CognitiveServices__Translator__ApiKey'
          value: cognitiveservices.translator.apiKey
        }
        {
          name: 'CognitiveServices__Speech__ApiKey'
          value: cognitiveservices.speech.apiKey
        }
        {
          name: 'CognitiveServices__Speech__Region'
          value: cognitiveservices.speech.region
        }
      ]
    }
  }
}

var policies = [
  {
    name: 'scm'
    allow: false
  }
  {
    name: 'ftp'
    allow: false
  }
]

resource fncappPolicies 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2022-03-01' = [for policy in policies: {
  name: '${fncapp.name}/${policy.name}'
  location: functionApp.location
  properties: {
    allow: policy.allow
  }
}]

output id string = fncapp.id
output name string = fncapp.name
