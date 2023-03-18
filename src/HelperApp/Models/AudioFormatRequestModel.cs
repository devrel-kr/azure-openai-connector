namespace DevRelKR.OpenAIConnector.HelperApp.Models
{
    public class AudioFormatRequestModel
    {
        public virtual string Input { get; set; } = "webm";
        public virtual string Output { get; set; } = "wav";
        public virtual string InputData { get; set; }
    }
}