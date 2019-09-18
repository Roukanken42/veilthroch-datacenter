using Alkahest.Core;
using Alkahest.Core.Data;
using Alkahest.Core.Logging;
using System;
using System.Configuration;
using System.Linq;
using System.Net;

namespace Alkahest
{
    static class Configuration
    {
        public static Uri AssetManifestUri { get; }

        public static string AssetDirectory { get; }

        public static TimeSpan AssetTimeout { get; }

        public static Region[] Regions { get; }

        static Configuration()
        {
            AssetManifestUri = new Uri("https://raw.githubusercontent.com/tera-alkahest/alkahest-assets/master/manifest.json");
            AssetDirectory = "assets";
            AssetTimeout = TimeSpan.FromMinutes(10);
//            Regions = Split("uk de fr jp kr na ru se th tw", ' ').Select(x => (Region)Enum.Parse(typeof(Region), x, true)).ToArray();
            Regions = Split("kr", ' ').Select(x => (Region)Enum.Parse(typeof(Region), x, true)).ToArray();
        }

        static string[] Split(string value, char separator)
        {
            return value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToArray();
        }
    }
}
