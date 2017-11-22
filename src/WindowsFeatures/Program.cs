using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace WindowsFeatures
{
    public static class StringExtensions
    {
        public static bool ContainsAny(this string item, string[] candidates)
        {
            foreach (var candidate in candidates)
                if (item.Trim().Contains(candidate.Trim()))
                    return true;
            return false;
        }

        public static bool DoesNotContainAny(this string item, string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (item.Trim().Contains(candidate.Trim()))
                    return false;
            }
            return true;
        }
    }

    class Program
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("This is to run on Windows Server 2016+");
            Console.WriteLine("This utility must be compiled as 64-bit!");
            var script = Path.Combine(AssemblyDirectory, "CreateFeaturesFile.cmd");
            var cmd = string.Format("/C {0}", script);
            Process.Start("cmd", cmd);

            Thread.Sleep(5000);
            var rawfeaturesFile = Path.Combine(AssemblyDirectory, "rawFeatures.txt");

            var featuresEnum = from line in File.ReadAllLines(rawfeaturesFile)
                let split = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                where line.Contains(":")
                      && split[0].ToLower().Contains("feature name")
                select split[1].Trim();

            var features = featuresEnum.ToList();
            var featuresFile = Path.Combine(AssemblyDirectory, "parsedFeatures.txt");
            File.WriteAllLines(featuresFile, features.ToArray());

            var inFeatures = ConfigurationManager.AppSettings["RolesMustContain"].Split(',');
            var outFeatures = ConfigurationManager.AppSettings["RolesMustNotContain"].Split(',');
            var matchingFeaturesEnum = from feature in features
                where feature.ContainsAny(inFeatures)
                      && feature.DoesNotContainAny(outFeatures)
                select feature;

            var matchingFeatures = matchingFeaturesEnum.ToList();
            var matchingFeaturesFile = Path.Combine(AssemblyDirectory, "matchingFeatures.txt");
            File.WriteAllLines(matchingFeaturesFile, matchingFeatures.ToArray());

            foreach (var feature in matchingFeatures)
            {
                Console.WriteLine(feature);
            }

            var fmt = ConfigurationManager.AppSettings["DismEnableFormat"];
            var enabledFeaturesEnum = from feature in matchingFeatures
                select string.Format(fmt, feature);

            var enabledFeatures = enabledFeaturesEnum.ToList();
            var enabledFeaturesFile = Path.Combine(AssemblyDirectory, "enableFeatures.cmd");
            File.WriteAllLines(enabledFeaturesFile, enabledFeatures.ToArray());
        }
    }
}
