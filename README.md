# OpenAI Connector

This is a Power Platform custom connector project for OpenAI API and Azure OpenAI Service API.


## Prerequisites

- [OpenAI API Key](https://platform.openai.com/account/api-keys)
- [Azure OpenAI Service Subscription](https://aka.ms/oai/access)
- [Azure CLI](https://learn.microsoft.com/cli/azure/what-is-azure-cli?WT.mc_id=dotnet-91706-juyoo)
- [Azure Dev CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview?WT.mc_id=dotnet-91706-juyoo)
- [GitHub CLI](https://cli.github.com)


## Getting Started

> For local development on various OS, please refer to [this document](./tools/README.md) downloading ffmpeg.

### App Provisioning & Deployment

1. Fork this repository to your account.
1. To provision resources, run the commands in the following order:

   ```bash
   # PowerShell
   $AZURE_ENV_NAME = "pp$(Get-Random -Max 9999)"
   $GITHUB_USERNAME = "{{GITHUB_USERNAME}}"

   # Bash
   AZURE_ENV_NAME="pp$RANDOM"
   GITHUB_USERNAME="{{GITHUB_USERNAME}}"
   ```

   ```bash
   azd auth login
   azd init -e $AZURE_ENV_NAME
   azd env set GITHUB_USERNAME $GITHUB_USERNAME
   azd env set GITHUB_REPOSITORY_NAME azure-openai-connector
   azd up
   ```

1. To deploy apps, run the commands in the following order:

   ```bash
   gh auth login
   azd pipeline config
   gh workflow run "Azure Dev" --repo $GITHUB_USERNAME/azure-openai-connector
   ```


### Power Platform Custom Connector

1. Import `https://github.com/devrel-kr/openai-connector/blob/main/infra/swagger-aoai.json`
1. Create a connection


## Sample Apps

- [Power Apps](https://learn.microsoft.com/power-apps/powerapps-overview?WT.mc_id=dotnet-91706-juyoo): [https://youtu.be/jO1HLjsA9RQ](https://youtu.be/jO1HLjsA9RQ)
- [Blazor WASM](https://learn.microsoft.com/aspnet/core/blazor/hosting-models?WT.mc_id=dotnet-91706-juyoo#blazor-webassembly) on [Azure Static Web Apps](https://learn.microsoft.com/azure/static-web-apps/overview?WT.mc_id=dotnet-91706-juyoo): TBD
- [Blazor Hybrid](https://learn.microsoft.com/aspnet/core/blazor/hybrid/?WT.mc_id=dotnet-91706-juyoo): TBD
