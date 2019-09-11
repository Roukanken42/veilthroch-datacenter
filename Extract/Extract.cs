
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
                {"abnormalities", Abnormalities.Extract(root)}
            };

            result = result.ToDictionary(
                entry => entry.Key, 
                entry => AddRegionAndPatch(entry.Value, "eu", 83000)
            );

            return result;
        }

        private static IEnumerable<Dictionary<string, object>> AddRegionAndPatch(IEnumerable<Dictionary<string, object>> data, string region, int patch)
        {
            return new Transform(data)
                .Add("region", region)
                .Add("patch", patch)
                .Finish();
        }
        
        public string Json()
        {
            return JsonConvert.SerializeObject(Data());
        }
    }
}