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
    public class VoiceToTextHttpTrigger
    {
        private readonly IAudioFormatConverter _af;
        private readonly IAudioToTextConverter _att;
        private readonly ILogger<VoiceToTextHttpTrigger> _logger;

        public VoiceToTextHttpTrigger(IAudioFormatConverter af, IAudioToTextConverter att, ILogger<VoiceToTextHttpTrigger> log)
        {
            this._af = af.ThrowIfNullOrDefault();
            this._att = att.ThrowIfNullOrDefault();
            this._logger = log.ThrowIfNullOrDefault();
        }

        [FunctionName(nameof(VoiceToTextHttpTrigger.ConvertVoiceToTextAsync))]
        [OpenApiOperation(operationId: "ConvertVoiceToTextAsync", tags: new[] { "converter" }, Summary = "Voice to text", Description = "This operation converts the voice input to text based on the given locale (`en-au` by default).")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(VoiceToTextRequestModel), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(VoiceToTextResponseModel), Description = "The output file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Either request header or body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Something went wrong")]
        public async Task<IActionResult> ConvertVoiceToTextAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/voice")] HttpRequest req,
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
            var request = JsonConvert.DeserializeObject<VoiceToTextRequestModel>(body);

            if (request.Input.IsNullOrWhiteSpace())
            {
                this._logger.LogInformation("Input is set to default of 'webm'.");
                request.Input = "webm";
            }
            if (request.Locale.IsNullOrWhiteSpace())
            {
                this._logger.LogInformation("Locale is set to default of 'en-au'.");
                request.Locale = "en-au";
            }
            if (request.InputData.IsNullOrWhiteSpace())
            {
                this._logger.LogError("Input audio is missing");
                return new BadRequestObjectResult("Input audio data is missing");
            }

            var fncappdir = context.FunctionAppDirectory;
#if DEBUG
            var tempPath = Path.Combine(fncappdir, "Temp");
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.wav");
#else
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.wav");
#endif
            Directory.CreateDirectory(tempPath);

            var stt = new AudioToTextResponseModel();
            try
            {
                var bytes = await this._af.ConvertAsync(request, context);

                await File.WriteAllBytesAsync(voiceIn, bytes);

                var output = await this._att.ConvertAsync(voiceIn, request.Locale);

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
#if !DEBUG
            File.Delete(voiceIn);
            Directory.Delete(tempPath, recursive: true);
#endif
            return new OkObjectResult(stt);
        }
    }
}