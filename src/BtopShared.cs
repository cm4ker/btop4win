using System;
using System.Collections.Generic;
using System.IO;

namespace BtopShared
{
    public static class Shared
    {
        public static string ConfigFilePath = "config.json";
        public static string ThemeFilePath = "theme.json";

        public static void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                string configContent = File.ReadAllText(ConfigFilePath);
                // Deserialize configContent to your config object
            }
            else
            {
                Console.WriteLine("Config file not found.");
            }
        }

        public static void LoadTheme()
        {
            if (File.Exists(ThemeFilePath))
            {
                string themeContent = File.ReadAllText(ThemeFilePath);
                // Deserialize themeContent to your theme object
            }
            else
            {
                Console.WriteLine("Theme file not found.");
            }
        }

        public static void SaveConfig(object config)
        {
            string configContent = ""; // Serialize your config object to string
            File.WriteAllText(ConfigFilePath, configContent);
        }

        public static void SaveTheme(object theme)
        {
            string themeContent = ""; // Serialize your theme object to string
            File.WriteAllText(ThemeFilePath, themeContent);
        }
    }
}
