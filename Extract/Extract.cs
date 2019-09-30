
using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Extract
    {
        
        public DataCenterElement Root;
        public StringResolver Strings;
        
        public Extract(DataCenterElement root)
        {
            this.Root = root;
            this.Strings = new StringResolver(root);
        }

        public Dictionary<string, IEnumerable<Dictionary<string, object>>> Data()
        {
            Console.WriteLine("Exporting...");
            
            var abnormalities = Abnormalities.Extract(this).ToList();
            
            var result = new Dictionary<string, IEnumerable<Dictionary<string, object>>>()
            {
//                {"abnormalities", abnormalities},
                {"passivities", new Passivities(this).Extract(abnormalities)},
//                {"passivityCategories", Passivities.ExtractCategories(this)},
//                {"glyphs", Glyphs.Extract(this)},
//                {"enchantData", Gear.ExtractEnchantData(this)},
//                {"equipmentData", Gear.ExtractEquipmentData(this)},
                {"cards", Cards.ExtractCards(this)},
                {"card_combines", Cards.ExtractCombines(this)},
            };

            foreach (var data in result.Values)
            {
                AddRegionAndPatch(data, "kr", 87);
            }

            return result;
        }

        private static void AddRegionAndPatch(IEnumerable<Dictionary<string, object>> data, string region, int patch)
        {
            foreach (var obj in data)
            {
                obj["region"] = region;
                obj["patch"] = patch;
            }
        }
        
        public string Json()
        {
            return JsonConvert.SerializeObject(Data());
        }
    }
}