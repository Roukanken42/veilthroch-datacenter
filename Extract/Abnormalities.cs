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
            var abnormals = new Transform(Utils.FindElementsAsDicts(root, "Abnormality", "Abnormal"))
                .Rename("AbnormalityEffect", "effects")
                .Rename("time", "duration")
                .Rename("infinity", "isInfinite")
                .Rename("isShow", "isVisible")
                .Rename("isHideOnRefresh", "doesHideOnRefresh")
                .ToLong("duration")
                .ToBool("isVisible")
//                .ToArray("bySkillCategory")
                .Finish()
                .ToList();
            

            var strings = Utils.FindElementsAsDicts(root, "StrSheet_Abnormality", "String");
            var icons = new Transform(Utils.FindElementsAsDicts(root, "AbnormalityIconData", "Icon"))
                .Rename("abnormalityId", "id")
                .Rename("iconName", "icon")
                .Finish();


            var abnormalKinds = Utils.FindElementsAsDicts(root, "StrSheet_AbnormalityKind", "String")
                .ToDictionary(elem => elem["id"], elem => elem);
            

            foreach (var abnormal in abnormals)
            {
                if (!abnormal.TryGetValue("kind", out var kind)) continue;
                {
                    abnormal["kind"] = abnormalKinds.GetValueOrDefault(abnormal["id"], null);
                }
            }

            var result = Utils.JoinByKey("id", abnormals, icons, strings);
            return result;
        }
    }
}