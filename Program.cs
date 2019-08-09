using System;
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
            
            var data = ExtractElementsCsv(dataCenter.Root, "ItemData", "Item");

            using var file = File.Open("text.csv", FileMode.Create);
//            using var writer = new BinaryWriter(file);
//            writer.Write(data.ExportToBytes());

            await UploadData("http://127.0.0.1:5000/data", data.ExportToBytes());
        }

        public static async Task<HttpResponseMessage> UploadData(string uri, byte[] data) {
            using var handler = new HttpClientHandler {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            
            using var client = new HttpClient(handler, false);

            data = GzipByte(data);
//            Console.WriteLine(BitConverter.ToString(data).Substring(0, 50));
            
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
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
        
        public static CsvExport ExtractElementsCsv(DataCenterElement element, params string[] path) {
            Console.Write("Extracting {0} ...", string.Join(".", path));
            var export = new CsvExport();
            
            // adds a typing row
            export.AddRow();
            ExtractElementsCsv(element, export, ElementTypingToCsv, path);
            Console.Write(" typing collected ...");
            // adds data rows
            ExtractElementsCsv(element, export, ElementAttributesToCsv, path);
            Console.WriteLine(" data collected!");
            return export;
        }

        public static void ExtractElementsCsv(DataCenterElement element, CsvExport export, ProcessDatacenterElementToCsv callback, params string[] path) {
            if (path.Length == 0) {
                callback.Invoke(element, export);
                return;
            }

            foreach (var child in element.Children(path.First()))
                ExtractElementsCsv(child, export, callback, path.Skip(1).ToArray());
        }
        
        private static void ElementAttributesToCsv(DataCenterElement element, CsvExport export) {
            export.AddRow();
            
            foreach (var attribute in element.Attributes) {
                var key = attribute.Key;
                var value = attribute.Value;
                
                if(value.IsBoolean) export[key] = value.AsBoolean;
                if(value.IsSingle) export[key] = value.AsSingle;
                if(value.IsInt32) export[key] = value.AsInt32;
                if(value.IsString) export[key] = value.AsString;
            }
        }
        
        private static void ElementTypingToCsv(DataCenterElement element, CsvExport export) {
            foreach (var attribute in element.Attributes) {
                var key = attribute.Key;
                var value = attribute.Value;
                
                if(value.IsBoolean) export[key] = "bool";
                if(value.IsSingle) export[key] = "float";
                if(value.IsInt32) export[key] = "int";
                if(value.IsString) export[key] = "str";
            }
        }
    }
}