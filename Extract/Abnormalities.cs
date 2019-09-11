using System;
using System.Collections.Generic;
using System.Linq;
using Alkahest.Core.Data;
using Newtonsoft.Json;

namespace VeiltrochDatacenter.Extract
{
    public class Abnormalities
    {
        public static IEnumerable<Dictionary<string, object>> Extract (DataCenterElement root)
        {
            Console.WriteLine("Exporting abnormalities...");
            
            var abnormalKinds = Utils.FindElementsAsDicts(root, "StrSheet_AbnormalityKind", "String")
                .ToDictionary(elem => elem["id"], elem => elem);

            var abnormals = Utils.FindElementsAsDicts(root, "Abnormality", "Abnormal").ToList();

            foreach (var abnormal in abnormals)
            {
                Transform.Rename(abnormal, "AbnormalityEffect", "effects");
                Transform.Rename(abnormal, "time", "duration");
                Transform.Rename(abnormal, "infinity", "isInfinite");
                Transform.ToBool(abnormal, "isShow");
                Transform.IfHas(abnormal, "isShow", val => !(bool) val);
                Transform.Rename(abnormal, "isShow", "isHidden");
                Transform.Rename(abnormal, "isHideOnRefresh", "isHiddenOnRefresh");
                Transform.ToLong(abnormal, "duration");
//                .ToArray("bySkillCategory")

                if (abnormal.TryGetValue("kind", out var kind))
                {
                    abnormal["kind"] = abnormalKinds.GetValueOrDefault(kind, null);
                }
            }

            abnormals.Select(a => (List<Dictionary<string, object>>) a["effects"]).SelectMany(a => a);
            

            var strings = Utils.FindElementsAsDicts(root, "StrSheet_Abnormality", "String");
            var icons = Utils.FindElementsAsDicts(root, "AbnormalityIconData", "Icon").ToList();

            foreach (var icon in icons)
            {
                Transform.Rename(icon, "abnormalityId", "id");
                Transform.Rename(icon, "iconName", "icon");
            }
            

            var result = Utils.JoinByKey("id", abnormals, icons, strings);
            return result;
        }
    }
}