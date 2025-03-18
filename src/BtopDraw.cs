using System;
using System.Collections.Generic;
using System.Linq;

namespace btop4win
{
    public static class Symbols
    {
        public const string HLine = "─";
        public const string VLine = "│";
        public const string DottedVLine = "╎";
        public const string LeftUp = "┌";
        public const string RightUp = "┐";
        public const string LeftDown = "└";
        public const string RightDown = "┘";
        public const string RoundLeftUp = "╭";
        public const string RoundRightUp = "╮";
        public const string RoundLeftDown = "╰";
        public const string RoundRightDown = "╯";
        public const string TitleLeftDown = "┘";
        public const string TitleRightDown = "└";
        public const string TitleLeft = "┐";
        public const string TitleRight = "┌";
        public const string DivRight = "┤";
        public const string DivLeft = "├";
        public const string DivUp = "┬";
        public const string DivDown = "┴";

        public const string Up = "↑";
        public const string Down = "↓";
        public const string Left = "←";
        public const string Right = "→";
        public const string Enter = "┙";

        public const string Meter = "■";

        public static readonly string[] Superscript = { "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹" };

        public static readonly Dictionary<string, List<string>> GraphSymbols = new Dictionary<string, List<string>>
        {
            { "braille_up", new List<string> {
                " ", "⢀", "⢠", "⢰", "⢸",
                "⡀", "⣀", "⣠", "⣰", "⣸",
                "⡄", "⣄", "⣤", "⣴", "⣼",
                "⡆", "⣆", "⣦", "⣶", "⣾",
                "⡇", "⣇", "⣧", "⣷", "⣿"
            }},
            { "braille_down", new List<string> {
                " ", "⠈", "⠘", "⠸", "⢸",
                "⠁", "⠉", "⠙", "⠹", "⢹",
                "⠃", "⠋", "⠛", "⠻", "⢻",
                "⠇", "⠏", "⠟", "⠿", "⢿",
                "⡇", "⡏", "⡟", "⡿", "⣿"
            }},
            { "block_up", new List<string> {
                " ", "▗", "▗", "▐", "▐",
                "▖", "▄", "▄", "▟", "▟",
                "▖", "▄", "▄", "▟", "▟",
                "▌", "▙", "▙", "█", "█",
                "▌", "▙", "▙", "█", "█"
            }},
            { "block_down", new List<string> {
                " ", "▝", "▝", "▐", "▐",
                "▘", "▀", "▀", "▜", "▜",
                "▘", "▀", "▀", "▜", "▜",
                "▌", "▛", "▛", "█", "█",
                "▌", "▛", "▛", "█", "█"
            }},
            { "tty_up", new List<string> {
                " ", "░", "░", "▒", "▒",
                "░", "░", "▒", "▒", "█",
                "░", "▒", "▒", "▒", "█",
                "▒", "▒", "▒", "█", "█",
                "▒", "█", "█", "█", "█"
            }},
            { "tty_down", new List<string> {
                " ", "░", "░", "▒", "▒",
                "░", "░", "▒", "▒", "█",
                "░", "▒", "▒", "▒", "█",
                "▒", "▒", "▒", "█", "█",
                "▒", "█", "█", "█", "█"
            }}
        };
    }

    public static class Draw
    {
        private static string banner;
        private static bool bannerGenerated = false;

        public static string BannerGen(int y = 0, int x = 0, bool centered = false, bool redraw = false)
        {
            if (bannerGenerated && !redraw)
                return banner;

            banner = "btop4win";
            bannerGenerated = true;
            return banner;
        }

        public class TextEdit
        {
            private int pos = 0;
            private int upos = 0;
            private bool numeric;
            public string Text { get; private set; }

            public TextEdit()
            {
                Text = string.Empty;
            }

            public TextEdit(string text, bool numeric = false)
            {
                Text = text;
                this.numeric = numeric;
            }

            public bool Command(string key)
            {
                switch (key)
                {
                    case "left":
                        if (pos > 0) pos--;
                        break;
                    case "right":
                        if (pos < Text.Length) pos++;
                        break;
                    case "home":
                        pos = 0;
                        break;
                    case "end":
                        pos = Text.Length;
                        break;
                    case "backspace":
                        if (pos > 0)
                        {
                            Text = Text.Remove(pos - 1, 1);
                            pos--;
                        }
                        break;
                    case "delete":
                        if (pos < Text.Length)
                        {
                            Text = Text.Remove(pos, 1);
                        }
                        break;
                    case "clear":
                        Text = string.Empty;
                        pos = 0;
                        break;
                    default:
                        if (!numeric || (numeric && char.IsDigit(key[0])))
                        {
                            Text = Text.Insert(pos, key);
                            pos++;
                        }
                        break;
                }
                return true;
            }

            public string GetText(int limit = 0)
            {
                if (limit > 0 && Text.Length > limit)
                {
                    return Text.Substring(0, limit);
                }
                return Text;
            }

            public void Clear()
            {
                Text = string.Empty;
                pos = 0;
            }
        }

        public static string CreateBox(int x, int y, int width, int height, string lineColor = "", bool fill = false, string title = "", string title2 = "", int num = 0)
        {
            string outStr = "";
            if (string.IsNullOrEmpty(lineColor)) lineColor = BtopTheme.C("div_line");
            var ttyMode = BtopConfig.GetB("tty_mode");
            var rounded = BtopConfig.GetB("rounded_corners");
            var numbering = (num == 0) ? "" : BtopTheme.C("hi_fg") + (ttyMode ? num.ToString() : Symbols.Superscript[Math.Clamp(num, 0, 9)]);
            var rightUp = (ttyMode || !rounded ? Symbols.RightUp : Symbols.RoundRightUp);
            var leftUp = (ttyMode || !rounded ? Symbols.LeftUp : Symbols.RoundLeftUp);
            var rightDown = (ttyMode || !rounded ? Symbols.RightDown : Symbols.RoundRightDown);
            var leftDown = (ttyMode || !rounded ? Symbols.LeftDown : Symbols.RoundLeftDown);

            outStr = BtopFx.Reset + lineColor;

            // Draw horizontal lines
            foreach (var hpos in new[] { y, y + height - 1 })
            {
                outStr += BtopMv.To(hpos, x) + new string(Symbols.HLine[0], width - 1);
            }

            // Draw vertical lines and fill if enabled
            for (var hpos = y + 1; hpos < y + height - 1; hpos++)
            {
                outStr += BtopMv.To(hpos, x) + Symbols.VLine
                    + ((fill) ? new string(' ', width - 2) : BtopMv.R(width - 2))
                    + Symbols.VLine;
            }

            // Draw corners
            outStr += BtopMv.To(y, x) + leftUp
                + BtopMv.To(y, x + width - 1) + rightUp
                + BtopMv.To(y + height - 1, x) + leftDown
                + BtopMv.To(y + height - 1, x + width - 1) + rightDown;

            // Draw titles if defined
            if (!string.IsNullOrEmpty(title))
            {
                outStr += BtopMv.To(y, x + 2) + Symbols.TitleLeft + BtopFx.B + numbering + BtopTheme.C("title") + title
                    + BtopFx.Ub + lineColor + Symbols.TitleRight;
            }
            if (!string.IsNullOrEmpty(title2))
            {
                outStr += BtopMv.To(y + height - 1, x + 2) + Symbols.TitleLeftDown + BtopFx.B + numbering + BtopTheme.C("title") + title2
                    + BtopFx.Ub + lineColor + Symbols.TitleRightDown;
            }

            return outStr + BtopFx.Reset + BtopMv.To(y + 1, x + 1);
        }

        public static bool UpdateClock(bool force = false)
        {
            var clockFormat = BtopConfig.GetS("clock_format");
            if (!BtopCpu.Shown || string.IsNullOrEmpty(clockFormat))
            {
                if (string.IsNullOrEmpty(clockFormat) && !string.IsNullOrEmpty(Global.Clock)) Global.Clock = string.Empty;
                return false;
            }

            var clockCustomFormat = new Dictionary<string, string>
            {
                { "/user", BtopTools.Username() },
                { "/host", BtopTools.Hostname() },
                { "/uptime", "" }
            };
            ulong cTime = 0;
            int clockLen = 0;
            string clockStr = "";

            if (BtopTools.TimeMs() / 1000 == cTime && !force)
                return false;
            else
            {
                cTime = BtopTools.TimeMs() / 1000;
                var newClock = BtopTools.StrfTime(clockFormat);
                if (newClock == clockStr && !force) return false;
                clockStr = newClock;
            }

            var outStr = Global.Clock;
            var cpuBottom = BtopConfig.GetB("cpu_bottom");
            var x = BtopCpu.X;
            var y = (cpuBottom ? BtopCpu.Y + BtopCpu.Height - 1 : BtopCpu.Y);
            var width = BtopCpu.Width;
            var titleLeft = (cpuBottom ? Symbols.TitleLeftDown : Symbols.TitleLeft);
            var titleRight = (cpuBottom ? Symbols.TitleRightDown : Symbols.TitleRight);

            foreach (var kvp in clockCustomFormat)
            {
                if (clockStr.Contains(kvp.Key))
                {
                    if (kvp.Key == "/uptime")
                    {
                        var upstr = BtopTools.SecToDhms(BtopTools.SystemUptime());
                        if (upstr.Length > 8) upstr = upstr.Substring(0, upstr.Length - 3);
                        clockStr = clockStr.Replace(kvp.Key, upstr);
                    }
                    else
                    {
                        clockStr = clockStr.Replace(kvp.Key, kvp.Value);
                    }
                }
            }

            clockStr = BtopTools.Uresize(clockStr, Math.Max(10, width - 66 - (BtopTerm.Width >= 100 && BtopConfig.GetB("show_battery") && BtopCpu.HasBattery ? 22 : 0)));
            outStr = string.Empty;

            if (clockStr.Length != clockLen)
            {
                if (!Global.Resized && clockLen > 0)
                    outStr = BtopMv.To(y, x + (width / 2) - (clockLen / 2)) + BtopFx.Ub + BtopTheme.C("cpu_box") + new string(Symbols.HLine[0], clockLen);
                clockLen = clockStr.Length;
            }

            outStr += BtopMv.To(y, x + (width / 2) - (clockLen / 2)) + BtopFx.Ub + BtopTheme.C("cpu_box") + titleLeft
                + BtopTheme.C("title") + BtopFx.B + clockStr + BtopTheme.C("cpu_box") + BtopFx.Ub + titleRight;

            Global.Clock = outStr;
            return true;
        }

        public class Meter
        {
            private int width;
            private string colorGradient;
            private bool invert;
            private string[] cache;

            public Meter()
            {
                cache = new string[101];
            }

            public Meter(int width, string colorGradient, bool invert = false)
            {
                this.width = width;
                this.colorGradient = colorGradient;
                this.invert = invert;
                cache = new string[101];
            }

            public string GetMeter(int value)
            {
                if (width < 1) return "";
                value = Math.Clamp(value, 0, 100);
                if (!string.IsNullOrEmpty(cache[value])) return cache[value];
                var outStr = cache[value];
                for (var i = 1; i <= width; i++)
                {
                    var y = (int)Math.Round((double)i * 100.0 / width);
                    if (value >= y)
                        outStr += BtopTheme.G(colorGradient)[invert ? 100 - y : y] + Symbols.Meter;
                    else
                    {
                        outStr += BtopTheme.C("meter_bg") + new string(Symbols.Meter[0], width + 1 - i);
                        break;
                    }
                }
                outStr += BtopFx.Reset;
                cache[value] = outStr;
                return outStr;
            }
        }

        public class Graph
        {
            private int width, height;
            private string colorGradient;
            private string output, symbol = "default";
            private bool invert, noZero;
            private long offset;
            private long last = 0, maxValue = 0;
            private bool current = true, ttyMode = false;
            private Dictionary<bool, List<string>> graphs = new Dictionary<bool, List<string>> { { true, new List<string>() }, { false, new List<string>() } };

            public Graph()
            {
            }

            public Graph(int width, int height, string colorGradient, Queue<long> data, string symbol = "default", bool invert = false, bool noZero = false, long maxValue = 0, long offset = 0)
            {
                this.width = width;
                this.height = height;
                this.colorGradient = colorGradient;
                this.symbol = symbol;
                this.invert = invert;
                this.noZero = noZero;
                this.maxValue = maxValue;
                this.offset = offset;
                Create(data, 0);
            }

            private void Create(Queue<long> data, int dataOffset)
            {
                var mult = (data.Count - dataOffset > 1);
                var graphSymbol = Symbols.GraphSymbols[symbol + '_' + (invert ? "down" : "up")];
                var result = new int[2];
                var mod = (height == 1) ? 0.3f : 0.1f;
                long dataValue = 0;
                if (mult && dataOffset > 0)
                {
                    last = data.ElementAt(dataOffset - 1);
                    if (maxValue > 0) last = Math.Clamp((last + offset) * 100 / maxValue, 0, 100);
                }

                for (var i = dataOffset; i < data.Count; i++)
                {
                    if (!ttyMode && mult) current = !current;
                    if (i < 0)
                    {
                        dataValue = 0;
                        last = 0;
                    }
                    else
                    {
                        dataValue = data.ElementAt(i);
                        if (maxValue > 0) dataValue = Math.Clamp((dataValue + offset) * 100 / maxValue, 0, 100);
                    }

                    for (var horizon = 0; horizon < height; horizon++)
                    {
                        var curHigh = (height > 1) ? (int)Math.Round(100.0 * (height - horizon) / height) : 100;
                        var curLow = (height > 1) ? (int)Math.Round(100.0 * (height - (horizon + 1)) / height) : 0;
                        for (var ai = 0; ai < 2; ai++)
                        {
                            var value = (ai == 0) ? last : dataValue;
                            var clampMin = (noZero && horizon == height - 1 && !(mult && i == dataOffset && ai == 0)) ? 1 : 0;
                            if (value >= curHigh)
                                result[ai] = 4;
                            else if (value <= curLow)
                                result[ai] = clampMin;
                            else
                            {
                                result[ai] = Math.Clamp((int)Math.Round((float)(value - curLow) * 4 / (curHigh - curLow) + mod), clampMin, 4);
                            }
                        }
                        if (height == 1)
                        {
                            if (result[0] + result[1] == 0) graphs[current][horizon] += BtopMv.R(1);
                            else
                            {
                                if (!string.IsNullOrEmpty(colorGradient)) graphs[current][horizon] += BtopTheme.G(colorGradient)[Math.Clamp(Math.Max(last, dataValue), 0, 100)];
                                graphs[current][horizon] += graphSymbol[result[0] * 5 + result[1]];
                            }
                        }
                        else graphs[current][horizon] += graphSymbol[result[0] * 5 + result[1]];
                    }
                    if (mult && i >= 0) last = dataValue;
                }
                last = dataValue;
                output = string.Empty;
                if (height == 1)
                {
                    if (!string.IsNullOrEmpty(colorGradient))
                        output += (last < 1 && !string.IsNullOrEmpty(colorGradient) ? BtopTheme.C("inactive_fg") : BtopTheme.G(colorGradient)[Math.Clamp(last, 0, 100)]);
                    output += graphs[current][0];
                }
                else
                {
                    for (var i = 1; i <= height; i++)
                    {
                        if (i > 1) output += BtopMv.D(1) + BtopMv.L(width);
                        if (!string.IsNullOrEmpty(colorGradient))
                            output += (invert) ? BtopTheme.G(colorGradient)[i * 100 / height] : BtopTheme.G(colorGradient)[100 - ((i - 1) * 100 / height)];
                        output += (invert) ? graphs[current][height - i] : graphs[current][i - 1];
                    }
                }
                if (!string.IsNullOrEmpty(colorGradient)) output += BtopFx.Reset;
            }

            public string GetGraph(Queue<long> data, bool dataSame = false)
            {
                if (dataSame) return output;

                if (!ttyMode) current = !current;
                for (var i = 0; i < height; i++)
                {
                    if (height == 1 && graphs[current][i][1] == '[')
                    {
                        if (graphs[current][i][3] == 'C') graphs[current][i] = graphs[current][i].Substring(4);
                        else graphs[current][i] = graphs[current][i].Substring(graphs[current][i].IndexOf('m') + 4);
                    }
                    else if (graphs[current][i][0] == ' ') graphs[current][i] = graphs[current][i].Substring(1);
                    else graphs[current][i] = graphs[current][i].Substring(3);
                }
                Create(data, data.Count - 1);
                return output;
            }

            public string GetGraph()
            {
                return output;
            }
        }

        public static void CalcSizes()
        {
            BtopRunner.Active = false;
            BtopConfig.Unlock();
            var boxes = BtopConfig.GetS("shown_boxes");
            var cpuBottom = BtopConfig.GetB("cpu_bottom");
            var memBelowNet = BtopConfig.GetB("mem_below_net");
            var procLeft = BtopConfig.GetB("proc_left");

            BtopCpu.Box = string.Empty;
            BtopMem.Box = string.Empty;
            BtopNet.Box = string.Empty;
            BtopProc.Box = string.Empty;
            Global.Clock = string.Empty;
            Global.Overlay = string.Empty;
            BtopRunner.PauseOutput = false;
            BtopRunner.Redraw = true;
            BtopProc.PCounters.Clear();
            BtopProc.PGraphs.Clear();
            if (BtopMenu.Active) BtopMenu.Redraw = true;

            BtopInput.MouseMappings.Clear();

            BtopCpu.X = BtopMem.X = BtopNet.X = BtopProc.X = 1;
            BtopCpu.Y = BtopMem.Y = BtopNet.Y = BtopProc.Y = 1;
            BtopCpu.Width = BtopMem.Width = BtopNet.Width = BtopProc.Width = 0;
            BtopCpu.Height = BtopMem.Height = BtopNet.Height = BtopProc.Height = 0;
            BtopCpu.Redraw = BtopMem.Redraw = BtopNet.Redraw = BtopProc.Redraw = true;

            BtopCpu.Shown = boxes.Contains("cpu");
            BtopMem.Shown = boxes.Contains("mem");
            BtopNet.Shown = boxes.Contains("net");
            BtopProc.Shown = boxes.Contains("proc");

            if (BtopCpu.Shown)
            {
                var showGpu = BtopConfig.GetB("show_gpu") && BtopCpu.HasGpu;
                BtopCpu.Width = (int)Math.Round((double)BtopTerm.Width * BtopCpu.WidthP / 100);
                BtopCpu.Height = Math.Max(8, (int)Math.Ceiling((double)BtopTerm.Height * (boxes.Trim() == "cpu" ? 100 : BtopCpu.HeightP) / 100));
                BtopCpu.X = 1;
                BtopCpu.Y = cpuBottom ? BtopTerm.Height - BtopCpu.Height + 1 : 1;

                BtopCpu.BColumns = Math.Max(1, (int)Math.Ceiling((double)(BtopShared.CoreCount + 1) / (BtopCpu.Height - 5 - (showGpu ? 1 : 0))));
                if (BtopCpu.BColumns * 33 < BtopCpu.Width - (BtopCpu.Width / 3))
                {
                    BtopCpu.BColumnSize = 2;
                    BtopCpu.BWidth = 33 * BtopCpu.BColumns - (BtopCpu.BColumns - 1);
                }
                else if (BtopCpu.BColumns * 21 < BtopCpu.Width - (BtopCpu.Width / 3))
                {
                    BtopCpu.BColumnSize = 1;
                    BtopCpu.BWidth = 21 * BtopCpu.BColumns - (BtopCpu.BColumns - 1);
                }
                else if (BtopCpu.BColumns * 14 < BtopCpu.Width - (BtopCpu.Width / 3))
                {
                    BtopCpu.BColumnSize = 0;
                }
                else
                {
                    BtopCpu.BColumns = (BtopCpu.Width - BtopCpu.Width / 3) / 14;
                    BtopCpu.BColumnSize = 0;
                }

                if (BtopCpu.BColumnSize == 0) BtopCpu.BWidth = 8 * BtopCpu.BColumns + 1;
                BtopCpu.BHeight = Math.Min(BtopCpu.Height - 2, (int)Math.Ceiling((double)BtopShared.CoreCount / BtopCpu.BColumns) + 4 + (showGpu ? 1 : 0));

                BtopCpu.BX = BtopCpu.X + BtopCpu.Width - BtopCpu.BWidth - 1;
                BtopCpu.BY = BtopCpu.Y + (int)Math.Ceiling((double)(BtopCpu.Height - 2) / 2) - (int)Math.Ceiling((double)BtopCpu.BHeight / 2) + 1;

                BtopCpu.Box = CreateBox(BtopCpu.X, BtopCpu.Y, BtopCpu.Width, BtopCpu.Height, BtopTheme.C("cpu_box"), true, (cpuBottom ? "" : "cpu"), (cpuBottom ? "cpu" : ""), 1);

                var custom = BtopConfig.GetS("custom_cpu_name");
                var cpuTitle = BtopTools.Uresize(string.IsNullOrEmpty(custom) ? BtopCpu.CpuName : custom, BtopCpu.BWidth - 14);
                BtopCpu.Box += CreateBox(BtopCpu.BX, BtopCpu.BY, BtopCpu.BWidth, BtopCpu.BHeight, "", false, cpuTitle);
            }

            if (BtopMem.Shown)
            {
                var showDisks = BtopConfig.GetB("show_disks");
                var memGraphs = BtopConfig.GetB("mem_graphs");
                var hasGpu = BtopCpu.HasGpu && BtopConfig.GetB("show_gpu");

                BtopMem.Width = (int)Math.Round((double)BtopTerm.Width * (BtopProc.Shown ? BtopMem.WidthP : 100) / 100);
                BtopMem.Height = (int)Math.Ceiling((double)BtopTerm.Height * (100 - BtopCpu.HeightP * BtopCpu.Shown - BtopNet.HeightP * BtopNet.Shown) / 100) + 1;
                if (BtopMem.Height + BtopCpu.Height > BtopTerm.Height) BtopMem.Height = BtopTerm.Height - BtopCpu.Height;
                BtopMem.X = (procLeft && BtopProc.Shown) ? BtopTerm.Width - BtopMem.Width + 1 : 1;
                if (memBelowNet && BtopNet.Shown)
                    BtopMem.Y = cpuBottom ? 1 : BtopCpu.Height + 1;
                else
                    BtopMem.Y = cpuBottom ? 1 : BtopCpu.Height + 1;

                if (showDisks)
                {
                    BtopMem.MemWidth = (int)Math.Ceiling((double)(BtopMem.Width - 3) / 2);
                    BtopMem.MemWidth += BtopMem.MemWidth % 2;
                    BtopMem.DisksWidth = BtopMem.Width - BtopMem.MemWidth - 2;
                    BtopMem.Divider = BtopMem.X + BtopMem.MemWidth;
                }
                else
                    BtopMem.MemWidth = BtopMem.Width - 1;

                BtopMem.ItemHeight = 4 + BtopMem.HasSwap + hasGpu;
                if (BtopMem.Height - (2 + BtopMem.HasSwap * 2 + hasGpu * 2) > 2 * BtopMem.ItemHeight)
                    BtopMem.MemSize = 3;
                else if (BtopMem.MemWidth > 25)
                    BtopMem.MemSize = 2;
                else
                    BtopMem.MemSize = 1;

                BtopMem.MemMeter = Math.Max(0, BtopMem.MemWidth - (BtopMem.MemSize > 2 ? 7 : 18));
                if (BtopMem.MemSize == 1) BtopMem.MemMeter += 6;

                if (memGraphs)
                {
                    BtopMem.GraphHeight = Math.Max(1, (int)Math.Round((double)((BtopMem.Height - (1 + BtopMem.HasSwap * 2 + hasGpu * 2)) - (BtopMem.MemSize == 3 ? 2 : 1) * BtopMem.ItemHeight) / BtopMem.ItemHeight));
                    if (BtopMem.GraphHeight > 1) BtopMem.MemMeter += 6;
                }
                else
                    BtopMem.GraphHeight = 0;

                if (showDisks)
                {
                    BtopMem.DiskMeter = Math.Max(-14, BtopMem.Width - BtopMem.MemWidth - 23);
                    if (BtopMem.DisksWidth < 25) BtopMem.DiskMeter += 14;
                }

                BtopMem.Box = CreateBox(BtopMem.X, BtopMem.Y, BtopMem.Width, BtopMem.Height, BtopTheme.C("mem_box"), true, "mem", "", 2);
                BtopMem.Box += BtopMv.To(BtopMem.Y, (showDisks ? BtopMem.Divider + 2 : BtopMem.X + BtopMem.Width - 9)) + BtopTheme.C("mem_box") + Symbols.TitleLeft + (showDisks ? BtopFx.B : "")
                    + BtopTheme.C("hi_fg") + 'd' + BtopTheme.C("title") + "isks" + BtopFx.Ub + BtopTheme.C("mem_box") + Symbols.TitleRight;
                BtopInput.MouseMappings["d"] = new BtopInput.MouseLoc { Y = BtopMem.Y, X = (showDisks ? BtopMem.Divider + 3 : BtopMem.X + BtopMem.Width - 8), Height = 1, Width = 5 };
                if (showDisks)
                {
                    BtopMem.Box += BtopMv.To(BtopMem.Y, BtopMem.Divider) + Symbols.DivUp + BtopMv.To(BtopMem.Y + BtopMem.Height - 1, BtopMem.Divider) + Symbols.DivDown + BtopTheme.C("div_line");
                    for (var i = 1; i < BtopMem.Height - 1; i++)
                        BtopMem.Box += BtopMv.To(BtopMem.Y + i, BtopMem.Divider) + Symbols.VLine;
                }
            }

            if (BtopNet.Shown)
            {
                BtopNet.Width = (int)Math.Round((double)BtopTerm.Width * (BtopProc.Shown ? BtopNet.WidthP : 100) / 100);
                BtopNet.Height = BtopTerm.Height - BtopCpu.Height - BtopMem.Height;
                BtopNet.X = (procLeft && BtopProc.Shown) ? BtopTerm.Width - BtopNet.Width + 1 : 1;
                if (memBelowNet && BtopMem.Shown)
                    BtopNet.Y = cpuBottom ? 1 : BtopCpu.Height + 1;
                else
                    BtopNet.Y = BtopTerm.Height - BtopNet.Height + 1 - (cpuBottom ? BtopCpu.Height : 0);

                BtopNet.BWidth = (BtopNet.Width > 45) ? 27 : 19;
                BtopNet.BHeight = (BtopNet.Height > 10) ? 9 : BtopNet.Height - 2;
                BtopNet.BX = BtopNet.X + BtopNet.Width - BtopNet.BWidth - 1;
                BtopNet.BY = BtopNet.Y + ((BtopNet.Height - 2) / 2) - BtopNet.BHeight / 2 + 1;
                BtopNet.DGraphHeight = (int)Math.Round((double)(BtopNet.Height - 2) / 2);
                BtopNet.UGraphHeight = BtopNet.Height - 2 - BtopNet.DGraphHeight;

                BtopNet.Box = CreateBox(BtopNet.X, BtopNet.Y, BtopNet.Width, BtopNet.Height, BtopTheme.C("net_box"), true, "net", "", 3);
                BtopNet.Box += CreateBox(BtopNet.BX, BtopNet.BY, BtopNet.BWidth, BtopNet.BHeight, "", false, "download", "upload");
            }

            if (BtopProc.Shown)
            {
                BtopProc.Width = BtopTerm.Width - (BtopMem.Shown ? BtopMem.Width : (BtopNet.Shown ? BtopNet.Width : 0));
                BtopProc.Height = BtopTerm.Height - BtopCpu.Height;
                BtopProc.X = procLeft ? 1 : BtopTerm.Width - BtopProc.Width + 1;
                BtopProc.Y = (cpuBottom && BtopCpu.Shown) ? 1 : BtopCpu.Height + 1;
                BtopProc.SelectMax = BtopProc.Height - 3;
                BtopProc.Box = CreateBox(BtopProc.X, BtopProc.Y, BtopProc.Width, BtopProc.Height, BtopTheme.C("proc_box"), true, "proc", "", 4);
            }
        }
    }

    public static class Proc
    {
        public static Draw.TextEdit Filter = new Draw.TextEdit();
        public static Dictionary<int, Draw.Graph> PGraphs = new Dictionary<int, Draw.Graph>();
        public static Dictionary<int, int> PCounters = new Dictionary<int, int>();
    }
}
