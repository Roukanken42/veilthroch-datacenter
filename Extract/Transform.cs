using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib;


namespace VeiltrochDatacenter.Extract
{
    public class Transform
    {
        private IEnumerable<Dictionary<string, object>> data;

        public Transform(IEnumerable<Dictionary<string, object>> data)
        {
            this.data = data;
        }

        public IEnumerable<Dictionary<string, object>> Finish()
        {
            return data;
        }
        
        public Transform Rename(string oldName, string newName)
        {    
            // This one is on Zor, blame him
            // I got chatlogs to prove it
            data = data.Select(elem =>
            {
                if (!elem.ContainsKey(oldName)) return elem;
                
                elem[newName] = elem[oldName];
                elem.Remove(oldName);
                return elem;
            });
            
            return this;
        }
        
        public Transform ToLong(string name)
        {    
            data = data.Select(elem =>
            {
                if (!elem.ContainsKey(name)) return elem;
                
                elem[name] = long.Parse((string) elem[name]);
                return elem;
            });
            
            return this;
        }
        
        public Transform ToBool(string name)
        {    
            data = data.Select(elem =>
            {
                if (!elem.ContainsKey(name)) return elem;

                var val = (string) elem[name];
                val = val.ToLower();

                
                elem[name] = val == "true" ? true : false;
                return elem;
            });
            
            return this;
        }
    }
}