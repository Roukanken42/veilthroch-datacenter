using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Glyphs
    {
        public static IEnumerable<Dictionary<string, object>> Extract (DataCenterElement root)
        {
            Console.WriteLine(" -  glyphs");
            
//            var glyphData = ExtractElements(dataCenter.Root, "CrestData", "CrestItem");
//            var glyphStrings = ExtractElements(dataCenter.Root, "StrSheet_Crest", "String");
//            var glyphs = JoinElementsByKey("id", glyphData, glyphStrings).ToList();
            
            
            var data = Utils.FindElementsAsDicts(root, "CrestData", "CrestItem").ToList();
            var strings = Utils.FindElementsAsDicts(root, "StrSheet_Crest", "String").ToList();

            foreach (var item in data)
            {
                Transform.Rename(item,"parentId", "parent");
                Transform.Rename(item,"grade", "rarity");
                Transform.Rename(item,"takePoint", "cost");
                Transform.Rename(item,"passivityLink", "passivity");
                Transform.IfHas(item, "parent", o => ((int) o) == 0 ? null: o);
            }
            
            var result = Utils.JoinByKey("id", data, strings);
            return result;
        }
    }
}