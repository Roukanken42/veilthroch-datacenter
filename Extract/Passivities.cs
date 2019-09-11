using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Passivities
    {
        public static IEnumerable<Dictionary<string, object>> Extract (DataCenterElement root)
        {
            Console.WriteLine("Exporting passivities...");
            var passives = Utils.FindElementsAsDicts(root, "Passivity", "Passive").ToList();

            foreach (var passive in passives)
            {
                Transform.Rename(passive, "isHidePassive", "isHidden");
                Transform.Rename(passive, "prob", "probability");
                Transform.Rename(passive, "name", "internalName");
                Transform.InferType(passive, "value");
            }
                

            var strings = Utils.FindElementsAsDicts(root, "StrSheet_Passivity", "String");
            var icons = Utils.FindElementsAsDicts(root, "PassivityIconData", "Icon").ToList();

            foreach (var icon in icons)
            {
                Transform.Rename(icon, "passivityId", "id");
                Transform.Rename(icon, "iconName", "icon");
            }
                
            var result = Utils.JoinByKey("id", passives, icons, strings);
            return result;
        }
        
        public static IEnumerable<Dictionary<string, object>> ExtractCategories(DataCenterElement root)
        {
            Console.WriteLine("Exporting passivity categories...");
            var categories = Utils.FindElementsAsDicts(root, "EquipmentEnchantData", "PassivityCategoryData", "Category").ToList();

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
    }
}