using System;
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

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DevRelKR.OpenAIConnector.HelperApp.Triggers
{
    public class AudioToTextHttpTrigger
    {
        private readonly IAudioToTextConverter _converter;
        private readonly ILogger<AudioToTextHttpTrigger> _logger;

        public AudioToTextHttpTrigger(IAudioToTextConverter converter, ILogger<AudioToTextHttpTrigger> log)
        {
            this._converter = converter.ThrowIfNullOrDefault();
            this._logger = log.ThrowIfNullOrDefault();
        }

        [FunctionName(nameof(AudioToTextHttpTrigger.ConvertAudioToTextAsync))]
        [OpenApiOperation(operationId: "ConvertSpeechToText", tags: new[] { "converter" }, Summary = "Converts the audio input to text", Description = "This operation converts the audio input to text based on the given locale (`en-au` by default).")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter(name: "locale", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The locale of the audio input")]
        [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(AudioToTextRequestModel), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AudioToTextResponseModel), Description = "The output file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Either request header or body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Something went wrong")]
        public async Task<IActionResult> ConvertAudioToTextAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/stt/{locale}")] HttpRequest req,
            string locale,
            ExecutionContext context)
        {
            this._logger.LogInformation("C# HTTP trigger function processed a request.");

            if (locale.IsNullOrWhiteSpace())
            {
                this._logger.LogInformation("Locale is set to default of 'en-au'.");
                locale = "en-au";
            }

            var files = req.Form.Files;
            if (files.Count == 0)
            {
                this._logger.LogError("Input audio is missing");
                return new BadRequestObjectResult("Input audio data is missing");
            }

            var stt = new AudioToTextResponseModel();
            try
            {
                var output = await this._converter.ConvertAsync(files[0], locale, context);
                stt.OutputData = output;
            }
            catch (Exception ex)
            {
                this._logger.LogError($"Error: {ex.Message}");

                return new ContentResult()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    ContentType = "text/plain",
                    Content = $"{ex.Message}\n\n\n{ex.StackTrace}",
                };
            }

            return new OkObjectResult(stt);
        }
    }
}