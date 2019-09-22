using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Ionic.Zip;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Passivities
    {
        private Extract extract;
        private HashSet<int> abnormalTypes;
        private HashSet<int> addableTypes;
        private HashSet<int> valueIsPercentCondition;
        private Dictionary<(int, string), string> conditionStrings;
        private Dictionary<(int, int), int?> mainStringIds;
        private Dictionary<(string, string), string> targetStrings;
        private Dictionary<string, string> valueColor;
        private Dictionary<int, (string, string)> mainStringValues;
        private Dictionary<int, string> abnormalNames;


        public Passivities(Extract extract)
        {
            this.extract = extract;
            LoadTooltipConstructionData();
        }

        public IEnumerable<Dictionary<string, object>> Extract(List<Dictionary<string, object>> abnormalities)
        {
            this.abnormalNames = abnormalities.ToDictionary(
                e => (int) e["id"],
                e => (string) e.GetValueOrDefault("name", "")
            );
            
            Console.WriteLine(" -  passivities");
            var passives = Utils.FindElementsAsDicts(extract.Root, "Passivity", "Passive").ToList();

            foreach (var passive in passives)
            {
                var generatedTooltip = GenerateTooltip(passive);
                
                Transform.Rename(passive, "isHidePassive", "isHidden");
                Transform.Rename(passive, "prob", "probability");
                Transform.Rename(passive, "name", "internalName");
                Transform.InferType(passive, "value");

                var condition = Transform.Collect(passive, "condition", "conditionValue", "conditionCategory");
                Transform.Rename(condition, "condition", "id");
                Transform.Rename(condition, "conditionValue", "value");
                Transform.Rename(condition, "conditionCategory", "Category");

                passive["condition"] = condition;
                
                passive["generatedTooltip"] = generatedTooltip;
            }
                

            var strings = Utils.FindElementsAsDicts(extract.Root, "StrSheet_Passivity", "String");
            var icons = Utils.FindElementsAsDicts(extract.Root, "PassivityIconData", "Icon").ToList();

            foreach (var icon in icons)
            {
                Transform.Rename(icon, "passivityId", "id");
                Transform.Rename(icon, "iconName", "icon");
            }
                
            var result = Utils.JoinByKey("id", passives, icons, strings);
            return result;
        }


        public static IEnumerable<Dictionary<string, object>> ExtractCategories(Extract extract)
        {
            Console.WriteLine(" -  passivity categories");
            var categories = Utils.FindElementsAsDicts(extract.Root, "EquipmentEnchantData", "PassivityCategoryData", "Category").ToList();

            foreach (var category in categories)
            {
                Transform.Rename(category, "unchangeable", "isRollable");
                Transform.IfHas(category, "isRollable", val => !(bool) val);
                
                Transform.ToIntList(category, "passivityLink", ',');
//                Transform.ToLinks(category, "passivityLink", "passivities");
                Transform.Rename(category, "passivityLink", "passivities");
            }
            return categories;
        }

        private string GenerateTooltip(IReadOnlyDictionary<string, object> passive)
        {
            var type = (int) passive["type"];
            var condition = (int) passive["condition"];
            var conditionValue = (string) passive["conditionValue"];

            var prob = (float) passive["prob"];
            var targetSpeciesId = (string) passive.GetValueOrDefault("targetSpeciesId", null);
            string targetSpecies = null;

            if (targetSpeciesId != null)
            {
                var id = int.Parse(targetSpeciesId);
                targetSpecies = extract.Strings.ResolveOrDefault("species", id, null);
            }

            
            
            var mainStringId = mainStringIds.GetValueOrDefault((type, condition), null)
                      ?? mainStringIds.GetValueOrDefault((type, -1), null);

            if (mainStringId == null)
                // Didn't find 'main string' template, quiting creation
                return null;

            var mainString = mainStringValues[mainStringId ?? 0].Item2;
            
            var mobSize = (string) passive.GetValueOrDefault("mobSize", "");
            var state = mainStringValues[mainStringId ?? 0].Item1;
            
            var data = new Dictionary<string, string>();

            data["type"] = extract.Strings.Resolve("passive.type", type);
            data["prob"] = prob == 1 ? "": extract.Strings.Resolve("passive.prob", 1);
            data["probValue"] = (prob * 100).ToString("g");
            data["condition"] = conditionStrings.GetValueOrDefault((condition, conditionValue), "")
                .Replace("{value}", "{conditionValue}");
            data["target"] = targetStrings.GetValueOrDefault((mobSize, state), "");

            data["value"] = "TODO";
            
            if (abnormalTypes.Contains(type))
            {
                var id = int.Parse((string) passive["value"]);
                data["abnormal"] = abnormalNames.GetValueOrDefault(id, "");
            }
            
            var res = ResolveTemplate(mainString, data);
            if (targetSpecies != null)
                res = "[" + targetSpecies + "] " + res; 
            
            return res;
        }

        private static string ResolveTemplate(string template, Dictionary<string, string> data)
        {
            var result = template;

            foreach (var pair in data)
            {
                var key = "{" + pair.Key + "}";
                if (!result.Contains(key) || pair.Value == null) continue;
                
                result = result.Replace(
                    key,
                    ResolveTemplate(pair.Value, data)
                );
            }

            return result;
        }
        
        private void LoadTooltipConstructionData()
        {
            extract.Strings.LoadStrings("species", "StrSheet_Species", "String");
            extract.Strings.LoadStringRegions("passive", "StrSheet_PassiveMainString", "StringGroup");
            extract.Strings.LoadStringRegions("passive", "StrSheet_PassiveStatsDefine", "StringGroup");

            var config = extract.Root.Children("PassiveStatsDefine").First();

            this.abnormalTypes = config.Children("AbnormalTooltipType")
                .First()
                .Children("Type")
                .Select(elem => elem.Attributes["id"].AsInt32)
                .ToHashSet();
            
            this.addableTypes = config.Children("AddableType")
                .First()
                .Children("Type")
                .Select(elem => elem.Attributes["id"].AsInt32)
                .ToHashSet();

            this.valueIsPercentCondition = config.Children("ConditionValueAsPercent")
                .First()
                .Children("Condition")
                .Select(elem => elem.Attributes["id"].AsInt32)
                .ToHashSet();

            this.conditionStrings = config.Children("ConditionStringDefine")
                .First()
                .Children("String")
                .ToDictionary(
                    elem => (elem["conditionId"].AsInt32, elem["conditionValue"].ToString()),
                    elem => extract.Strings.Resolve("passive.condition", elem["id"].AsInt32)
                );
            
            this.mainStringIds = config.Children("MainStringDefine")
                .First()
                .Children("MainString")
                .ToDictionary(
                    elem => (elem["typeId"].AsInt32, elem["conditionId"].AsInt32),
                    elem => (int?) elem["mainStringId"].AsInt32
                );
            
            this.mainStringValues = extract.Root.Children("StrSheet_PassiveMainString")
                .First()
                .Children("StringGroup")
                .First(e => e.AttributeOrDefault("type", "") == "mainString")
                .Children("String")
                .ToDictionary(
                    elem => elem["id"].AsInt32,
                    elem => (elem["name"].AsString, elem["string"].AsString)
                );
            
            this.targetStrings = config.Children("TargetStringDefine")
                .First()
                .Children("String")
                .ToDictionary(
                    elem => (elem["mobSize"].AsString, elem["state"].AsString),
                    elem => extract.Strings.Resolve("passive.target", elem["id"].AsInt32)
                );
            
            this.valueColor = config.Children("ValueColorDefine")
                .First()
                .Children("String")
                .ToDictionary(
                    elem => elem["typeId"].ToString(),
                    elem => elem["valueType"].AsString
                );
        }
    }
}