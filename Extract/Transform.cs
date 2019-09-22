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
        
        public static void ToIntList(Dictionary<string, object> elem, string name, params char[] separators)
        {
            if (!elem.ContainsKey(name)) return;

            var values = ((string) elem[name]).Split(separators);
            elem[name] = values.Select(long.Parse).ToList();
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
        
        private static bool IsDefault(object value)
        {
            return value switch
            {
                int i => i == 0,
                float f => Math.Abs(f) < 0.0000001,
                string s => s == "",
                _ => false
            };
        }
            
        public static void ToNullable(Dictionary<string, object> elem, string name)
        {    
            if (!elem.ContainsKey(name)) return ;
            if (IsDefault(elem[name]))
            {
                elem[name] = null;
            }
        }
        
        public delegate object SimpleTransform(object x);

        public static void IfHas(Dictionary<string, object> elem, string name, SimpleTransform f)
        {
            if (elem.ContainsKey(name))
                elem[name] = f(elem[name]);
        }
        
        public static void ToLinks(Dictionary<string, object> elem, string name, string linkTo)
        {    
            if (!elem.ContainsKey(name)) return;
            var elems = (List<long>) elem[name];

            elem[name] = elems.Select(id => new Dictionary<string, object>
            {
                {"linkTo", linkTo},
                {"id", id}
            }).ToList();
        }

        public static Dictionary<string, object> Collect(Dictionary<string, object> elem, params string[] names)
        {
            var result = new Dictionary<string, object>();

            foreach (var name in names)
            {
                if (elem.TryGetValue(name, out var val))
                    result[name] = val;
            }

            return result;
        }
    }
}