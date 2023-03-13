using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

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

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

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
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Description = "The input file data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "audio/wav", bodyType: typeof(byte[]), Description = "The output file data")]
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

            if (req.Body.Length == 0)
            {
                return new BadRequestResult();
            }

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{input}");
            var voiceOut = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{output}");
            Directory.CreateDirectory(tempPath);

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            var bytes = Convert.FromBase64String(body);

            await File.WriteAllBytesAsync(voiceIn, bytes);

            var fncappdir = context.FunctionAppDirectory;
            var ffmpeg = Path.Combine(fncappdir, $"Tools{Path.DirectorySeparatorChar}ffmpeg.exe");

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