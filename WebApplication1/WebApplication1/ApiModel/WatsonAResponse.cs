using System.Collections.Generic;

namespace WebApplication1.ApiModel
{
    public class WatsonAResponse
    {
        public List<WatsonAIntent> intents;
        public List<WatsonAEntity> entities;
        public WatsonAOutput output;
        public WatsonAMessageInput input;
        public WatsonAContext context;
    }

    public class WatsonASystem
    {

    }

    public class WatsonAContext : Dictionary<string, object>
    {
        //public WatsonASystem system;
    }
    public class WatsonAMessageInput
    {
        public string text;
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

        public override bool Equals(object obj)
        {
            return entity == ((WatsonAEntity) obj).entity;
        }
    }

    public class WatsonAIntent
    {
        public string intent;
        public double confidence;
    }
}