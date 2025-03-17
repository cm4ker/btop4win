using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BtopTheme
{
    public static class Theme
    {
        public static string ThemeDir;
        public static string UserThemeDir;
        public static List<string> Themes = new List<string>();
        public static Dictionary<string, string> Colors = new Dictionary<string, string>();
        public static Dictionary<string, int[]> Rgbs = new Dictionary<string, int[]>();
        public static Dictionary<string, string[]> Gradients = new Dictionary<string, string[]>();

        private static readonly Dictionary<string, string> DefaultTheme = new Dictionary<string, string>
        {
            { "main_bg", "#00" },
            { "main_fg", "#cc" },
            { "title", "#ee" },
            { "hi_fg", "#b54040" },
            { "selected_bg", "#6a2f2f" },
            { "selected_fg", "#ee" },
            { "inactive_fg", "#40" },
            { "graph_text", "#60" },
            { "meter_bg", "#40" },
            { "proc_misc", "#0de756" },
            { "cpu_box", "#556d59" },
            { "mem_box", "#6c6c4b" },
            { "net_box", "#5c588d" },
            { "proc_box", "#805252" },
            { "div_line", "#30" },
            { "temp_start", "#4897d4" },
            { "temp_mid", "#5474e8" },
            { "temp_end", "#ff40b6" },
            { "cpu_start", "#77ca9b" },
            { "cpu_mid", "#cbc06c" },
            { "cpu_end", "#dc4c4c" },
            { "free_start", "#384f21" },
            { "free_mid", "#b5e685" },
            { "free_end", "#dcff85" },
            { "cached_start", "#163350" },
            { "cached_mid", "#74e6fc" },
            { "cached_end", "#26c5ff" },
            { "available_start", "#4e3f0e" },
            { "available_mid", "#ffd77a" },
            { "available_end", "#ffb814" },
            { "used_start", "#592b26" },
            { "used_mid", "#d9626d" },
            { "used_end", "#ff4769" },
            { "download_start", "#291f75" },
            { "download_mid", "#4f43a3" },
            { "download_end", "#b0a9de" },
            { "upload_start", "#620665" },
            { "upload_mid", "#7d4180" },
            { "upload_end", "#dcafde" },
            { "process_start", "#80d0a3" },
            { "process_mid", "#dcd179" },
            { "process_end", "#d45454" }
        };

        private static readonly Dictionary<string, string> TtyTheme = new Dictionary<string, string>
        {
            { "main_bg", "\x1b[0;40m" },
            { "main_fg", "\x1b[37m" },
            { "title", "\x1b[97m" },
            { "hi_fg", "\x1b[91m" },
            { "selected_bg", "\x1b[41m" },
            { "selected_fg", "\x1b[97m" },
            { "inactive_fg", "\x1b[90m" },
            { "graph_text", "\x1b[90m" },
            { "meter_bg", "\x1b[90m" },
            { "proc_misc", "\x1b[92m" },
            { "cpu_box", "\x1b[32m" },
            { "mem_box", "\x1b[33m" },
            { "net_box", "\x1b[35m" },
            { "proc_box", "\x1b[31m" },
            { "div_line", "\x1b[90m" },
            { "temp_start", "\x1b[94m" },
            { "temp_mid", "\x1b[96m" },
            { "temp_end", "\x1b[95m" },
            { "cpu_start", "\x1b[92m" },
            { "cpu_mid", "\x1b[93m" },
            { "cpu_end", "\x1b[91m" },
            { "free_start", "\x1b[32m" },
            { "free_mid", "" },
            { "free_end", "\x1b[92m" },
            { "cached_start", "\x1b[36m" },
            { "cached_mid", "" },
            { "cached_end", "\x1b[96m" },
            { "virtual_start", "\x1b[36m" },
            { "virtual_mid", "" },
            { "virtual_end", "\x1b[96m" },
            { "available_start", "\x1b[33m" },
            { "available_mid", "" },
            { "available_end", "\x1b[93m" },
            { "used_start", "\x1b[31m" },
            { "used_mid", "" },
            { "used_end", "\x1b[91m" },
            { "download_start", "\x1b[34m" },
            { "download_mid", "" },
            { "download_end", "\x1b[94m" },
            { "upload_start", "\x1b[35m" },
            { "upload_mid", "" },
            { "upload_end", "\x1b[95m" },
            { "process_start", "\x1b[32m" },
            { "process_mid", "\x1b[33m" },
            { "process_end", "\x1b[31m" }
        };

        private static int TruecolorTo256(int r, int g, int b)
        {
            if (Math.Round((double)r / 11) == Math.Round((double)g / 11) && Math.Round((double)b / 11) == Math.Round((double)r / 11))
            {
                return 232 + (int)Math.Round((double)r / 11);
            }
            else
            {
                return (int)Math.Round((double)r / 51) * 36 + (int)Math.Round((double)g / 51) * 6 + (int)Math.Round((double)b / 51) + 16;
            }
        }

        public static string HexToColor(string hexa, bool tTo256, string depth)
        {
            if (hexa.Length > 1)
            {
                hexa = hexa.Substring(1);
                foreach (char c in hexa)
                {
                    if (!Uri.IsHexDigit(c))
                    {
                        Console.WriteLine("Invalid hex value: " + hexa);
                        return "";
                    }
                }
                string pre = "\x1b[" + (depth == "fg" ? "38" : "48") + ";" + (tTo256 ? "5;" : "2;");

                if (hexa.Length == 2)
                {
                    int hInt = Convert.ToInt32(hexa, 16);
                    if (tTo256)
                    {
                        return pre + TruecolorTo256(hInt, hInt, hInt) + "m";
                    }
                    else
                    {
                        string hStr = hInt.ToString();
                        return pre + hStr + ";" + hStr + ";" + hStr + "m";
                    }
                }
                else if (hexa.Length == 6)
                {
                    if (tTo256)
                    {
                        return pre + TruecolorTo256(
                            Convert.ToInt32(hexa.Substring(0, 2), 16),
                            Convert.ToInt32(hexa.Substring(2, 2), 16),
                            Convert.ToInt32(hexa.Substring(4, 2), 16)) + "m";
                    }
                    else
                    {
                        return pre +
                            Convert.ToInt32(hexa.Substring(0, 2), 16) + ";" +
                            Convert.ToInt32(hexa.Substring(2, 2), 16) + ";" +
                            Convert.ToInt32(hexa.Substring(4, 2), 16) + "m";
                    }
                }
                else
                {
                    Console.WriteLine("Invalid size of hex value: " + hexa);
                }
            }
            else
            {
                Console.WriteLine("Hex value missing: " + hexa);
            }
            return "";
        }

        public static string DecToColor(int r, int g, int b, bool tTo256, string depth)
        {
            string pre = "\x1b[" + (depth == "fg" ? "38" : "48") + ";" + (tTo256 ? "5;" : "2;");
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);
            if (tTo256)
            {
                return pre + TruecolorTo256(r, g, b) + "m";
            }
            else
            {
                return pre + r + ";" + g + ";" + b + "m";
            }
        }

        private static int[] HexToDec(string hexa)
        {
            if (hexa.Length > 1)
            {
                hexa = hexa.Substring(1);
                foreach (char c in hexa)
                {
                    if (!Uri.IsHexDigit(c))
                    {
                        return new int[] { -1, -1, -1 };
                    }
                }

                if (hexa.Length == 2)
                {
                    int hInt = Convert.ToInt32(hexa, 16);
                    return new int[] { hInt, hInt, hInt };
                }
                else if (hexa.Length == 6)
                {
                    return new int[]
                    {
                        Convert.ToInt32(hexa.Substring(0, 2), 16),
                        Convert.ToInt32(hexa.Substring(2, 2), 16),
                        Convert.ToInt32(hexa.Substring(4, 2), 16)
                    };
                }
            }
            return new int[] { -1, -1, -1 };
        }

        private static void GenerateColors(Dictionary<string, string> source)
        {
            Colors.Clear();
            Rgbs.Clear();
            foreach (var kvp in DefaultTheme)
            {
                string name = kvp.Key;
                string color = kvp.Value;
                if (name == "main_bg" && !Config.GetB("theme_background"))
                {
                    Colors[name] = "\x1b[49m";
                    Rgbs[name] = new int[] { -1, -1, -1 };
                    continue;
                }
                string depth = (name.EndsWith("bg") && name != "meter_bg") ? "bg" : "fg";
                if (source.ContainsKey(name))
                {
                    if (name == "main_bg" && string.IsNullOrEmpty(source[name]))
                    {
                        Colors[name] = "\x1b[49m";
                        Rgbs[name] = new int[] { -1, -1, -1 };
                        continue;
                    }
                    else if (string.IsNullOrEmpty(source[name]) && (name.EndsWith("_mid") || name.EndsWith("_end")))
                    {
                        Colors[name] = "";
                        Rgbs[name] = new int[] { -1, -1, -1 };
                        continue;
                    }
                    else if (source[name].StartsWith("#"))
                    {
                        Colors[name] = HexToColor(source[name], Config.GetB("lowcolor"), depth);
                        Rgbs[name] = HexToDec(source[name]);
                    }
                    else if (!string.IsNullOrEmpty(source[name]))
                    {
                        string[] tRgb = source[name].Split(' ');
                        if (tRgb.Length != 3)
                        {
                            Console.WriteLine("Invalid RGB decimal value: \"" + source[name] + "\"");
                        }
                        else
                        {
                            Colors[name] = DecToColor(int.Parse(tRgb[0]), int.Parse(tRgb[1]), int.Parse(tRgb[2]), Config.GetB("lowcolor"), depth);
                            Rgbs[name] = new int[] { int.Parse(tRgb[0]), int.Parse(tRgb[1]), int.Parse(tRgb[2]) };
                        }
                    }
                }
                if (!Colors.ContainsKey(name) && !new[] { "meter_bg", "process_start", "process_mid", "process_end", "graph_text" }.Contains(name))
                {
                    Console.WriteLine("Missing color value for \"" + name + "\". Using value from default.");
                    Colors[name] = HexToColor(color, Config.GetB("lowcolor"), depth);
                    Rgbs[name] = HexToDec(color);
                }
            }
            if (!Colors.ContainsKey("meter_bg"))
            {
                Colors["meter_bg"] = Colors["inactive_fg"];
                Rgbs["meter_bg"] = Rgbs["inactive_fg"];
            }
            if (!Colors.ContainsKey("process_start"))
            {
                Colors["process_start"] = Colors["cpu_start"];
                Colors["process_mid"] = Colors["cpu_mid"];
                Colors["process_end"] = Colors["cpu_end"];
                Rgbs["process_start"] = Rgbs["cpu_start"];
                Rgbs["process_mid"] = Rgbs["cpu_mid"];
                Rgbs["process_end"] = Rgbs["cpu_end"];
            }
            if (!Colors.ContainsKey("graph_text"))
            {
                Colors["graph_text"] = Colors["inactive_fg"];
                Rgbs["graph_text"] = Rgbs["inactive_fg"];
            }
            if (!Colors.ContainsKey("virtual_start"))
            {
                Colors["virtual_start"] = Colors["cached_start"];
                Colors["virtual_mid"] = Colors["cached_mid"];
                Colors["virtual_end"] = Colors["cached_end"];
                Rgbs["virtual_start"] = Rgbs["cached_start"];
                Rgbs["virtual_mid"] = Rgbs["cached_mid"];
                Rgbs["virtual_end"] = Rgbs["cached_end"];
            }
        }

        private static void GenerateGradients()
        {
            Gradients.Clear();
            bool tTo256 = Config.GetB("lowcolor");

            Rgbs["proc_start"] = Rgbs["main_fg"];
            Rgbs["proc_mid"] = new int[] { -1, -1, -1 };
            Rgbs["proc_end"] = Rgbs["inactive_fg"];
            Rgbs["proc_color_start"] = Rgbs["inactive_fg"];
            Rgbs["proc_color_mid"] = new int[] { -1, -1, -1 };
            Rgbs["proc_color_end"] = Rgbs["process_start"];

            foreach (var kvp in Rgbs)
            {
                if (!kvp.Key.EndsWith("_start")) continue;
                string colorName = kvp.Key.Substring(0, kvp.Key.Length - 6);

                int[][] inputColors = new int[][]
                {
                    kvp.Value,
                    Rgbs[colorName + "_mid"],
                    Rgbs[colorName + "_end"]
                };

                int[][] outputColors = new int[101][];
                outputColors[0] = new int[] { -1, -1, -1 };

                if (inputColors[2][0] >= 0)
                {
                    int currentRange = (inputColors[1][0] >= 0) ? 50 : 100;
                    for (int rgb = 0; rgb < 3; rgb++)
                    {
                        int start = 0, offset = 0;
                        int end = (currentRange == 50) ? 1 : 2;
                        for (int i = 0; i <= 100; i++)
                        {
                            outputColors[i] = new int[3];
                            outputColors[i][rgb] = inputColors[start][rgb] + (i - offset) * (inputColors[end][rgb] - inputColors[start][rgb]) / currentRange;

                            if (i == currentRange)
                            {
                                start++;
                                end++;
                                offset = 50;
                            }
                        }
                    }
                }

                string[] colorGradient = new string[101];
                if (outputColors[0][0] != -1)
                {
                    for (int y = 0; y <= 100; y++)
                    {
                        colorGradient[y] = DecToColor(outputColors[y][0], outputColors[y][1], outputColors[y][2], tTo256);
                    }
                }
                else
                {
                    for (int y = 0; y <= 100; y++)
                    {
                        colorGradient[y] = Colors[kvp.Key];
                    }
                }
                Gradients[colorName] = colorGradient;
            }
        }

        private static void GenerateTtyColors()
        {
            Rgbs.Clear();
            Gradients.Clear();
            Colors = new Dictionary<string, string>(TtyTheme);
            if (!Config.GetB("theme_background"))
            {
                Colors["main_bg"] = "\x1b[49m";
            }

            foreach (var kvp in Colors)
            {
                if (!kvp.Key.EndsWith("_start")) continue;
                string baseName = kvp.Key.Substring(0, kvp.Key.Length - 6);
                string section = "_start";
                int split = string.IsNullOrEmpty(Colors[baseName + "_mid"]) ? 50 : 33;
                for (int i = 0; i <= 100; i++)
                {
                    Gradients[baseName][i] = Colors[baseName + section];
                    if (i == split)
                    {
                        section = (split == 33) ? "_mid" : "_end";
                        split *= 2;
                    }
                }
            }
        }

        private static Dictionary<string, string> LoadFile(string filename)
        {
            Dictionary<string, string> themeOut = new Dictionary<string, string>();
            if (!File.Exists(filename))
            {
                return new Dictionary<string, string>(DefaultTheme);
            }

            using (StreamReader themeFile = new StreamReader(filename))
            {
                Console.WriteLine("Loading theme file: " + filename);
                while (!themeFile.EndOfStream)
                {
                    themeFile.ReadLine();
                    string line = themeFile.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(']');
                    if (parts.Length < 2) continue;
                    string name = parts[0].TrimStart('[');
                    if (!DefaultTheme.ContainsKey(name)) continue;
                    string value = parts[1].Split('=')[1].Trim();
                    themeOut[name] = value;
                }
            }
            return themeOut;
        }

        public static void UpdateThemes()
        {
            Themes.Clear();
            Themes.Add("Default");
            Themes.Add("TTY");

            foreach (string path in new[] { UserThemeDir, ThemeDir })
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*.theme"))
                {
                    Themes.Add(file);
                }
            }
        }

        public static void SetTheme()
        {
            string theme = Config.GetS("color_theme");
            string themePath = Themes.FirstOrDefault(t => t == theme || Path.GetFileNameWithoutExtension(t) == theme || Path.GetFileName(t) == theme);
            if (theme == "TTY" || Config.GetB("tty_mode"))
            {
                GenerateTtyColors();
            }
            else
            {
                GenerateColors(theme == "Default" || string.IsNullOrEmpty(themePath) ? DefaultTheme : LoadFile(themePath));
                GenerateGradients();
            }
            Term.Fg = Colors["main_fg"];
            Term.Bg = Colors["main_bg"];
            Fx.Reset = Fx.ResetBase + Term.Fg + Term.Bg;
        }
    }
}
