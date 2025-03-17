using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace btop4win
{
    public static class Input
    {
        private static readonly Dictionary<char, string> KeyEscapes = new Dictionary<char, string>
        {
            { (char)ConsoleKey.Escape, "escape" },
            { (char)ConsoleKey.Enter, "enter" },
            { (char)ConsoleKey.Spacebar, "space" },
            { (char)ConsoleKey.Backspace, "backspace" },
            { (char)ConsoleKey.UpArrow, "up" },
            { (char)ConsoleKey.DownArrow, "down" },
            { (char)ConsoleKey.LeftArrow, "left" },
            { (char)ConsoleKey.RightArrow, "right" },
            { (char)ConsoleKey.Insert, "insert" },
            { (char)ConsoleKey.Delete, "delete" },
            { (char)ConsoleKey.Home, "home" },
            { (char)ConsoleKey.End, "end" },
            { (char)ConsoleKey.PageUp, "page_up" },
            { (char)ConsoleKey.PageDown, "page_down" },
            { (char)ConsoleKey.Tab, "tab" },
            { (char)ConsoleKey.F1, "f1" },
            { (char)ConsoleKey.F2, "f2" },
            { (char)ConsoleKey.F3, "f3" },
            { (char)ConsoleKey.F4, "f4" },
            { (char)ConsoleKey.F5, "f5" },
            { (char)ConsoleKey.F6, "f6" },
            { (char)ConsoleKey.F7, "f7" },
            { (char)ConsoleKey.F8, "f8" },
            { (char)ConsoleKey.F9, "f9" },
            { (char)ConsoleKey.F10, "f10" },
            { (char)ConsoleKey.F11, "f11" },
            { (char)ConsoleKey.F12, "f12" }
        };

        public static bool Interrupt { get; set; } = false;
        public static bool Polling { get; set; } = false;
        public static int[] MousePos { get; set; } = new int[2];
        public static Dictionary<string, MouseLoc> MouseMappings { get; set; } = new Dictionary<string, MouseLoc>();

        public static Queue<string> History { get; set; } = new Queue<string>(Enumerable.Repeat("", 50));
        public static int LastMouseButton { get; set; } = 0;
        public static string OldFilter { get; set; } = string.Empty;

        public static bool Poll(int timeout)
        {
            while (timeout > 0)
            {
                if (Interrupt)
                {
                    Interrupt = false;
                    return false;
                }

                if (Console.KeyAvailable)
                    return true;

                Thread.Sleep(timeout < 10 ? timeout : 10);
                timeout -= 10;
            }
            return false;
        }

        public static string Get()
        {
            if (!Console.KeyAvailable)
                return string.Empty;

            var keyInfo = Console.ReadKey(intercept: true);
            if (KeyEscapes.ContainsKey(keyInfo.KeyChar))
            {
                var key = KeyEscapes[keyInfo.KeyChar];
                if (key == "tab" && (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
                    key = "shift_tab";
                return key;
            }
            else if (keyInfo.KeyChar > 32 && keyInfo.KeyChar < 126)
            {
                var key = keyInfo.KeyChar.ToString();
                History.Enqueue(key);
                if (History.Count > 50)
                    History.Dequeue();
                return key;
            }
            return string.Empty;
        }

        public static void Process(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                var filtering = Config.GetBool("proc_filtering");
                var vimKeys = Config.GetBool("vim_keys");
                var helpKey = vimKeys ? "H" : "h";
                var killKey = vimKeys ? "K" : "k";

                if (!filtering)
                {
                    if (key.ToLower() == "q")
                    {
                        Program.CleanQuit(0);
                    }
                    else if (new[] { "escape", "m" }.Contains(key))
                    {
                        Menu.Show(Menu.Menus.Main);
                        return;
                    }
                    else if (new[] { "f1", helpKey }.Contains(key))
                    {
                        Menu.Show(Menu.Menus.Help);
                        return;
                    }
                    else if (new[] { "f2", "o" }.Contains(key))
                    {
                        Menu.Show(Menu.Menus.Options);
                        return;
                    }
                    else if (new[] { "1", "2", "3", "4" }.Contains(key))
                    {
                        Runner.WaitForActive();
                        Config.CurrentPreset = -1;
                        var boxes = new[] { "cpu", "mem", "net", "proc" };
                        Config.ToggleBox(boxes[int.Parse(key) - 1]);
                        Draw.CalcSizes();
                        Runner.Run("all", false, true);
                        return;
                    }
                    else if (new[] { "p", "P" }.Contains(key) && Config.PresetList.Count > 1)
                    {
                        if (key == "p")
                        {
                            if (++Config.CurrentPreset >= Config.PresetList.Count)
                                Config.CurrentPreset = 0;
                        }
                        else
                        {
                            if (--Config.CurrentPreset < 0)
                                Config.CurrentPreset = Config.PresetList.Count - 1;
                        }
                        Runner.WaitForActive();
                        Config.ApplyPreset(Config.PresetList[Config.CurrentPreset]);
                        Draw.CalcSizes();
                        Runner.Run("all", false, true);
                        return;
                    }
                }

                if (Proc.Shown)
                {
                    if (filtering)
                    {
                        if (key == "enter" || key == "down")
                        {
                            Config.Set("proc_filter", Proc.Filter.Text);
                            Config.Set("proc_filtering", false);
                            OldFilter = string.Empty;
                            if (key == "down")
                            {
                                Process("down");
                                return;
                            }
                        }
                        else if (key == "escape" || key == "mouse_click")
                        {
                            Config.Set("proc_filter", OldFilter);
                            Config.Set("proc_filtering", false);
                            OldFilter = string.Empty;
                        }
                        else if (Proc.Filter.Command(key))
                        {
                            if (Config.GetString("proc_filter") != Proc.Filter.Text)
                                Config.Set("proc_filter", Proc.Filter.Text);
                        }
                        else
                            return;
                    }
                    else if (key == "left" || (vimKeys && key == "h"))
                    {
                        if (Config.GetBool("proc_services"))
                        {
                            var curIndex = Proc.SortVectorService.IndexOf(Config.GetString("services_sorting"));
                            if (--curIndex < 0)
                                curIndex = Proc.SortVectorService.Count - 1;
                            Config.Set("services_sorting", Proc.SortVectorService[curIndex]);
                        }
                        else
                        {
                            var curIndex = Proc.SortVector.IndexOf(Config.GetString("proc_sorting"));
                            if (--curIndex < 0)
                                curIndex = Proc.SortVector.Count - 1;
                            Config.Set("proc_sorting", Proc.SortVector[curIndex]);
                        }
                    }
                    else if (key == "right" || (vimKeys && key == "l"))
                    {
                        if (Config.GetBool("proc_services"))
                        {
                            var curIndex = Proc.SortVectorService.IndexOf(Config.GetString("services_sorting"));
                            if (++curIndex >= Proc.SortVectorService.Count)
                                curIndex = 0;
                            Config.Set("services_sorting", Proc.SortVectorService[curIndex]);
                        }
                        else
                        {
                            var curIndex = Proc.SortVector.IndexOf(Config.GetString("proc_sorting"));
                            if (++curIndex >= Proc.SortVector.Count)
                                curIndex = 0;
                            Config.Set("proc_sorting", Proc.SortVector[curIndex]);
                        }
                    }
                    else if (new[] { "f", "/" }.Contains(key))
                    {
                        Config.Toggle("proc_filtering");
                        Proc.Filter = new Draw.TextEdit(Config.GetString("proc_filter"));
                        OldFilter = Proc.Filter.Text;
                    }
                    else if (key == "e" && !Config.GetBool("proc_services"))
                    {
                        Config.Toggle("proc_tree");
                    }
                    else if (key == "r")
                    {
                        Config.Toggle("proc_reversed");
                    }
                    else if (key == "c")
                    {
                        Config.Toggle("proc_per_core");
                    }
                    else if (key == "delete" && !string.IsNullOrEmpty(Config.GetString("proc_filter")))
                    {
                        Config.Set("proc_filter", string.Empty);
                    }
                    else if (key.StartsWith("mouse_"))
                    {
                        var col = MousePos[0];
                        var line = MousePos[1];
                        var y = Config.GetBool("show_detailed") ? Proc.Y + 8 : Proc.Y;
                        var height = Config.GetBool("show_detailed") ? Proc.Height - 8 : Proc.Height;
                        if (col >= Proc.X + 1 && col < Proc.X + Proc.Width && line >= y + 1 && line < y + height - 1)
                        {
                            if (key == "mouse_click")
                            {
                                if (col < Proc.X + Proc.Width - 2)
                                {
                                    var currentSelection = Config.GetInt("proc_selected");
                                    if (currentSelection == line - y - 1)
                                    {
                                        if (!Config.GetBool("proc_services") && Config.GetBool("proc_tree"))
                                        {
                                            var xPos = col - Proc.X;
                                            var offset = Config.GetInt("selected_depth") * 3;
                                            if (xPos > offset && xPos < 4 + offset)
                                            {
                                                Process("space");
                                                return;
                                            }
                                        }
                                        Process("enter");
                                        return;
                                    }
                                    else if (currentSelection == 0 || line - y - 1 == 0)
                                    {
                                        Config.Set("proc_selected", line - y - 1);
                                    }
                                }
                                else if (line == y + 1)
                                {
                                    if (Proc.Selection("page_up") == -1)
                                        return;
                                }
                                else if (line == y + height - 2)
                                {
                                    if (Proc.Selection("page_down") == -1)
                                        return;
                                }
                                else if (Proc.Selection("mousey" + (line - y - 2)) == -1)
                                    return;
                            }
                        }
                    }
                    else if (key == "enter")
                    {
                        if (Config.GetInt("proc_selected") == 0 && !Config.GetBool("show_detailed"))
                        {
                            return;
                        }
                        else if (Config.GetInt("proc_selected") > 0 && (Config.GetInt("detailed_pid") != Config.GetInt("selected_pid") || Config.GetString("detailed_name") != Config.GetString("selected_name")))
                        {
                            Config.Set("detailed_pid", Config.GetInt("selected_pid"));
                            Config.Set("detailed_name", Config.GetString("selected_name"));
                            Config.Set("proc_last_selected", Config.GetInt("proc_selected"));
                            Config.Set("proc_selected", 0);
                            Config.Set("show_detailed", true);
                        }
                        else if (Config.GetBool("show_detailed"))
                        {
                            if (Config.GetInt("proc_last_selected") > 0)
                                Config.Set("proc_selected", Config.GetInt("proc_last_selected"));
                            Config.Set("proc_last_selected", 0);
                            Config.Set("detailed_pid", 0);
                            Config.Set("detailed_name", string.Empty);
                            Config.Set("show_detailed", false);
                        }
                    }
                    else if (new[] { "+", "-", "space" }.Contains(key) && !Config.GetBool("proc_services") && Config.GetBool("proc_tree") && Config.GetInt("proc_selected") > 0)
                    {
                        Runner.WaitForActive();
                        var pid = Config.GetInt("selected_pid");
                        if (key == "+" || key == "space")
                            Proc.Expand = pid;
                        if (key == "-" || key == "space")
                            Proc.Collapse = pid;
                    }
                    else if (new[] { "t", killKey }.Contains(key) && (Config.GetBool("show_detailed") || Config.GetInt("selected_pid") > 0 || !string.IsNullOrEmpty(Config.GetString("selected_name"))))
                    {
                        Runner.WaitForActive();
                        if (!Config.GetBool("proc_services") && Config.GetBool("show_detailed") && Config.GetInt("proc_selected") == 0 && Proc.Detailed.Status == "Stopped")
                            return;
                        Menu.Show(Menu.Menus.SignalSend);
                        return;
                    }
                    else if (key == "u" && Config.GetBool("proc_services") && Config.GetBool("show_detailed"))
                    {
                        Runner.WaitForActive();
                        if (!Proc.Detailed.CanPause || Proc.Detailed.Status == "Stopped")
                            return;
                        Menu.Show(Menu.Menus.SignalPause);
                        return;
                    }
                    else if (key == "S" && Config.GetBool("proc_services") && Config.GetBool("show_detailed"))
                    {
                        Menu.Show(Menu.Menus.SignalConfig);
                        return;
                    }
                    else if (key == "s")
                    {
                        Runner.WaitForActive();
                        Config.Toggle("proc_services");
                        Config.Set("proc_selected", 0);
                        Config.Set("proc_last_selected", 0);
                        Config.Set("detailed_pid", 0);
                        Config.Set("detailed_name", string.Empty);
                        Config.Set("show_detailed", false);
                    }
                    else if (new[] { "up", "down", "page_up", "page_down", "home", "end" }.Contains(key) || (vimKeys && new[] { "j", "k", "g", "G" }.Contains(key)))
                    {
                        var oldSelected = Config.GetInt("proc_selected");
                        var newSelected = Proc.Selection(key);
                        if (newSelected == -1)
                            return;
                        else if (oldSelected != newSelected && (oldSelected == 0 || newSelected == 0))
                        {
                            Config.Set("proc_selected", newSelected);
                        }
                    }
                }

                if (Cpu.Shown)
                {
                    if (key == "+" && Config.GetInt("update_ms") <= 86399900)
                    {
                        var add = (Config.GetInt("update_ms") <= 86399000 && Tools.TimeMs() - Tools.TimeMs() <= 200 && History.All(str => str == "+")) ? 1000 : 100;
                        Config.Set("update_ms", Config.GetInt("update_ms") + add);
                    }
                    else if (key == "-" && Config.GetInt("update_ms") >= 200)
                    {
                        var sub = (Config.GetInt("update_ms") >= 2000 && Tools.TimeMs() - Tools.TimeMs() <= 200 && History.All(str => str == "-")) ? 1000 : 100;
                        Config.Set("update_ms", Config.GetInt("update_ms") - sub);
                    }
                }

                if (Mem.Shown)
                {
                    if (key == "i")
                    {
                        Config.Toggle("io_mode");
                    }
                    else if (key == "d")
                    {
                        Config.Toggle("show_disks");
                        Draw.CalcSizes();
                    }
                }

                if (Net.Shown)
                {
                    if (new[] { "b", "n" }.Contains(key))
                    {
                        Runner.WaitForActive();
                        var cIndex = Net.Interfaces.IndexOf(Net.SelectedIface);
                        if (cIndex != -1)
                        {
                            if (key == "b")
                            {
                                if (--cIndex < 0)
                                    cIndex = Net.Interfaces.Count - 1;
                            }
                            else if (key == "n")
                            {
                                if (++cIndex == Net.Interfaces.Count)
                                    cIndex = 0;
                            }
                            Net.SelectedIface = Net.Interfaces[cIndex];
                            Net.Rescale = true;
                        }
                    }
                    else if (key == "y")
                    {
                        Config.Toggle("net_sync");
                        Net.Rescale = true;
                    }
                    else if (key == "a")
                    {
                        Config.Toggle("net_auto");
                        Net.Rescale = true;
                    }
                    else if (key == "z")
                    {
                        Runner.WaitForActive();
                        var ndev = Net.CurrentNet[Net.SelectedIface];
                        if (ndev.Stat["download"].Offset + ndev.Stat["upload"].Offset > 0)
                        {
                            ndev.Stat["download"].Offset = 0;
                            ndev.Stat["upload"].Offset = 0;
                        }
                        else
                        {
                            ndev.Stat["download"].Offset = ndev.Stat["download"].Last + ndev.Stat["download"].Rollover;
                            ndev.Stat["upload"].Offset = ndev.Stat["upload"].Last + ndev.Stat["upload"].Rollover;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Input::Process(\"" + key + "\") : " + e.Message);
            }
        }
    }

    public class MouseLoc
    {
        public int Line { get; set; }
        public int Col { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
}
