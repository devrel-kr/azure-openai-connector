using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using DevRelKR.OpenAIConnector.HelperApp.Converters;
using DevRelKR.OpenAIConnector.HelperApp.Models;

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

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DevRelKR.OpenAIConnector.HelperApp.Triggers
{
    public class AudioFormatHttpTrigger
    {
        private readonly IAudioFormatConverter _converter;
        private readonly ILogger<AudioFormatHttpTrigger> _logger;

        public AudioFormatHttpTrigger(IAudioFormatConverter converter, ILogger<AudioFormatHttpTrigger> log)
        {
            this._converter = converter.ThrowIfNullOrDefault();
            this._logger = log.ThrowIfNullOrDefault();
        }

        [FunctionName(nameof(AudioFormatHttpTrigger.ConvertFormatAsync))]
        [OpenApiOperation(operationId: "ConvertAudioFormat", tags: new[] { "converter" }, Summary = "Converts the audio format from one to the other", Description = "This operation converts the input audio file format (`webm` by default) to the given output file format (`wav` by default).")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(AudioFormatRequestModel), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "audio/wav", bodyType: typeof(byte[]), Description = "The output file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Either request header or body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Something went wrong")]
        public async Task<IActionResult> ConvertFormatAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/audio")] HttpRequest req,
            ExecutionContext context)
        {
            this._logger.LogInformation("C# HTTP trigger function processed a request.");

            if (req.Body.Length == 0)
            {
                this._logger.LogError("No request payload");
                return new BadRequestObjectResult("Request payload not found");
            }

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<AudioFormatRequestModel>(body);

            if (request.Input.IsNullOrWhiteSpace())
            {
                this._logger.LogInformation("Input is set to default of 'webm'.");
                request.Input = "webm";
            }
            if (request.Output.IsNullOrWhiteSpace())
            {
                this._logger.LogInformation("Output is set to default of 'wav'.");
                request.Output = "wav";
            }
            if (request.InputData.IsNullOrWhiteSpace())
            {
                this._logger.LogError("Input audio is missing");
                return new BadRequestObjectResult("Input audio data is missing");
            }

            var bytes = default(byte[]);
            try
            {
                bytes = await this._converter.ConvertAsync(request, context);
            }
            catch (Exception ex)
            {
                this._logger.LogError($"Error: {ex.Message}");

                return new ContentResult()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    ContentType = "text/plain",
                    Content = ex.Message,
                };
            }

            return new FileContentResult(bytes, "audio/wav");
        }
    }
}