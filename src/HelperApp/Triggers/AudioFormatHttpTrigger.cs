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
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DevRelKR.OpenAIConnector.HelperApp.Triggers
{
    public class AudioFormatHttpTrigger
    {
        private readonly CognitiveServicesSettings _settings;
        private readonly ILogger<AudioFormatHttpTrigger> _logger;

        public AudioFormatHttpTrigger(CognitiveServicesSettings settings, ILogger<AudioFormatHttpTrigger> log)
        {
            this._settings = settings.ThrowIfNullOrDefault();
            this._logger = log.ThrowIfNullOrDefault();
        }

        [FunctionName(nameof(AudioFormatHttpTrigger.ConvertFormatAsync))]
        [OpenApiOperation(operationId: "ConvertAudioFormat", tags: new[] { "converter" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(AudioFormatRequestModel), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "audio/wav", bodyType: typeof(byte[]), Description = "The output file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Either request header or body is invalid")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "Resource not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Something went wrong")]
        public async Task<IActionResult> ConvertFormatAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "convert/audio")] HttpRequest req,
            string input, string output,
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
                this._logger.LogError("Input is missing");
                return new BadRequestObjectResult("Either input or output is missing");
            }
            if (request.Output.IsNullOrWhiteSpace())
            {
                this._logger.LogError("Output is missing");
                return new BadRequestObjectResult("Either input or output is missing");
            }
            if (request.InputData.IsNullOrWhiteSpace())
            {
                this._logger.LogError("Audio is missing");
                return new BadRequestObjectResult("Input audio data is missing");
            }

            var bytes = Convert.FromBase64String(request.InputData);

            var fncappdir = context.FunctionAppDirectory;
#if DEBUG
            var tempPath = Path.Combine(fncappdir, "Temp");
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{input}");
            var voiceOut = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{output}");
#else
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{input}");
            var voiceOut = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{output}");
#endif
            Directory.CreateDirectory(tempPath);

            await File.WriteAllBytesAsync(voiceIn, bytes);

            var ffmpeg = Path.Combine(fncappdir,
                                      $"Tools{Path.DirectorySeparatorChar}ffmpeg{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = $"-i \"{voiceIn}\" \"{voiceOut}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                var process = Process.Start(psi);
                process.WaitForExit();
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