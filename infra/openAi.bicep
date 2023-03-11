param name string
param location string = 'southcentralus'

var openai = {
  name: 'aoai-${name}'
  location: location
  skuName: 'S0'
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

resource openaiDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = {
  name: '${aoai.name}/model-gpt35turbo'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0301'
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}

output id string = aoai.id
output name string = aoai.name
output endpoint string = aoai.properties.endpoint
output apiKey string = listKeys(aoai.id, '2022-12-01').key1
