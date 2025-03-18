using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace btop4win
{
    public static class Btop
    {
        public static class Global
        {
            public static readonly List<string[]> BannerSrc = new List<string[]>
            {
                new string[] { "#E62525", "██████╗ ████████╗ ██████╗ ██████╗" },
                new string[] { "#CD2121", "██╔══██╗╚══██╔══╝██╔═══██╗██╔══██╗   ██╗    ██╗" },
                new string[] { "#B31D1D", "██████╔╝   ██║   ██║   ██║██████╔╝ ██████╗██████╗" },
                new string[] { "#9A1919", "██╔══██╗   ██║   ██║   ██║██╔═══╝  ╚═██╔═╝╚═██╔═╝" },
                new string[] { "#801414", "██████╔╝   ██║   ╚██████╔╝██║        ╚═╝    ╚═╝" },
                new string[] { "#000000", "╚═════╝    ╚═╝    ╚═════╝ ╚═╝" },
            };
            public static readonly string Version = "1.0.4";

            public static int CoreCount;
            public static string Overlay;
            public static string Clock;

            public static string SelfPath;

            public static string ExitErrorMsg;
            public static bool ThreadException = false;

            public static bool DebugInit = false;
            public static bool Debug = false;
            public static bool UtfForce = false;

            public static ulong StartTime;

            public static bool Resized = false;
            public static bool Quitting = false;
            public static bool ShouldQuit = false;
            public static bool ShouldSleep = false;
            public static bool RunnerStarted = false;

            public static bool ArgTty = false;
            public static bool ArgLowColor = false;
            public static int ArgPreset = -1;
        }

        public static void ArgumentParser(int argc, string[] argv)
        {
            for (int i = 1; i < argc; i++)
            {
                string argument = argv[i];
                if (argument == "-h" || argument == "--help")
                {
                    Console.WriteLine("usage: btop [-h] [-v] [-/+t] [-p <id>] [--utf-force] [--debug]\n\n" +
                                      "optional arguments:\n" +
                                      "  -h, --help            show this help message and exit\n" +
                                      "  -v, --version         show version info and exit\n" +
                                      "  -lc, --low-color      disable truecolor, converts 24-bit colors to 256-color\n" +
                                      "  -t, --tty_on          force (ON) tty mode, max 16 colors and tty friendly graph symbols\n" +
                                      "  +t, --tty_off         force (OFF) tty mode\n" +
                                      "  -p, --preset <id>     start with preset, integer value between 0-9\n" +
                                      "  --debug               start in DEBUG mode: shows microsecond timer for information collect\n" +
                                      "                        and screen draw functions and sets loglevel to DEBUG\n");
                    Environment.Exit(0);
                }
                else if (argument == "-v" || argument == "--version")
                {
                    Console.WriteLine("btop4win version: " + Global.Version);
                    Environment.Exit(0);
                }
                else if (argument == "-lc" || argument == "--low-color")
                {
                    Global.ArgLowColor = true;
                }
                else if (argument == "-t" || argument == "--tty_on")
                {
                    BtopConfig.Set("tty_mode", true);
                    Global.ArgTty = true;
                }
                else if (argument == "+t" || argument == "--tty_off")
                {
                    BtopConfig.Set("tty_mode", false);
                    Global.ArgTty = true;
                }
                else if (argument == "-p" || argument == "--preset")
                {
                    if (++i >= argc)
                    {
                        Console.WriteLine("ERROR: Preset option needs an argument.");
                        Environment.Exit(1);
                    }
                    else if (int.TryParse(argv[i], out int val) && val >= 0 && val <= 9)
                    {
                        Global.ArgPreset = Math.Clamp(val, 0, 9);
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Preset option only accepts an integer value between 0-9.");
                        Environment.Exit(1);
                    }
                }
                else if (argument == "--debug")
                {
                    Global.Debug = true;
                }
                else
                {
                    Console.WriteLine(" Unknown argument: " + argument + "\n" +
                                      " Use -h or --help for help.");
                    Environment.Exit(1);
                }
            }
        }

        public static void TermResize(bool force)
        {
            if (BtopInput.Polling)
            {
                Global.Resized = true;
                BtopInput.Interrupt = true;
                return;
            }

            if (BtopTerm.Refresh(true) || force)
            {
                if (force && BtopTerm.Refresh(true)) force = false;
            }
            else return;

            Global.Resized = true;
            if (BtopRunner.Active) BtopRunner.Stop();
            BtopTerm.Refresh();
            BtopConfig.Unlock();

            var boxes = BtopConfig.GetS("shown_boxes");
            var minSize = BtopTerm.GetMinSize(boxes);

            while (!force || (BtopTerm.Width < minSize[0] || BtopTerm.Height < minSize[1]))
            {
                Thread.Sleep(100);
                if (BtopTerm.Width < minSize[0] || BtopTerm.Height < minSize[1])
                {
                    Console.WriteLine(BtopTerm.Clear + BtopGlobal.BgBlack + BtopGlobal.FgWhite + BtopMv.To((BtopTerm.Height / 2) - 2, (BtopTerm.Width / 2) - 11) +
                                      "Terminal size too small:" + BtopMv.To((BtopTerm.Height / 2) - 1, (BtopTerm.Width / 2) - 10) +
                                      " Width = " + (BtopTerm.Width < minSize[1] ? BtopGlobal.FgRed : BtopGlobal.FgGreen) + BtopTerm.Width +
                                      BtopGlobal.FgWhite + " Height = " + (BtopTerm.Height < minSize[0] ? BtopGlobal.FgRed : BtopGlobal.FgGreen) + BtopTerm.Height +
                                      BtopMv.To((BtopTerm.Height / 2) + 1, (BtopTerm.Width / 2) - 12) + BtopGlobal.FgWhite +
                                      "Needed for current config:" + BtopMv.To((BtopTerm.Height / 2) + 2, (BtopTerm.Width / 2) - 10) +
                                      "Width = " + minSize[0] + " Height = " + minSize[1]);
                    bool gotKey = false;
                    for (; !BtopTerm.Refresh() && !gotKey; gotKey = BtopInput.Poll(10)) ;
                    if (gotKey)
                    {
                        var key = BtopInput.Get();
                        if (key == "q")
                            CleanQuit(0);
                        else if (new[] { "1", "2", "3", "4" }.Contains(key))
                        {
                            BtopConfig.CurrentPreset = -1;
                            BtopConfig.ToggleBox(new[] { "cpu", "mem", "net", "proc" }[int.Parse(key) - 1]);
                            boxes = BtopConfig.GetS("shown_boxes");
                        }
                    }
                    minSize = BtopTerm.GetMinSize(boxes);
                }
                else if (!BtopTerm.Refresh()) break;
            }

            BtopInput.Interrupt = true;
        }

        public static void CleanQuit(int sig)
        {
            if (Global.Quitting) return;
            Global.Quitting = true;
            BtopRunner.Stop();

            BtopConfig.Write();

            if (BtopTerm.Initialized)
            {
                BtopTerm.Restore();
            }

            if (!string.IsNullOrEmpty(Global.ExitErrorMsg))
            {
                sig = 1;
                BtopLogger.Error(Global.ExitErrorMsg);
                Console.Error.WriteLine(BtopGlobal.FgRed + "ERROR: " + BtopGlobal.FgWhite + Global.ExitErrorMsg + BtopFx.Reset);
            }
            BtopLogger.Info("Quitting! Runtime: " + BtopTools.SecToDhms(BtopTools.TimeS() - Global.StartTime));

            Environment.Exit(sig != -1 ? sig : 0);
        }

        public static void ExitHandler()
        {
            CleanQuit(-1);
        }

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        public static bool CtrlHandler(CtrlTypes fdwCtrlType)
        {
            switch (fdwCtrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    CleanQuit(0);
                    return true;
                default:
                    return false;
            }
        }

        public static class BtopRunner
        {
            public static bool Active = false;
            public static bool Stopping = false;
            public static bool Waiting = false;
            public static bool Redraw = false;

            public static string Output;
            public static string EmptyBg;
            public static bool PauseOutput = false;

            public static string DebugBg;
            public static Dictionary<string, ulong[]> DebugTimes = new Dictionary<string, ulong[]>();

            public struct RunnerConf
            {
                public List<string> Boxes;
                public bool NoUpdate;
                public bool ForceRedraw;
                public bool BackgroundUpdate;
                public string Overlay;
                public string Clock;
            }

            public static RunnerConf CurrentConf;

            public static void DebugTimer(string name, int action)
            {
                switch (action)
                {
                    case 0:
                        DebugTimes[name][0] = BtopTools.TimeMicros();
                        return;
                    case 1:
                        DebugTimes[name][1] = BtopTools.TimeMicros();
                        DebugTimes[name][0] = DebugTimes[name][1] - DebugTimes[name][0];
                        DebugTimes["total"][0] += DebugTimes[name][0];
                        return;
                    case 2:
                        DebugTimes[name][1] = BtopTools.TimeMicros() - DebugTimes[name][1];
                        DebugTimes["total"][1] += DebugTimes[name][1];
                        return;
                }
            }

            public static void Runner()
            {
                while (!Global.Quitting)
                {
                    Thread.Sleep(10);
                    if (Active)
                    {
                        Global.ExitErrorMsg = "Runner thread failed to get active lock!";
                        Global.ThreadException = true;
                        BtopInput.Interrupt = true;
                        Stopping = true;
                    }
                    if (Stopping || Global.Resized)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    Active = true;

                    var conf = CurrentConf;

                    if (Global.Debug)
                    {
                        if (string.IsNullOrEmpty(DebugBg) || Redraw) DebugBg = BtopDraw.CreateBox(2, 2, 32, 10, "", true, "debug");
                        DebugTimes.Clear();
                        DebugTimes["total"] = new ulong[] { 0, 0 };
                    }

                    Output = string.Empty;

                    try
                    {
                        if (conf.Boxes.Contains("cpu"))
                        {
                            try
                            {
                                if (Global.Debug) DebugTimer("cpu", 0);

                                var cpu = BtopCpu.Collect(conf.NoUpdate);

                                if (Global.Debug) DebugTimer("cpu", 1);

                                if (!PauseOutput) Output += BtopCpu.Draw(cpu, conf.ForceRedraw, conf.NoUpdate);

                                if (Global.Debug) DebugTimer("cpu", 2);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Cpu:: -> " + e.Message);
                            }
                        }

                        if (conf.Boxes.Contains("mem"))
                        {
                            try
                            {
                                if (Global.Debug) DebugTimer("mem", 0);

                                var mem = BtopMem.Collect(conf.NoUpdate);

                                if (Global.Debug) DebugTimer("mem", 1);

                                if (!PauseOutput) Output += BtopMem.Draw(mem, conf.ForceRedraw, conf.NoUpdate);

                                if (Global.Debug) DebugTimer("mem", 2);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Mem:: -> " + e.Message);
                            }
                        }

                        if (conf.Boxes.Contains("net"))
                        {
                            try
                            {
                                if (Global.Debug) DebugTimer("net", 0);

                                var net = BtopNet.Collect(conf.NoUpdate);

                                if (Global.Debug) DebugTimer("net", 1);

                                if (!PauseOutput) Output += BtopNet.Draw(net, conf.ForceRedraw, conf.NoUpdate);

                                if (Global.Debug) DebugTimer("net", 2);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Net:: -> " + e.Message);
                            }
                        }

                        if (conf.Boxes.Contains("proc"))
                        {
                            try
                            {
                                if (Global.Debug) DebugTimer("proc", 0);

                                var proc = BtopProc.Collect(conf.NoUpdate);

                                if (Global.Debug) DebugTimer("proc", 1);

                                if (!PauseOutput) Output += BtopProc.Draw(proc, conf.ForceRedraw, conf.NoUpdate);

                                if (Global.Debug) DebugTimer("proc", 2);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Proc:: -> " + e.Message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Global.ExitErrorMsg = "Exception in runner thread -> " + e.Message;
                        Global.ThreadException = true;
                        BtopInput.Interrupt = true;
                        Stopping = true;
                    }

                    if (Stopping)
                    {
                        continue;
                    }

                    if (Redraw || conf.ForceRedraw)
                    {
                        EmptyBg = string.Empty;
                        Redraw = false;
                    }

                    if (!PauseOutput) Output += conf.Clock;
                    if (!string.IsNullOrEmpty(conf.Overlay) && !conf.BackgroundUpdate) PauseOutput = true;
                    if (string.IsNullOrEmpty(Output) && !PauseOutput)
                    {
                        if (string.IsNullOrEmpty(EmptyBg))
                        {
                            int x = BtopTerm.Width / 2 - 10, y = BtopTerm.Height / 2 - 10;
                            Output += BtopTerm.Clear;
                            EmptyBg += BtopDraw.BannerGen(y, 0, true) +
                                       BtopMv.To(y + 6, x) + BtopTheme.C("title") + BtopFx.B + "No boxes shown!" +
                                       BtopMv.To(y + 8, x) + BtopTheme.C("hi_fg") + "1" + BtopTheme.C("main_fg") + " | Show CPU box" +
                                       BtopMv.To(y + 9, x) + BtopTheme.C("hi_fg") + "2" + BtopTheme.C("main_fg") + " | Show MEM box" +
                                       BtopMv.To(y + 10, x) + BtopTheme.C("hi_fg") + "3" + BtopTheme.C("main_fg") + " | Show NET box" +
                                       BtopMv.To(y + 11, x) + BtopTheme.C("hi_fg") + "4" + BtopTheme.C("main_fg") + " | Show PROC box" +
                                       BtopMv.To(y + 12, x - 2) + BtopTheme.C("hi_fg") + "esc" + BtopTheme.C("main_fg") + " | Show menu" +
                                       BtopMv.To(y + 13, x) + BtopTheme.C("hi_fg") + "q" + BtopTheme.C("main_fg") + " | Quit";
                        }
                        Output += EmptyBg;
                    }

                    if (Global.Debug && !BtopMenu.Active)
                    {
                        Output += DebugBg + BtopTheme.C("title") + BtopFx.B + BtopTools.Ljust(" Box", 9) + BtopTools.Ljust("Collect us", 12, true) + BtopTools.Ljust("Draw us", 9, true) + BtopTheme.C("main_fg") + BtopFx.Ub;
                        foreach (var name in new[] { "cpu", "mem", "net", "proc", "total" })
                        {
                            if (!DebugTimes.ContainsKey(name)) DebugTimes[name] = new ulong[] { 0, 0 };
                            var timeCollect = DebugTimes[name][0];
                            var timeDraw = DebugTimes[name][1];
                            if (name == "total") Output += BtopFx.B;
                            Output += BtopMv.L(29) + BtopMv.D(1) + BtopTools.Ljust(name, 8) + BtopTools.Ljust(timeCollect.ToString(), 12) + BtopTools.Ljust(timeDraw.ToString(), 9);
                        }
                        Output += BtopMv.L(29) + BtopMv.D(1) + BtopTools.Ljust("*WMI", 8) + BtopTools.Ljust(BtopProc.WMITimer.ToString(), 12) + BtopTools.Ljust("0", 9);
                    }

                    Console.WriteLine(BtopTerm.SyncStart + (string.IsNullOrEmpty(conf.Overlay)
                            ? Output
                            : (string.IsNullOrEmpty(Output) ? "" : BtopFx.Ub + BtopTheme.C("inactive_fg") + BtopFx.Uncolor(Output)) + conf.Overlay) +
                        BtopTerm.HideCursor + BtopTerm.SyncEnd);
                }
            }

            public static void Run(string box, bool noUpdate, bool forceRedraw)
            {
                if (Active)
                {
                    return;
                }

                if (Stopping || Global.Resized) return;

                if (box == "overlay")
                {
                    Console.WriteLine(BtopTerm.SyncStart + Global.Overlay + BtopTerm.SyncEnd);
                }
                else if (box == "clock")
                {
                    Console.WriteLine(BtopTerm.SyncStart + Global.Clock + BtopTerm.SyncEnd);
                }
                else
                {
                    BtopConfig.Unlock();
                    BtopConfig.Lock();

                    CurrentConf = new RunnerConf
                    {
                        Boxes = box == "all" ? BtopConfig.CurrentBoxes : new List<string> { box },
                        NoUpdate = noUpdate,
                        ForceRedraw = forceRedraw,
                        BackgroundUpdate = !BtopConfig.GetB("tty_mode") && BtopConfig.GetB("background_update"),
                        Overlay = Global.Overlay,
                        Clock = Global.Clock
                    };

                    if (BtopMenu.Active && !CurrentConf.BackgroundUpdate) Global.Overlay = string.Empty;

                    ThreadPool.QueueUserWorkItem(_ => Runner());
                }
            }

            public static void Stop()
            {
                Stopping = true;
                Thread.Sleep(10);
                Stopping = false;
            }
        }

        public static void Main(string[] args)
        {
            Global.StartTime = BtopTools.TimeS();

            if (args.Length > 0) ArgumentParser(args.Length, args);

            SetConsoleCtrlHandler(CtrlHandler, true);

            Console.Title = "btop4win++";

            Global.SelfPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            BtopConfig.ConfDir = Global.SelfPath;
            BtopConfig.ConfFile = Path.Combine(BtopConfig.ConfDir, "btop.conf");
            BtopLogger.Logfile = Path.Combine(BtopConfig.ConfDir, "btop.log");
            BtopTheme.ThemeDir = Path.Combine(BtopConfig.ConfDir, "themes");

            var loadWarnings = new List<string>();
            BtopConfig.Load(BtopConfig.ConfFile, loadWarnings);

            if (BtopConfig.CurrentBoxes.Count == 0) BtopConfig.CheckBoxes(BtopConfig.GetS("shown_boxes"));
            BtopConfig.Set("lowcolor", Global.ArgLowColor ? true : !BtopConfig.GetB("truecolor"));

            if (Global.Debug)
            {
                BtopLogger.Set("DEBUG");
                BtopLogger.Debug("Starting in DEBUG mode!");
            }
            else BtopLogger.Set(BtopConfig.GetS("log_level"));

            BtopLogger.Info("Logger set to " + (Global.Debug ? "DEBUG" : BtopConfig.GetS("log_level")));

            foreach (var errStr in loadWarnings) BtopLogger.Warning(errStr);

            if (!BtopTerm.Init())
            {
                Global.ExitErrorMsg = "No tty detected!\nbtop4win needs an interactive shell to run.";
                CleanQuit(1);
            }

            int tCount = 0;
            while (BtopTerm.Width <= 0 || BtopTerm.Width > 10000 || BtopTerm.Height <= 0 || BtopTerm.Height > 10000)
            {
                Thread.Sleep(10);
                BtopTerm.Refresh();
                if (++tCount == 100)
                {
                    Global.ExitErrorMsg = "Failed to get size of terminal!";
                    CleanQuit(1);
                }
            }

            try
            {
                BtopShared.Init();
            }
            catch (Exception e)
            {
                Global.ExitErrorMsg = "Exception in Shared::init() -> " + e.Message;
                CleanQuit(1);
            }

            BtopTheme.UpdateThemes();
            BtopTheme.SetTheme();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ExitHandler();

            Thread runnerThread = new Thread(BtopRunner.Runner);
            runnerThread.Start();
            if (!runnerThread.IsAlive)
            {
                Global.ExitErrorMsg = "Failed to create _runner thread!";
                CleanQuit(1);
            }
            else
            {
                Global.RunnerStarted = true;
            }

            BtopConfig.PresetsValid(BtopConfig.GetS("presets"));
            if (Global.ArgPreset >= 0)
            {
                BtopConfig.CurrentPreset = Math.Min(Global.ArgPreset, BtopConfig.PresetList.Count - 1);
                BtopConfig.ApplyPreset(BtopConfig.PresetList[BtopConfig.CurrentPreset]);
            }

            var minSize = BtopTerm.GetMinSize(BtopConfig.GetS("shown_boxes"));
            if (BtopTerm.Height < minSize[1] || BtopTerm.Width < minSize[0])
            {
                TermResize(true);
                Global.Resized = false;
                BtopInput.Interrupt = false;
            }

            BtopDraw.CalcSizes();

            Console.WriteLine(BtopTerm.SyncStart + BtopCpu.Box + BtopMem.Box + BtopNet.Box + BtopProc.Box + BtopTerm.SyncEnd);

            ulong updateMs = (ulong)BtopConfig.GetI("update_ms");
            ulong futureTime = BtopTools.TimeMs();

            try
            {
                while (true)
                {
                    if (Global.ThreadException) CleanQuit(1);
                    else if (Global.ShouldQuit) CleanQuit(0);

                    TermResize(Global.Resized);

                    if (Global.Resized)
                    {
                        BtopDraw.CalcSizes();
                        BtopDraw.UpdateClock(true);
                        Global.Resized = false;
                        if (BtopMenu.Active) BtopMenu.Process();
                        else BtopRunner.Run("all", true, true);
                    }

                    if (BtopDraw.UpdateClock() && !BtopMenu.Active)
                    {
                        BtopRunner.Run("clock");
                    }

                    if (BtopTools.TimeMs() >= futureTime && !Global.Resized)
                    {
                        BtopRunner.Run("all");
                        updateMs = (ulong)BtopConfig.GetI("update_ms");
                        futureTime = BtopTools.TimeMs() + updateMs;
                    }

                    for (ulong currentTime = BtopTools.TimeMs(); currentTime < futureTime; currentTime = BtopTools.TimeMs())
                    {
                        if (updateMs != (ulong)BtopConfig.GetI("update_ms"))
                        {
                            updateMs = (ulong)BtopConfig.GetI("update_ms");
                            futureTime = BtopTools.TimeMs() + updateMs;
                        }
                        else if (futureTime - currentTime > updateMs)
                            futureTime = currentTime;

                        else if (BtopInput.Poll(Math.Min((ulong)1000, futureTime - currentTime)))
                        {
                            if (!BtopRunner.Active) BtopConfig.Unlock();

                            if (BtopMenu.Active) BtopMenu.Process(BtopInput.Get());
                            else BtopInput.Process(BtopInput.Get());
                        }

                        else break;
                    }
                }
            }
            catch (Exception e)
            {
                Global.ExitErrorMsg = "Exception in main loop -> " + e.Message;
                CleanQuit(1);
            }
        }
    }
}
