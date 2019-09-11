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
            var passives = new Transform(Utils.FindElementsAsDicts(root, "Passivity", "Passive"))
                .Rename("isHidePassive", "isHidden")
                .Rename("prob", "probability")
                .Rename("name", "internalName")
                .Finish()
                .ToList();
            

            var strings = Utils.FindElementsAsDicts(root, "StrSheet_Passivity", "String");
            var icons = new Transform(Utils.FindElementsAsDicts(root, "PassivityIconData", "Icon"))
                .Rename("passivityId", "id")
                .Rename("iconName", "icon")
                .Finish();
            
            var result = Utils.JoinByKey("id", passives, icons, strings);
            return result;
        }
        
        public static IEnumerable<Dictionary<string, object>> ExtractCategories(DataCenterElement root)
        {
            Console.WriteLine("Exporting passivity categories...");
            var categories = new Transform(Utils.FindElementsAsDicts(root, "EquipmentEnchantData", "PassivityCategoryData", "Category"))
                    .Finish();
            
            return categories;
        }
    }
}