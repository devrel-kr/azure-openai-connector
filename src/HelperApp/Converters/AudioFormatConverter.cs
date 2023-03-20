using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using DevRelKR.OpenAIConnector.HelperApp.Models;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DevRelKR.OpenAIConnector.HelperApp.Converters
{
    public interface IAudioFormatConverter
    {
        Task<byte[]> ConvertAsync(AudioFormatRequestModel request, ExecutionContext context);
    }

    public class AudioFormatConverter : IAudioFormatConverter
    {
        public async Task<byte[]> ConvertAsync(AudioFormatRequestModel request, ExecutionContext context)
        {
            var bytes = Convert.FromBase64String(request.InputData);

            var fncappdir = context.FunctionAppDirectory;
#if DEBUG
            var tempPath = Path.Combine(fncappdir, "Temp");
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{request.Input}");
            var voiceOut = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{request.Output}");
#else
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{request.Input}");
            var voiceOut = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.{request.Output}");
#endif
            Directory.CreateDirectory(tempPath);

            await File.WriteAllBytesAsync(voiceIn, bytes);

            var ffmpeg = Path.Combine(fncappdir,
                                      $"Tools{Path.DirectorySeparatorChar}ffmpeg{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");

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

            bytes = await File.ReadAllBytesAsync(voiceOut);
#if !DEBUG
            File.Delete(voiceIn);
            File.Delete(voiceOut);
            Directory.Delete(tempPath, recursive: true);
#endif
            return bytes;
        }
    }
}