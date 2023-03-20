namespace DevRelKR.OpenAIConnector.HelperApp.Models
{
    public class VoiceToTextRequestModel : AudioFormatRequestModel
    {
        public virtual string Locale { get; set; } = "en-au";
    }
}