using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;

namespace VeiltrochDatacenter.Extract
{
    public class Utils
    {
        public static IEnumerable<DataCenterElement> FindElements(DataCenterElement element, params string[] path)
        {
            if (path.Length == 0)
            {
                return new List<DataCenterElement>(){element};
            }

            var elements = element.Children(path.First());

            elements = path.Skip(1)
                .Aggregate(
                    elements, 
                    (current, name) => 
                        current.SelectMany(elem => elem.Children(name))
                );

            return elements;
        }
        
        public static Dictionary<string, object> ElementToDict(DataCenterElement element)
        {
            var result = element.Attributes.ToDictionary(
                entry => entry.Key, 
                entry => entry.Value.Value
            );

            foreach (var child in element.Children())
            {
                var attr = (List<Dictionary<string, object>>) result.GetValueOrDefault(child.Name, new List<Dictionary<string, object>>());
                attr.Add(ElementToDict(child));
                result[child.Name] = attr;
            }
            
            return result;
        }
        
        /***
         * Caution! First parameter is special (on purpose) - ids not existing in first won't be in result
         */
        public static IEnumerable<Dictionary<string, object>> JoinByKey(string key, params IEnumerable<IDictionary<string,object>>[] elementLists)
        {
            Dictionary<object, Dictionary<string, object>> result = null;

            foreach (var elementList in elementLists) {
                if (result == null)
                {
                    result = elementList.ToDictionary(e => e["id"], e => e.ToDictionary(e => e.Key, e => e.Value));
                    continue;
                }
                
                foreach (var element in elementList)
                {
                    if (!element.TryGetValue(key, out var id))
                        continue;
                    
                    if(!result.TryGetValue(id, out var merged))
                        continue;

                    foreach (var attribute in element) {
                        merged[attribute.Key] = attribute.Value;
                    }

                    result[id] = merged;
                }
            }

            return result.Values.ToList();
        }

        public static IEnumerable<Dictionary<string, object>> FindElementsAsDicts(DataCenterElement element, params string[] path)
        {
            return FindElements(element, path).Select(ElementToDict);
        }
    }
}