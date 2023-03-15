using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using DevRelKR.OpenAIConnector.HelperApp.Configurations;
using DevRelKR.OpenAIConnector.HelperApp.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DevRelKR.OpenAIConnector.HelperApp.Triggers
{
    public class SpeechToTextHttpTrigger
    {
        private readonly CognitiveServicesSettings _settings;
        private readonly ILogger<SpeechToTextHttpTrigger> _logger;

        public SpeechToTextHttpTrigger(CognitiveServicesSettings settings, ILogger<SpeechToTextHttpTrigger> log)
        {
            this._settings = settings.ThrowIfNullOrDefault();
            this._logger = log.ThrowIfNullOrDefault();
        }

        [FunctionName(nameof(SpeechToTextHttpTrigger.ConvertSpeechToTextAsync))]
        [OpenApiOperation(operationId: "ConvertSpeechToText", tags: new[] { "converter" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "audio/wav", bodyType: typeof(byte[]), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SpeechToTextResponseModel), Description = "The output file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Either request header or body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "Resource not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Something went wrong")]
        public async Task<IActionResult> ConvertSpeechToTextAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/stt/{locale}")] HttpRequest req,
            string locale,
            ExecutionContext context)
        {
            this._logger.LogInformation("C# HTTP trigger function processed a request.");

            if (locale.IsNullOrWhiteSpace())
            {
                this._logger.LogError("Locale is missing");
                return new NotFoundObjectResult("Locale is missing");
            }

            if (req.Body.Length == 0)
            {
                this._logger.LogError("No request payload");
                return new BadRequestObjectResult("Request payload not found");
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

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            var bytes = Convert.FromBase64String(body);

            await File.WriteAllBytesAsync(voiceIn, bytes);

            try
            {
                var speechConfig = SpeechConfig.FromSubscription(this._settings.Speech.ApiKey, this._settings.Speech.Region);        
                speechConfig.SpeechRecognitionLanguage = "en-US";

                using var audioConfig = AudioConfig.FromWavFileInput(voiceIn);
                using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

                var result = await speechRecognizer.RecognizeOnceAsync();
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

            bytes = await File.ReadAllBytesAsync(voiceOut);

            File.Delete(voiceIn);
            File.Delete(voiceOut);
            Directory.Delete(tempPath, recursive: true);

            return new FileContentResult(bytes, "audio/wav");
        }
    }
}