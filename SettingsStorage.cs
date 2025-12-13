using System;
using System.IO;
using Newtonsoft.Json;

namespace maorc287.RBRDataExtPlugin
{
    internal static class SettingsStorage
    {
        private static readonly string SettingsFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PluginsData", "RBRDataExt");

        private static readonly string SettingsFilePath =
            Path.Combine(SettingsFolder, "RBRDataExt.settings.json");

        public static RBRSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new RBRSettings();

                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<RBRSettings>(json);
                return settings ?? new RBRSettings();
            }
            catch
            {
                return new RBRSettings();
            }
        }

        public static void Save(RBRSettings settings)
        {
            try
            {
                if (!Directory.Exists(SettingsFolder))
                    Directory.CreateDirectory(SettingsFolder);

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Ignore save errors.
            }
        }
    }
}
