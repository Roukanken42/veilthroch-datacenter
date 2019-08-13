using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Alkahest.Core.Data;
using Alkahest.Packager;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Ionic.Zlib;
using Jitbit.Utils;

namespace VeiltrochDatacenter {
    internal class Program {
        private static IEnumerable<int> ProcessLink(object link, char separator = ',')
        {
            return link switch {
                int id => new List<int>{id},
                string arrayString => 
                    arrayString
                        .Split(';', ',')
                        .Where(e => !string.IsNullOrEmpty(e))
                        .Select(int.Parse),
                _ => throw new ArgumentException("Link attribute is not processable")
            };
        }

        public static async Task Main(string[] args) {
            var manager = new AssetManager();
            manager.UpdateAll();

            var dc = manager._dataCenters.First();
            var dataCenter = new DataCenter(dc.File.Open(FileMode.Open), DataCenterMode.Persistent, DataCenterStringOptions.None);
            Console.WriteLine("Loaded DC");
            
            
            var itemData = ExtractElements(dataCenter.Root, "ItemData", "Item").ToList();
            var itemStrings = ExtractElements(dataCenter.Root, "StrSheet_Item", "String");
            Console.WriteLine("Extracting ItemData@maxEnchant");
            var itemMaxEnchant = itemData.Select(item => 
                item.TryGetValue("linkMaterialEnchantId", out var matId) && matId is int id && id != 0 ? 
                    new Dictionary<string, object>()
                    {
                        {"id", item["id"]},
                        {"maxEnchant", 
                            dataCenter.Root
                            .Children("MaterialEnchantData")
                            .First()
                            .Children("ItemEnchant")
                            .Where(e => e["materialEnchantId"].AsInt32 == id)
                            .Select(e => e["maxEnchantCount"].Value)
                            .First()
                        },
                    } 
                    : new Dictionary<string, object>()
            );

            var itemSkillLinkData = ResolveLinkSkillIds(dataCenter.Root, itemData);

            
            var items = JoinElementsByKey("id", itemData, itemStrings, itemMaxEnchant, itemSkillLinkData);
            
            var passivityData = ExtractElements(dataCenter.Root, "Passivity", "Passive");
            var passivityStrings = ExtractElements(dataCenter.Root, "StrSheet_Passivity", "String");
            var passivities = JoinElementsByKey("id", passivityData, passivityStrings);

            var itemPassivityRelation = GenerateLinkRelation(items, "linkPassivityId", "item", "passivity").ToList();

            var passivityCategories = ExtractElements(dataCenter.Root, "EquipmentEnchantData", "PassivityCategoryData", "Category");
            var passivityCategoryToPassivity = GenerateLinkRelation(passivityCategories, "passivityLink", "passivity_category", "passivity");
            var itemToPassivityCategoryUnfiltered = GenerateLinkRelation(items, "linkPassivityCategoryId", "item", "passivity_category");
            
            Console.WriteLine("Filtering BHs mess on passivity categories");
            var passivityCategoryIds = passivityCategories.Select(e => e["id"]).ToHashSet();
            var itemToPassivityCategory = itemToPassivityCategoryUnfiltered.Where(e => passivityCategoryIds.Contains(e["passivity_category"]));
            
            var equipmentData = ExtractElements(dataCenter.Root, "EquipmentData", "Equipment");

            var enchantData = ExtractElements(dataCenter.Root, "EquipmentEnchantData", "EnchantData", "Enchant");
            var enchantEffects = GenerateIds(ExtractElements(dataCenter.Root, "EquipmentEnchantData", "EnchantData", "Enchant", "Effect"));
            var enchantStats = GenerateIds(ExtractElements(dataCenter.Root, "EquipmentEnchantData", "EnchantData", "Enchant", "BasicStat"));
            
            
            
            var abnormalData = ExtractElements(dataCenter.Root, "Abnormality", "Abnormal");
            var abnormalStrings = ExtractElements(dataCenter.Root, "StrSheet_Abnormality", "String");
            var abnormalIcons = MapKeys(
                ExtractElements(dataCenter.Root, "AbnormalityIconData", "Icon"), 
                new Dictionary<string, string>{{ "abnormalityId", "id" }, { "iconName", "icon" }}
            );
            var abnormals = JoinElementsByKey("id", abnormalData, abnormalStrings, abnormalIcons);
            var abnormalEffects = GenerateIds(ExtractElements(dataCenter.Root, "Abnormality", "Abnormal", "AbnormalityEffect")).ToList();

            

            var form = new MultipartFormDataContent
            {
                {ElementsContent(items), "items"}, 
//                {ElementsContent(passivities), "passivities"},
//                {ElementsContent(itemPassivityRelation), "item_to_passivity"},
//                {ElementsContent(passivityCategories), "passivity_categories"},
//                {ElementsContent(itemToPassivityCategory), "item_to_passivity_category"},
//                {ElementsContent(passivityCategoryToPassivity), "passivity_category_to_passivity"},
//                {ElementsContent(equipmentData), "equipment_data"}, 
//                {ElementsContent(enchantData), "enchant_data"},
//                {ElementsContent(enchantEffects), "enchant_effects"},
//                {ElementsContent(enchantStats), "enchant_stats"},
//                {ElementsContent(abnormals), "abnormals"},
//                {ElementsContent(abnormalEffects), "abnormal_effects"},
            };

            Console.WriteLine("Gzipped !");

//            await UploadData("http://127.0.0.1:8000/analyse/", form);
            await UploadData("http://127.0.0.1:8000/upload/items/", form);
        }

        private static IEnumerable<Dictionary<string, object>> ResolveLinkSkillIds(DataCenterElement root, IEnumerable<IDictionary<string, object>> items)
        {
            Console.WriteLine("Processing linkSkillIds");
            var skills = root
                .Children("SkillData")
                .Select(e => e.Children("Skill"))
                .SelectMany(e => e);
//                .ToDictionary(e => e["id"].AsInt32, e => e);

            var skillsMap = new Dictionary<int, DataCenterElement>();

            foreach (var skill in skills)
            {
                skillsMap[skill["id"].AsInt32] = skill;
            }
            
            Console.WriteLine("SkillData mapping created");

            return items.Select(item =>
            {

                item.TryGetValue("linkSkillId", out var sId);
                if (!(sId is int id) || id == 0) return new Dictionary<string, object>();

                if (!skillsMap.TryGetValue(id, out var skill))
                    return new Dictionary<string, object>();

                var abnormals = skill.Descendants("AbnormalityOnCommon").ToList();
                if (abnormals.Count < 1) return new Dictionary<string, object>();

                var hasAbnormal = abnormals.First()
                    .Attributes.TryGetValue("id", out var abnormalId);

                if (!hasAbnormal || abnormalId.AsInt32 == 0) return new Dictionary<string, object>();

                return new Dictionary<string, object>()
                {
                    {"id", item["id"]},
                    {"linkAbnormalityId", abnormalId.AsInt32}
                };
            });
        }

        private static IEnumerable<IDictionary<string, object>> GenerateLinkRelation(IEnumerable<IDictionary<string, object>> elements, string linkKey, string thisSide, string otherSide, char separator = ';')
        {
            Console.WriteLine("Generating link relation {0} to {1}", thisSide, otherSide);
            var result = elements
                .Select(element =>
                    ProcessLink(element.TryGetValue(linkKey, out var got) ? got : "", separator)
                        .Select((link, position) =>
                            new Dictionary<string, object>
                            {
                                [thisSide] = element["id"],
                                [otherSide] = link,
                                ["position"] = position
                            }
                        )
                )
                .SelectMany(e => e);

            return GenerateIds(result);
        }

        private static IEnumerable<IDictionary<string, object>> MapKeys(IEnumerable<IDictionary<string, object>> elements, IReadOnlyDictionary<string, string> renames)
        {
            return elements.Select(d => 
                d.ToDictionary(
                    entry => renames.TryGetValue(entry.Key, out var newKey) ? newKey : entry.Key, 
                    entry => entry.Value
                )
            );
        }

        private static IEnumerable<IDictionary<string, object>> GenerateXmlChildRelation(
            IEnumerable<IDictionary<string, object>> elements, string thisSide, string otherSide)
        {
            Console.WriteLine("Generating xml child relation {0} to {1}", thisSide, otherSide);
            var result = elements
                .Select(element => 
                    new Dictionary<string, object> {
                        [thisSide] = element["id"], 
                        [otherSide] = element["parent_id"], 
                        ["position"] = element["element_position"]
                    }
                );

            return GenerateIds(result);
        }

        private static IEnumerable<IDictionary<string, object>> GenerateIds(
            IEnumerable<IDictionary<string, object>> elements,
            string name = "id")
        {
            return elements.Select((e, i) =>
            {
                e[name] = i;
                return e;
            });
        }

        /***
         * Caution! First parameter is special (on purpose) - ids not existing in first won't be in result
         */
        private static List<Dictionary<string, object>> JoinElementsByKey(string key, params IEnumerable<IDictionary<string,object>>[] elementLists)
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

        public static HttpContent GZippedCsvContent(CsvExport data)
        {
            return new StringContent(Convert.ToBase64String(GzipByte(data.ExportToBytes())));
        }
        
        public static HttpContent ElementsContent(IEnumerable<IDictionary<string, object>> elements)
        {
            return GZippedCsvContent(ExtractCsv(elements));
        }
        
        public static async Task<HttpResponseMessage> UploadData(string uri, HttpContent content) {
            using var handler = new HttpClientHandler {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            
            using var client = new HttpClient(handler, false);
            client.Timeout = TimeSpan.FromDays(5);
            
//            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Headers.ContentEncoding.Add("deflate");
            
            var response = await client.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            Console.WriteLine(response.Content.ToString());

            return response;
        }

        public static byte[] GzipByte(byte[] bytes) {  
            using var output = new MemoryStream();
            using var compressedStream = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression);

            compressedStream.Write(bytes, 0, bytes.Length);  
            compressedStream.Close();
            
            return output.ToArray();
        }

        public delegate void ProcessDatacenterElementToCsv(DataCenterElement element, CsvExport export);

        public static List<IDictionary<string, object>> ExtractElements(DataCenterElement element, params string[] path)
        {
            return ExtractElements(element, true, path);
        }
        
        public static List<IDictionary<string, object>> ExtractElements(DataCenterElement element, bool verbose = false, params string[] path)
        {
            if (verbose) Console.WriteLine("Extracting {0}", string.Join(".", path));

            if (path.Length == 0)
            {
                var attributes = element.Attributes.ToDictionary(entry => entry.Key, entry => entry.Value.Value);
                return new List<IDictionary<string, object>> {ProcessElement(attributes, element)};
            }

            var result = new List<IDictionary<string, object>>();
            foreach (var child in element.Children(path.First()))
                result.AddRange(ExtractElements(child, false, path.Skip(1).ToArray()));

            return result;
        }

        private static IDictionary<string, object> ProcessElement(IDictionary<string, object> attributes, DataCenterElement element)
        {
            if (element.Parent.Attributes.TryGetValue("id", out var parentId))
            {
                attributes["parent_id"] = parentId.Value;
                attributes["element_position"] = element.Parent.Children()
                    .Where(sibling => sibling.Name == element.Name)
                    .TakeWhile(sibling => !sibling.Equals(element))
                    .Count();
            }

            return attributes;
        }

        public static CsvExport ExtractCsv(IEnumerable<IDictionary<string, object>> elements) {
            var export = new CsvExport();
            
            // adds a typing row
            export.AddRow();
            var enumerable = elements.ToList();
            foreach (var element in enumerable) ExtractCvsElementTyping(element, export);
            foreach (var element in enumerable) ExtractCvsElement(element, export);
            
            return export;
        }

        private static void ExtractCvsElementTyping(IDictionary<string, object> element, CsvExport export)
        {
            foreach (var attribute in element) {
                var key = attribute.Key;
                var value = attribute.Value;

                export[key] = value switch
                {
                    bool _ => "bool",
                    float _ => "float",
                    int _ => "int",
                    string _ => "str",
                    _ => throw new ArgumentException("Wtf did you even get this from ??")
                };
            }
        }

        private static void ExtractCvsElement(IDictionary<string, object> element, CsvExport export) {
            export.AddRow();
            
            foreach (var attribute in element) {
                var key = attribute.Key;
                var value = attribute.Value;
                
                export[key] = value;
            }
        }
        
        public static CsvExport ExtractElementsCsv(DataCenterElement element, params string[] path)
        {
            var elements = ExtractElements(element, path);
            return ExtractCsv(elements);
        }
    }
}