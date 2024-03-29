﻿namespace DevRelKR.OpenAIConnector.HelperApp.Configurations
{
    public class CognitiveServicesSettings
    {
        public const string Name = "CognitiveServices";

        public virtual TranslatorSettings Translator { get; set; }
        public virtual SpeechSettings Speech { get; set; }
    }

    public class TranslatorSettings
    {
        public virtual string ApiKey { get; set; }
        public virtual string Endpoint { get; set; }
    }

    public class SpeechSettings
    {
        public virtual string ApiKey { get; set; }
        public virtual string Region { get; set; }
    }
}