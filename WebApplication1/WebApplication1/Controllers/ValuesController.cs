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
            // wir müsen zuerst prüfen, ob der ankommende text ein neuer Intent ist
            var result = new WatsonProvider().Query(query.Text, null, null);

            if (data != null && result.intents.Count > 0 && result.intents.Max(x => x.confidence) > 0.7 && data.Intent != result.intents.OrderByDescending(x => x.confidence).First().intent)
            {
                // es gab einen vorherigen intent und der neue intent ist anders + signifikant
                // lösche den alten Intent
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
                // es gab einen vorherigen intent und der neue ist nicht signifikant oder der neue ist der gleiche wie der alte oder der neue ist nicht existent
                // dann frage nochmal watson ab, jetzt aber mit den temporären daten
                result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent>{new WatsonAIntent{intent = data.Intent, confidence = 1 } }, data.Entities);

                // im idealfall wurden mit der Usereingabe bestimmte entitäten befüllt und im dialogflow wurde ein step vorangeschritten
                if (result.entities.Count > 0)
                {
                    // dann speichere die angaben für später
                    data.Entities.AddRange(result.entities);
                    // für was habe ich diese zeule eingebaut?? result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = data.Intent, confidence = 1} }, data.Entities);
                }
            }

            // baue ein neues storage objekt, welches den intent und die entitäten (später = context) beinhaltet
            var newData = new TempData
            {
                Intent = data?.Intent ?? result.intents.OrderByDescending(x => x.confidence).First().intent,
                Entities = result.entities ?? new List<WatsonAEntity>()
            };

            //newData.Entities.AddRange(result.entities);
            //newData.Entities = newData.Entities.Distinct().ToList();
            StaticStorage.AddData(query.SessionId, newData);

            // jetzt könnte folgendes passieren;:
            // a) wir haben einen spezielle intent welcher eine Validierung benötigt oder eine Serverseitige Aktion -> Schnittstelle für Wissensmanager
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

                    StaticStorage.AddData(query.SessionId, null);
                    


                    result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = newData.Intent, confidence = 1 } }, newData.Entities);
                }
                else
                {
                    StaticStorage.AddData(query.SessionId, null);
                    var context = new List<WatsonAEntity>(newData.Entities);
                    context.Add(new WatsonAEntity {entity = "math_request_failed_right", value = "true"});
                    var items = Calculator.Extract(result.input.text);
                    newData.Entities.Add(new WatsonAEntity { entity = "left", value = items.Item1.ToString() });
                    newData.Entities.Add(new WatsonAEntity { entity = "right", value = items.Item3.ToString() });
                    newData.Entities.Add(new WatsonAEntity { entity = "operator", value = items.Item2 });
                    result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = newData.Intent, confidence = 1 } }, context);
                }
            }
            // b) der schritt im dialogflow wurde mit "final" gekennzeichnet, was für uns heißt: Schließe diese Konversation ab ( = Lösche Daten) und führe eine Aktion durch
            //      Diese Aktion ist im idealfall im context im dialogflow spezifiziert und abhängig vom intent
            if (result.intents[0].intent == "math.request.binary" && result.context.ContainsKey("final"))
            {
                StaticStorage.AddData(query.SessionId, null);
                // Der Wissensmanager möchte, dass nun dieser Intent beendet ist. Wir führen die von ihmn spezifizierte Aktion durch. Mögliche Werte: "createLinkBasedOnContext" oder "createSolutionBasedOnCo´ntext"
                if (result.context["userResponse"].ToString() == "createLink")
                {
                    result.output.text = new List<string>{"Hier finden Sie die Lösung für ihr Problem <a href='http://google.de?q=BlaBlub'>Lösung</a>"};
                }
                else
                {
                    result.output.text = new List<string> { "Machen Sie dies und jenes um ihr Passwort zurückzusetzen" };
                }
            }

            if (result.intents[0].intent == "conversation.end")
            {
                result.context["dialog"] = new Dialog().AddTextPanel("Konnte ich Ihnen helfen?", "p1").AddButton("Ja", "yes").AddButton("Nein", "no").AddTextInput("reason");
            }

            if (result.intents[0].intent == "intent.with.selection" && result.context.ContainsKey("provideSelection") && query.PressedButton == null)
            {
                var values = (Newtonsoft.Json.Linq.JArray)result.context["value"];
                var dialog = new Dialog();
                Func<string, string> js = name =>
                {
                    if (result.context.ContainsKey("setVariable"))
                    {
                        return @"

                            $('a[name=""" + name + @"""]').parent().click(function() {
                                window.pressedButton='" + name + @"';
return false;
                            });";
                    }

                    return "";
                };

                values.Select(x => ((Newtonsoft.Json.Linq.JValue)x).Value.ToString()).ToList().ForEach(x => dialog = dialog.AddButton(x, x, js(x)));
                result.context["dialog"] = dialog;
            } else if (query.PressedButton != null)
            {
                result.context[result.context["setVariable"].ToString()] = query.PressedButton;
                result.context["system"] = null; // "system" is a NewtoinSoft Json object. ignore it because we do not need it
                result = new WatsonProvider().Query(query.Text, new List<WatsonAIntent> { new WatsonAIntent { intent = newData.Intent, confidence = 1 } }, result.context.Select(x => new WatsonAEntity{entity = x.Key, value = x.Value?.ToString()}).ToList());
            }

            var possibleKey = result.output
                .nodes_visited_details[result.output.nodes_visited_details.Count - 1].title;

            if (possibleKey != null && TranslationRepository.Instance.ContainsKey(possibleKey))
            {
                result.output.text = new List<string>
                {
                    TranslationRepository.Instance[result.output
                        .nodes_visited_details[result.output.nodes_visited_details.Count - 1].title]
                };
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
