using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib;
using Octokit;


namespace VeiltrochDatacenter.Extract
{
    public class Transform
    {
        public static void Rename(Dictionary<string, object> elem, string oldName, string newName)
        {
            if (!elem.ContainsKey(oldName)) return;

            elem[newName] = elem[oldName];
            elem.Remove(oldName);
        }

        public static void ToLong(Dictionary<string, object> elem, string name)
        {
            if (!elem.ContainsKey(name)) return;
            elem[name] = long.Parse((string) elem[name]);
        }

        public static void ToBool(Dictionary<string, object> elem, string name)
        {
            if (!elem.ContainsKey(name)) return;

            var val = (string) elem[name];
            val = val.ToLower();

            elem[name] = val == "true" ? true : false;
        }
    

        private static object InferTypeHelper(string value)
        {
            if (long.TryParse(value, out var integer)) return integer;
            if (double.TryParse(value, out var fractional)) return fractional;
            if (value.ToLower() == "true") return true;
            if (value.ToLower() == "false") return false;
            return value;
        }
            
        public static void InferType(Dictionary<string, object> elem, string name)
        {    
            if (!elem.ContainsKey(name)) return ;
            elem[name] = InferTypeHelper((string) elem[name]);
        }
        
        public delegate object SimpleTransform(object x);

        public static void IfHas(Dictionary<string, object> elem, string name, SimpleTransform f)
        {
            if (elem.ContainsKey(name))
                elem[name] = f(elem[name]);
        }        
    }
}