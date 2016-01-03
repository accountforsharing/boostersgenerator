using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BoosterGenerator;

using Newtonsoft.Json;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var setCode = args[0];
            string settingPath;
            if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
            {
                settingPath = args[1];
            }
            else
            {
                settingPath = "settings.json";
            }

            using (var writer = File.CreateText(settingPath))
            {
                writer.Write(JsonConvert.SerializeObject(Settings.StandartHand(), Formatting.Indented));
            }

            Settings settings;
            try
            {
                using (var reader = File.OpenText(settingPath))
                {
                    settings = JsonConvert.DeserializeObject<Settings>(reader.ReadToEnd());
                }
            }
            catch (Exception)
            {
                settings = Settings.StandartHand();
            }

            new DocumentGenerator().Generate(setCode, settings);
        }
    }
}
