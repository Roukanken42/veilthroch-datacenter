using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Gear
    {
        public static IEnumerable<Dictionary<string, object>> ExtractEnchantData (DataCenterElement root)
        {
            Console.WriteLine(" -  enchant data");

            var data = Utils.FindElementsAsDicts(root, "EquipmentEnchantData", "EnchantData", "Enchant").ToList();

            foreach (var item in data)
            {
                Transform.Rename(item,"Effect", "effects");
                Transform.Rename(item,"BasicStat", "stats");

                var stats = (List<Dictionary<string, object>>) item.GetValueOrDefault("stats", new List<Dictionary<string, object>>());
                foreach (var stat in stats)
                {
                    Transform.Rename(stat,"enchantStep", "step");
                    Transform.Rename(stat,"kind", "stat");
                    Transform.IfHas(stat, "stat", o => ((string) o).ToUpper());
                }
                
                var effects = (List<Dictionary<string, object>>) item.GetValueOrDefault("effects", new List<Dictionary<string, object>>());
                foreach (var effect in effects)
                {
                    Transform.Rename(effect,"passivityCategoryId", "passivityCategory");
                }
            }
            
            return data;
        }
    }
}