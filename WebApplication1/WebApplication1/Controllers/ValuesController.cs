using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public WatsonAResponse Query([FromBody]WatsonCQuery query)
        {
            var data = StaticStorage.GetData<TempData>(query.SessionId);
            var result = new WatsonProvider().Query(query.Text, null, null);

            if (data != null && result.intents.Max(x => x.confidence) > 0.7 && data.Intent != result.intents.OrderByDescending(x => x.confidence).First().intent)
            {
                // es gab einen vorherigen intent und der neue intent ist anders + signifikant
                StaticStorage.AddData(query.SessionId, null);
                data = null;
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
            newData.Entities = newData.Entities.Distinct().ToList();
            StaticStorage.AddData(query.SessionId, newData);

            if (result.intents[0].intent == "math.request.binary2")
            {
                if (Calculator.IsValid(result.input.text))
                {
                    var items = Calculator.Extract(result.input.text);

                    var left = Double.Parse(result.context.ContainsKey("left") ? result.context["left"].ToString() : items.Item1.ToString());
                    var op = result.context.ContainsKey("operator") ? result.context["operator"] : items.Item2;
                    double? res;

                    if (result.context.ContainsKey("operator"))
                    {
                        // wir kommen von einem vorherigen invaliden berechnungsergebnis
                        res = Calculator.Calc(left, result.input.text, op.ToString());
                        newData.Entities.Add(new WatsonAEntity { entity = "right", value = result.input.text.ToString() });
                    }
                    else
                    {
                        res = Calculator.Calc(result.input.text);
                        newData.Entities.Add(new WatsonAEntity { entity = "right", value = items.Item3.ToString() });
                    }
                    
                    newData.Entities.Add(new WatsonAEntity { entity = "math_request_success", value = "true" });
                    newData.Entities.Add(new WatsonAEntity { entity = "result", value = res.ToString() });
                    newData.Entities.Add(new WatsonAEntity { entity = "left", value = left.ToString() });
                    
                    newData.Entities.Add(new WatsonAEntity { entity = "operator", value = op.ToString() });

                    newData.Entities.Add(new WatsonAEntity{entity = "sessionId", value=new Random().NextDouble().ToString()});


                    result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = newData.Intent, confidence = 1 } }, newData.Entities);
                }
                else
                {
                    var context = new List<WatsonAEntity>(newData.Entities);
                    context.Add(new WatsonAEntity {entity = "math_request_failed_right", value = "true"});
                    var items = Calculator.Extract(result.input.text);
                    newData.Entities.Add(new WatsonAEntity { entity = "left", value = items.Item1.ToString() });
                    newData.Entities.Add(new WatsonAEntity { entity = "right", value = items.Item3.ToString() });
                    newData.Entities.Add(new WatsonAEntity { entity = "operator", value = items.Item2 });
                    result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = newData.Intent, confidence = 1 } }, context);
                }
            }
            return result;
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
