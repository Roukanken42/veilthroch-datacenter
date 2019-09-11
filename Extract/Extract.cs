
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Extract
    {
        private DataCenterElement root; 
        
        public Extract(DataCenterElement root)
        {
            this.root = root;
        }

        public Dictionary<string, IEnumerable<Dictionary<string, object>>> Data()
        {
            var result = new Dictionary<string, IEnumerable<Dictionary<string, object>>>()
            {
                {"abnormalities", Abnormalities.Extract(root)},
                {"passivities", Passivities.Extract(root)}
            };

            foreach (var data in result.Values)
            {
                AddRegionAndPatch(data, "eu", 83000);
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