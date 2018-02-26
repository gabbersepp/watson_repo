using System.Collections.Generic;

namespace WebApplication1.ApiModel
{
    public class WatsonAResponse
    {
        public List<WatsonAIntent> intents;
        public List<WatsonAEntity> entities;
        public WatsonAOutput output;
    }

    public class WatsonAOutput
    {
        public List<string> text;

    }
    public class WatsonAEntity
    {
        public string entity;
        public double confidence;
        public string value;
    }

    public class WatsonAIntent
    {
        public string intent;
        public double confidence;
    }
}