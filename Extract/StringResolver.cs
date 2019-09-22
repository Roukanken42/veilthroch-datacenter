using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Alkahest.Core.Data;

namespace VeiltrochDatacenter.Extract
{
    public class StringResolver
    {
        private Dictionary<string, Dictionary<int, string>> _strings = new Dictionary<string, Dictionary<int, string>>();
        private DataCenterElement _root = null;
        

        public StringResolver(DataCenterElement root)
        {
            _root = root;
            
            LoadStrings("cardsystem", "StrSheet_CardSystem", "String");
        }

        public void LoadStrings(string name, params string[] path)
        {
            var strings = new Dictionary<int, string>();
            
            var elements = Utils.FindElementsAsDicts(_root, path);
            foreach (var element in elements)
            {
                var id = (int) element["id"];
                var str = (string) element.GetValueOrDefault("string", null) ?? (string) element.GetValueOrDefault("name", null);

                strings[id] = str;
            }

            _strings[name] = strings;
        }
        
        public void LoadStringRegions(string name, params string[] path)
        {
            var regions = Utils.FindElements(_root, path);
            foreach (var region in regions)
            {
                var strings = new Dictionary<int, string>();

                var type = region.Attributes["type"].AsString;

                foreach (var str in region.Children("String"))
                {
                    if(!str.Attributes.TryGetValue("string", out var s)) continue;
                    if(!str.Attributes.TryGetValue("id", out var id)) continue;
                    
                    strings[id.AsInt32] = s.AsString;
                }
                
                _strings[name + "." + type] = strings;
            }
        }

        public string ResolveDcLink(string link)
        {
            var match = Regex.Match(link, "^@([a-z]+):([0-9]+)$");
            if (!match.Success) throw new ArgumentException("Link '" + link + "' doesn't have correct shape !");

            var name = match.Captures[1].Value;
            var id = int.Parse(match.Captures[2].Value);

            return Resolve(name, id);
        }

        public string Resolve(string name, int id)
        {
            return _strings[name][id];
        }

        public string ResolveOrDefault(string name, int id, string default_s)
        {
            return _strings[name].GetValueOrDefault(id, default_s);
        }
    }
}