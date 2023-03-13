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
using Microsoft.Azure.WebJobs.Host;
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
        [OpenApiParameter(name: "input", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The input file format")]
        [OpenApiParameter(name: "output", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The output file format")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "The output file data")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Either request header or body is invalid")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found")]
        public async Task<IActionResult> ConvertAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/{input}/{output}")] HttpRequest req,
            string input, string output,
            ExecutionContext context)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (input.IsNullOrWhiteSpace())
            {
                return new NotFoundResult();
            }
            if (output.IsNullOrWhiteSpace())
            {
                return new NotFoundResult();
            }

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            if (body.IsNullOrWhiteSpace())
            {
                return new BadRequestResult();
            }

            string name = req.Query["name"];

            var fncappdir = context.FunctionAppDirectory;
            var fncdir = context.FunctionDirectory;
            var ffmpeg = Path.Combine(fncappdir, "Tools/ffmpeg.exe");
            var exists = File.Exists(ffmpeg);

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