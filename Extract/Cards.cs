using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Cards
    {
        public static IEnumerable<Dictionary<string, object>> ExtractCards (Extract extract)
        {
            Console.WriteLine(" -  cards");

            var templates = Utils.FindElementsAsDicts(extract.root, "CardTemplateData", "CardTemplate").ToList();

            foreach (var template in templates)
            {
                Transform.IfHas(template, "effect", o => ParseImageUrl((string) o));
                Transform.IfHas(template, "image", o => ParseImageUrl((string) o));
                Transform.Rename(template, "collectionBookPoint", "exp"); // TODO: check if correct !
                Transform.Rename(template, "needAmountForActivation", "itemsNeeded");
                Transform.Rename(template, "superiorCardItemId", "superiorCard");

                Transform.ToNullable(template, "effect");
                Transform.ToNullable(template, "superiorCard");
                
                var cardPassive = (List<Dictionary<string, object>>) template.GetValueOrDefault("CardPassive", new List<Dictionary<string, object>>());
                var passivities = (List<Dictionary<string, object>>) cardPassive[0].GetValueOrDefault("Passivity", new List<Dictionary<string, object>>());
                var passivityIds = passivities.Select(d => d["id"]);

                template.Remove("CardPassive");
                template["passivities"] = passivityIds;
            }


            var strings = Utils.FindElementsAsDicts(extract.root, "StrSheet_Card", "String").ToList();

            foreach (var s in strings)
            {
                Transform.Rename(s, "tooltip1", "lore");
                Transform.Rename(s, "tooltip2", "source");
            }
            
            var result = Utils.JoinByKey("id", templates, strings);
            return result;
        }
        
        public static IEnumerable<Dictionary<string, object>> ExtractCombines (Extract extract)
        {
            Console.WriteLine(" -  cards combines");

            var combines = Utils.FindElementsAsDicts(extract.root, "CardCombineList", "CombineList").ToList();

            foreach (var combine in combines)
            {
                Transform.ToIntList(combine, "cardCategory", ',');
                Transform.Rename(combine, "cardCategory", "categories");

                var basePassivities = (List<Dictionary<string, object>>) combine.GetValueOrDefault("BasePassivity", new List<Dictionary<string, object>>());
                var bonusPassivities = (List<Dictionary<string, object>>) combine.GetValueOrDefault("BonusPassivity", new List<Dictionary<string, object>>());
                
                combine.Remove("BasePassivity");
                combine.Remove("BonusPassivity");
                var passivities = basePassivities.Concat(bonusPassivities);

                foreach (var passivity in passivities)
                {
                    Transform.Rename(passivity, "id", "passivity");
                }
                
                combine["effects"] = passivities;
            }


            var strings = Utils.FindElementsAsDicts(extract.root, "StrSheet_CardCombineList", "String").ToList();

            var result = Utils.JoinByKey("id", combines, strings);
            return result;
        }

        private static string ParseImageUrl(string url)
        {
            url = url.Replace("img://__", "");
            url = url.Replace(".", "/");
            
            return "datacenter/" + url.ToLower() + ".png";
        }
    }
}