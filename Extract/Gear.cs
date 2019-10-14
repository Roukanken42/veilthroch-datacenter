using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Gear
    {
        public static IEnumerable<Dictionary<string, object>> ExtractEnchantData (Extract extract)
        {
            Console.WriteLine(" -  enchant data");

            var data = Utils.FindElementsAsDicts(extract.Root, "EquipmentEnchantData", "EnchantData", "Enchant").ToList();

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
        
        public static IEnumerable<Dictionary<string, object>> ExtractEquipmentData (Extract extract)
        {
            Console.WriteLine(" -  equipment data");

            var data = Utils.FindElementsAsDicts(extract.Root, "EquipmentData", "Equipment").ToList();
            
            foreach (var item in data)
            {
                Transform.Rename(item,"equipmentId", "id");
                Transform.Rename(item,"countOfSlots", "crystalCount");
                Transform.Rename(item,"minAtk", "attackMin");
                Transform.Rename(item,"maxAtk", "attackMax");
                Transform.Rename(item,"def", "defense");
                Transform.Rename(item,"magicalDefence", "magicalDefense");
                Transform.Rename(item,"physicalDefence", "physicalDefense");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.Rename(item,"BasicStat", "stats");
                Transform.ToLong(item,"impact");
                Transform.ToLong(item,"balance");
                Transform.ToBool(item, "lock");
                Transform.IfHas(item, "part", o => ((string) o).ToUpper());
                Transform.IfHas(item, "type", o => ((string) o).ToUpper());
            }
            
            return data;
        }
    }
}