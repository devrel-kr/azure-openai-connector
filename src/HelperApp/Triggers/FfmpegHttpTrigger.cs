using System.IO;
using System.Net;
using System.Threading.Tasks;

using DevRelKR.OpenAIConnector.HelperApp.Configurations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

namespace DevRelKR.OpenAIConnector.HelperApp.Triggers
{
    public class FfmpegHttpTrigger
    {
        private readonly CognitiveServicesSettings _settings;
        private readonly ILogger<FfmpegHttpTrigger> _logger;

        public FfmpegHttpTrigger(CognitiveServicesSettings settings, ILogger<FfmpegHttpTrigger> log)
        {
            this._settings = settings.ThrowIfNullOrDefault();
            this._logger = log.ThrowIfNullOrDefault();
        }

        [FunctionName(nameof(FfmpegHttpTrigger.ConvertAsync))]
        [OpenApiOperation(operationId: "Convert", tags: new[] { "converter" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "The output file data")]
        public async Task<IActionResult> ConvertAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/{source}/{target}")] HttpRequest req,
            string source, string target)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}