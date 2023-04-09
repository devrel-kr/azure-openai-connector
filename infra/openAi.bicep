param name string
param location string = 'southcentralus'

var openai = {
  name: 'aoai-${name}'
  location: location
  skuName: 'S0'
  models: [
    {
      name: 'gpt-35-turbo'
      deploymentName: 'model-gpt35turbo'
      version: '0301'
    }
    {
      name: 'gpt-4-32k'
      deploymentName: 'model-gpt432k'
      version: '0314'
    }
  ]
}

resource aoai 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: openai.name
  location: openai.location
  kind: 'OpenAI'
  sku: {
    name: openai.skuName
  }
  properties: {
    customSubDomainName: openai.name
    publicNetworkAccess: 'Enabled'
  }
}

resource openaiDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = [for model in openai.models: {
  name: '${aoai.name}/${model.deploymentName}'
  properties: {
    model: {
      format: 'OpenAI'
      name: model.name
      version: model.version
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}]

output id string = aoai.id
output name string = aoai.name
output endpoint string = aoai.properties.endpoint
output apiKey string = listKeys(aoai.id, '2022-12-01').key1
