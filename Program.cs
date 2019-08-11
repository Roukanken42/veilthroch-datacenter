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
            
            
            var itemData = ExtractElementsCsv(dataCenter.Root, "ItemData", "Item");
            var itemStrings = ExtractElementsCsv(dataCenter.Root, "StrSheet_Item", "String");

            var passivityData = ExtractElementsCsv(dataCenter.Root, "Passivity", "Passive");
            var passivityStrings = ExtractElementsCsv(dataCenter.Root, "StrSheet_Passivity", "String");

            var equipmentData = ExtractElementsCsv(dataCenter.Root, "EquipmentData", "Equipment");
            
            var abnormals = ExtractElementsCsv(dataCenter.Root, "Abnormality", "Abnormal");
            var abnormalEffecs = ExtractElementsCsv(dataCenter.Root, "Abnormality", "Abnormal", "AbnormalityEffect");
            var abnormalStrings = ExtractElementsCsv(dataCenter.Root, "StrSheet_Abnormality", "String");


            await UploadData("http://127.0.0.1:8000/analyse/", GZippedCsvContent(abnormals));
//            await UploadData("http://127.0.0.1:8000/analyse/", GZippedCsvContent(abnormalEffecs));
//            await UploadData("http://127.0.0.1:8000/analyse/", GZippedCsvContent(abnormalStrings));

            
            var form = new MultipartFormDataContent
            {
                {GZippedCsvContent(equipmentData), "equipment_data"}, 
                {GZippedCsvContent(itemData), "item_data"}, 
                {GZippedCsvContent(itemStrings), "item_strings"},
                {GZippedCsvContent(passivityData), "passivity_data"},
                {GZippedCsvContent(passivityStrings), "passivity_strings"},
            };

            Console.WriteLine("Gzipped !");
//            using var file = File.Open("passivity.csv", FileMode.Create);
//            using var writer = new BinaryWriter(file);
//            writer.Write(passivityData.ExportToBytes());


//            await UploadData("http://127.0.0.1:8000/upload/items/", form);
        }

        public static HttpContent GZippedCsvContent(CsvExport data)
        {
            return new StringContent(Convert.ToBase64String(GzipByte(data.ExportToBytes())));
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