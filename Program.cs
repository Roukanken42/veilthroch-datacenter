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
//            manager.UpdateAll();

            var dc = manager._dataCenters.First();
            var dataCenter = new DataCenter(dc.File.Open(FileMode.Open), DataCenterMode.Persistent, DataCenterStringOptions.None);
            Console.WriteLine("Loaded DC");


            var fixtures = new Extract.Extract(dataCenter.Root).Json();

            Console.WriteLine("done !");
            Console.WriteLine("Uploading...");

            File.WriteAllText("test.json", fixtures);
//            var data = await UploadData("http://staging.veilthroch.com/api/datacenter/upload", new StringContent(fixtures));
            var data = await UploadData("http://localhost:8000/datacenter/upload", new StringContent(fixtures));
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
//            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadAsStringAsync();
            Console.Write(result);
            
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