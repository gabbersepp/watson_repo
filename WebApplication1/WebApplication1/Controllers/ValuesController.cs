using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.ApiModel;
using WebApplication1.ControllerModel;

namespace WebApplication1.Controllers
{
    public class ValuesController : Controller
    {

        [Route("api/watson/query")]
        [HttpPost]
        public void Query(WatsonCQuery query)
        {
            var data = StaticStorage.GetData<TempData>(query.SessionId);
            var result = new WatsonProvider().Query(query.Text, null, null);

            if (data != null && result.intents.Max(x => x.confidence) > 0.5 && data.Intent != result.intents.OrderByDescending(x => x.confidence).First().intent)
            {
                // es gab einen vorherigen intent und der neue intent ist anders + signifikant
                StaticStorage.AddData(query.SessionId, null);
            }
            else if (data == null)
            {
                // es gab keinen vorherigen intent
                data = new TempData();

                
            }
            else
            {
                // es gab einen vorherigen intent und der neue ist nicht signifikant
                result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent>{new WatsonAIntent{intent = data.Intent, confidence = 1 } }, data.Entities);
                if (result.entities.Count > 0)
                {
                    data.Entities.AddRange(result.entities);
                    result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = data.Intent, confidence = 1} }, data.Entities);
                }
            }

            var newData = new TempData
            {
                Intent = data?.Intent ?? result.intents.OrderByDescending(x => x.confidence).First().intent,
                Entities = result.entities ?? new List<WatsonAEntity>()
            };
            newData.Entities.AddRange(result.entities);
            StaticStorage.AddData(query.SessionId, newData);

            
        }

        [Route("api/watson/test")]
        [HttpGet]
        public void test(string text, string sessionId)
        {
            Query(new WatsonCQuery{Text = text, SessionId = sessionId});
        }

        class TempData
        {
            public string Intent;
            public List<WatsonAEntity> Entities;
        }
    }
}
