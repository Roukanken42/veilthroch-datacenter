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
        
        public static async Task Main(string[] args) {
            var manager = new AssetManager();
            manager.UpdateAll();

            var dc = manager._dataCenters.First();
            var dataCenter = new DataCenter(dc.File.Open(FileMode.Open), DataCenterMode.Persistent, DataCenterStringOptions.None);
            Console.WriteLine("Loaded DC");
            
            
            var itemData = ExtractElements(dataCenter.Root, "ItemData", "Item");
            var itemStrings = ExtractElements(dataCenter.Root, "StrSheet_Item", "String");
            var items = JoinElementsByKey("id", itemData, itemStrings);


            var passivityData = ExtractElements(dataCenter.Root, "Passivity", "Passive");
            var passivityStrings = ExtractElements(dataCenter.Root, "StrSheet_Passivity", "String");
            var passivities = JoinElementsByKey("id", passivityData, passivityStrings);

            var equipmentData = ExtractElements(dataCenter.Root, "EquipmentData", "Equipment");
            
            var abnormalData = ExtractElements(dataCenter.Root, "Abnormality", "Abnormal");
            var abnormalStrings = ExtractElements(dataCenter.Root, "StrSheet_Abnormality", "String");
            var abnormals = JoinElementsByKey("id", abnormalData, abnormalStrings);
            
            var abnormalEffects = GenerateIds(ExtractElements(dataCenter.Root, "Abnormality", "Abnormal", "AbnormalityEffect")).ToList();

            var abnormalEffectAbnormalRelation =
                GenerateManyToOneRelation(abnormalEffects, "parent_id", "abnormality_effect", "abnormality");
            
//            await UploadData("http://127.0.0.1:8000/analyse/", GZippedCsvContent(abnormals));

            
            var form = new MultipartFormDataContent
            {
                {ElementsContent(items), "items"}, 
                {ElementsContent(equipmentData), "equipment_data"}, 
                {ElementsContent(passivities), "passivities"},
                {ElementsContent(abnormals), "abnormals"},
                {ElementsContent(abnormalEffects), "abnormal_effects"},
                {ElementsContent(abnormalEffectAbnormalRelation), "abnormal_effect_to_abnormal"},
            };

            Console.WriteLine("Gzipped !");


            await UploadData("http://127.0.0.1:8000/upload/items/", form);
        }

        private static IEnumerable<IDictionary<string, object>> GenerateManyToOneRelation(
            IEnumerable<IDictionary<string, object>> elements, string key, string thisSide, string otherSide)
        {
            var result = new List<Dictionary<string, object>>();

            foreach (var element in elements)
            {
                var rel = new Dictionary<string, object>();
                rel[thisSide] = element["id"];
                rel[otherSide] = element[key];
                rel["position"] = element["element_position"];
                result.Add(rel);
            }

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

        private static List<Dictionary<string, object>> JoinElementsByKey(string key, params List<IDictionary<string,object>>[] elementLists)
        {
            var result = new Dictionary<object, Dictionary<string, object>>();

            foreach (var elementList in elementLists) {
                foreach (var element in elementList) {
                    if (!element.TryGetValue(key, out var id))
                        throw new KeyNotFoundException();
                    
                    var merged = result.GetValueOrDefault(id, new Dictionary<string, object>());

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
                attributes["element_position"] = element.Siblings()
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
                    _ => ""
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