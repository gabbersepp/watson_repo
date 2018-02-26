using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RestSharp;
using WebApplication1.ApiModel;

namespace WebApplication1
{
    public class WatsonProvider
    {
        private static string username = "";
        private static string baseUrl = "https://gateway.watsonplatform.net/conversation/api";

        public WatsonAResponse Query(string text, List<WatsonAIntent> intents, List<WatsonAEntity> entities)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest("v1/workspaces/649ef879-4c2d-4d7a-93c5-3a456c060f52/message?version=2018-02-16", Method.POST);
            request.AddHeader("Authorization", "Basic " + Base64Encode(username));

            if (intents == null)
            {
                request.AddJsonBody(new
                {
                    input = new
                    {
                        text = text
                    },
                    context = (entities ?? new List<WatsonAEntity>()).ToDictionary(x => x.entity, x => x.value)
                });
            }
            else
            {

                    request.AddJsonBody(new
                    {
                        input = new
                        {
                            text = text
                        },
                        intents = intents,
                        context = (entities ?? new List<WatsonAEntity>()).ToDictionary(x => x.entity, x => x.value)
                    });
            }

            request.AddHeader("Content-Type", "application/json; charset=UTF-8");
            var response = client.Execute(request);
            var content = response.Content;

            return JsonConvert.DeserializeObject<WatsonAResponse>(content);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}