﻿using System;
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
using VeiltrochDatacenter.Extract;

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


            var data = new Extract.Extract(dataCenter.Root).Json();

//            
//            var itemData = ExtractElements(dataCenter.Root, "ItemData", "Item").ToList();
//            var itemStrings = ExtractElements(dataCenter.Root, "StrSheet_Item", "String");
//            Console.WriteLine("Extracting ItemData@maxEnchant");
//            var itemMaxEnchant = itemData.Select(item => 
//                item.TryGetValue("linkMaterialEnchantId", out var matId) && matId is int id && id != 0 ? 
//                    new Dictionary<string, object>()
//                    {
//                        {"id", item["id"]},
//                        {"maxEnchant", 
//                            dataCenter.Root
//                            .Children("MaterialEnchantData")
//                            .First()
//                            .Children("ItemEnchant")
//                            .Where(e => e["materialEnchantId"].AsInt32 == id)
//                            .Select(e => e["maxEnchantCount"].Value)
//                            .First()
//                        },
//                    } 
//                    : new Dictionary<string, object>()
//            );
//
//            var itemSkillLinkData = ResolveLinkSkillIds(dataCenter.Root, itemData, abnormals);
//
//            
//            var items = JoinElementsByKey("id", itemData, itemStrings, itemMaxEnchant, itemSkillLinkData);
//            
//            var passivityData = ExtractElements(dataCenter.Root, "Passivity", "Passive");
//            var passivityStrings = ExtractElements(dataCenter.Root, "StrSheet_Passivity", "String");
//            var passivities = JoinElementsByKey("id", passivityData, passivityStrings);
//
//            var itemPassivityRelation = GenerateLinkRelation(items, "linkPassivityId", "item", "passivity").ToList();
//
//            var passivityCategories = ExtractElements(dataCenter.Root, "EquipmentEnchantData", "PassivityCategoryData", "Category");
//            var passivityCategoryToPassivity = GenerateLinkRelation(passivityCategories, "passivityLink", "passivity_category", "passivity");
//            var itemToPassivityCategoryUnfiltered = GenerateLinkRelation(items, "linkPassivityCategoryId", "item", "passivity_category");
//            
//            Console.WriteLine("Filtering BHs mess on passivity categories");
//            var passivityCategoryIds = passivityCategories.Select(e => e["id"]).ToHashSet();
//            var itemToPassivityCategory = itemToPassivityCategoryUnfiltered.Where(e => passivityCategoryIds.Contains(e["passivity_category"]));
//            
//            var equipmentData = ExtractElements(dataCenter.Root, "EquipmentData", "Equipment");
//
//            var enchantData = ExtractElements(dataCenter.Root, "EquipmentEnchantData", "EnchantData", "Enchant");
//            var enchantEffects = GenerateIds(ExtractElements(dataCenter.Root, "EquipmentEnchantData", "EnchantData", "Enchant", "Effect"));
//            var enchantStats = GenerateIds(ExtractElements(dataCenter.Root, "EquipmentEnchantData", "EnchantData", "Enchant", "BasicStat"));
//
//            var crystals = ExtractElements(dataCenter.Root, "CustomizingItems", "CustomizingItem").ToList();
//            var crystalToPassivity = GenerateLinkRelation(crystals, "passivityLink", "crystal", "passivity");
//
//            var glyphData = ExtractElements(dataCenter.Root, "CrestData", "CrestItem");
//            var glyphStrings = ExtractElements(dataCenter.Root, "StrSheet_Crest", "String");
//            var glyphs = JoinElementsByKey("id", glyphData, glyphStrings).ToList();
//
//            var glyphIds = glyphs.Select(g => (int) g["id"]).ToHashSet();
//            
//            items = items.Select(item =>
//            {
//                // Filter glyphs because BH be BH and have non existing non-0 links in dc...
//                if (item.TryGetValue("linkCrestId", out var id) && !glyphIds.Contains((int) id))
//                    item["linkCrestId"] = 0;
//
//                return item;
//            }).ToList();
//
//
//            Console.Write("Compressing... ");
//            
//            var form = new MultipartFormDataContent
//            {
//                {new StringContent("RUS"), "region"},
//                {ElementsContent(items), "items"}, 
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
//                {ElementsContent(abnormalKinds), "abnormality_kind"},
//                {ElementsContent(abnormalEffects), "abnormal_effects"},
//                {ElementsContent(crystals), "crystals"},
//                {ElementsContent(crystalToPassivity), "crystal_to_passivity"},
//                {ElementsContent(glyphs), "glyphs"},
//            };
//
//
//            Console.WriteLine("done !");
//
////            await UploadData("http://127.0.0.1:8000/analyse/", form);
//            var data = await UploadData("http://127.0.0.1:8000/datacenter/process/csv/", form);
//            var fixtures = Convert.FromBase64String(data);
//            System.IO.File.WriteAllBytes(@"RUS.json.gz", fixtures);
        }

        private static IEnumerable<Dictionary<string, object>> ResolveLinkSkillIds(DataCenterElement root, IEnumerable<IDictionary<string, object>> items, IEnumerable<IDictionary<string, object>> abnormals)
        {
            Console.WriteLine("Processing linkSkillIds");
            var skills = root
                .Children("SkillData")
                .SelectMany(e => e.Children("Skill"));
//                .ToDictionary(e => e["id"].AsInt32, e => e);

            var abnormalIds = abnormals.Select(a => (int) a["id"]).ToHashSet();
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

                var effects = skill
                    .Descendants("TargetingList")
                    .SelectMany(e => e.Descendants("Effect"))
                    .ToList();
                if (effects.Count < 1) return new Dictionary<string, object>();

                var effect = effects.First();

                var abnormal = effect.Descendants("AbnormalityOnCommon").FirstOrDefault();
                var hpDiff = effect.Descendants("HpDiff").FirstOrDefault();
                var mpDiff = effect.Descendants("MpDiff").FirstOrDefault();

                var result = new Dictionary<string, object>()
                {
                    {"id", item["id"]},
                };

                if (abnormal != null && abnormal.Attributes.TryGetValue("id", out var abnormalId) &&
                    abnormalId.AsInt32 != 0 && abnormalIds.Contains(abnormalId.AsInt32))
                    result["skillEffectAbnormalityCommon"] = abnormalId.AsInt32;

                if (hpDiff != null && hpDiff.Attributes.TryGetValue("value", out var hpValue))
                    result["skillEffectHpDiff"] = hpValue.AsSingle;

                if (mpDiff != null && mpDiff.Attributes.TryGetValue("value", out var mpValue))
                    result["skillEffectMpDiff"] = mpValue.AsInt32;

                return result;
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

//        public static HttpContent GZippedJsonContent(CsvExport data)
//        {
//            return new StringContent(Convert.ToBase64String(GzipByte(data.ExportToBytes())));
//        }
//        
//        public static HttpContent ElementsContent(IEnumerable<IDictionary<string, object>> elements)
//        {
//            return GZippedCsvContent(ExtractCsv(elements));
//        }
        
        public static async Task<string> UploadData(string uri, HttpContent content) {
            using var handler = new HttpClientHandler {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            
            using var client = new HttpClient(handler, false);
            client.Timeout = TimeSpan.FromDays(5);
            
//            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Headers.ContentEncoding.Add("deflate");
            
            var response = await client.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadAsStringAsync();
//            Console.Write(result);
            
            return result;
        }

        public static byte[] GzipByte(byte[] bytes) {  
            using var output = new MemoryStream();
            using var compressedStream = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression);

            compressedStream.Write(bytes, 0, bytes.Length);  
            compressedStream.Close();
            
            return output.ToArray();
        }
    }
}