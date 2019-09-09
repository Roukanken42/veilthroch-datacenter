
using System.Collections.Generic;
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

        public Dictionary<string, object> Data()
        {
            return new Dictionary<string, object>()
            {
                {"abnormalities", Abnormalities.Extract(root)}
            };
        }
        
        public string Json()
        {
            return JsonConvert.SerializeObject(Data());
        }
    }
}