using System.IO;
using System.Threading.Tasks;

using DevRelKR.OpenAIConnector.HelperApp.Configurations;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DevRelKR.OpenAIConnector.HelperApp.Converters
{
    public interface IAudioToTextConverter
    {
        Task<string> ConvertAsync(IFormFile input, string locale, ExecutionContext context);

        Task<string> ConvertAsync(string input, string locale);
    }

    public class AudioToTextConverter : IAudioToTextConverter
    {
        private readonly CognitiveServicesSettings _settings;

        public AudioToTextConverter(CognitiveServicesSettings settings)
        {
            this._settings = settings.ThrowIfNullOrDefault();
        }

        public async Task<string> ConvertAsync(IFormFile input, string locale, ExecutionContext context)
        {
            var fncappdir = context.FunctionAppDirectory;
#if DEBUG
            var tempPath = Path.Combine(fncappdir, "Temp");
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.wav");
#else
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var voiceIn = Path.Combine(tempPath, $"{Path.GetRandomFileName()}.wav");
#endif
            Directory.CreateDirectory(tempPath);

            using var ms = new MemoryStream();
            await input.CopyToAsync(ms);

            var bytes = ms.ToArray();
            await File.WriteAllBytesAsync(voiceIn, bytes);

            var output = await this.ConvertAsync(voiceIn, locale);
#if !DEBUG
            File.Delete(voiceIn);
            Directory.Delete(tempPath, recursive: true);
#endif
            return output;
        }

        public async Task<string> ConvertAsync(string input, string locale)
        {
            var speechConfig = SpeechConfig.FromSubscription(this._settings.Speech.ApiKey, this._settings.Speech.Region);
            speechConfig.SpeechRecognitionLanguage = locale;

            using var audioConfig = AudioConfig.FromWavFileInput(input);
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var result = await speechRecognizer.RecognizeOnceAsync();
            var output = result.Text;

            return output;
        }
    }
}