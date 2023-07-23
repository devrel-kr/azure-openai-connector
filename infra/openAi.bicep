param name string
param location string = 'eastus'

var openai = {
  name: 'aoai-${name}'
  location: location
  skuName: 'S0'
  models: [
    {
      name: 'gpt-35-turbo-16k'
      deploymentName: 'model-gpt35turbo16k'
      version: '0613'
      skuName: 'Standard'
      skuCapacity: 240
    }
  ]
}

resource aoai 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
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

resource openaiDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for model in openai.models: {
  name: '${aoai.name}/${model.deploymentName}'
  sku: {
    name: model.skuName
    capacity: model.skuCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: model.name
      version: model.version
    }
  }
}]

output id string = aoai.id
output name string = aoai.name
output endpoint string = aoai.properties.endpoint
output apiKey string = listKeys(aoai.id, '2023-05-01').key1
