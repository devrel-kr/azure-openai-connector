using System;
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
        [OpenApiOperation(operationId: "ConvertSpeechToText", tags: new[] { "converter" }, Summary = "Converts the voice input to text", Description = "This operation converts the voice input to text based on the given locale (`en-au` by default).")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter(name: "locale", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The locale of the voice input")]
        [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(SpeechToTextRequestModel), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SpeechToTextResponseModel), Description = "The output file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Either request header or body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Something went wrong")]
        public async Task<IActionResult> ConvertSpeechToTextAsync(
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

            var fncappdir = context.FunctionAppDirectory;
#if DEBUG
            var tempPath = Path.Combine(fncappdir, "Temp");
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.wav");
#else
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.wav");
#endif
            Directory.CreateDirectory(tempPath);

            var input = files[0];

            using var ms = new MemoryStream();
            await input.CopyToAsync(ms);

            var bytes = ms.ToArray();
            await File.WriteAllBytesAsync(voiceIn, bytes);

            var stt = new SpeechToTextResponseModel();
            try
            {
                var speechConfig = SpeechConfig.FromSubscription(this._settings.Speech.ApiKey, this._settings.Speech.Region);
                speechConfig.SpeechRecognitionLanguage = locale;

                using var audioConfig = AudioConfig.FromWavFileInput(voiceIn);
                using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

                var result = await speechRecognizer.RecognizeOnceAsync();
                stt.OutputData = result.Text;
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