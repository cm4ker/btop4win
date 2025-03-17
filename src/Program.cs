using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace btop4win
{
    class Program
    {
        static void Main(string[] args)
        {
            // Argument parsing
            if (args.Contains("-h") || args.Contains("--help"))
            {
                Console.WriteLine("usage: btop [-h] [-v] [-/+t] [-p <id>] [--utf-force] [--debug]");
                Console.WriteLine("\noptional arguments:");
                Console.WriteLine("  -h, --help            show this help message and exit");
                Console.WriteLine("  -v, --version         show version info and exit");
                Console.WriteLine("  -lc, --low-color      disable truecolor, converts 24-bit colors to 256-color");
                Console.WriteLine("  -t, --tty_on          force (ON) tty mode, max 16 colors and tty friendly graph symbols");
                Console.WriteLine("  +t, --tty_off         force (OFF) tty mode");
                Console.WriteLine("  -p, --preset <id>     start with preset, integer value between 0-9");
                Console.WriteLine("  --debug               start in DEBUG mode: shows microsecond timer for information collect");
                Console.WriteLine("                        and screen draw functions and sets loglevel to DEBUG");
                return;
            }
            else if (args.Contains("-v") || args.Contains("--version"))
            {
                Console.WriteLine("btop4win version: 1.0.4");
                return;
            }

            // Initialize terminal and set options
            if (!Term.Init())
            {
                Console.WriteLine("No tty detected!\nbtop4win needs an interactive shell to run.");
                return;
            }

            // Check for valid terminal dimensions
            int t_count = 0;
            while (Term.Width <= 0 || Term.Width > 10000 || Term.Height <= 0 || Term.Height > 10000)
            {
                Thread.Sleep(10);
                Term.Refresh();
                if (++t_count == 100)
                {
                    Console.WriteLine("Failed to get size of terminal!");
                    return;
                }
            }

            // Collector init and error check
            try
            {
                Shared.Init();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Shared::Init() -> " + e.Message);
                return;
            }

            // Update list of available themes and generate the selected theme
            Theme.UpdateThemes();
            Theme.SetTheme();

            // Setup signal handler for exit
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => CleanQuit(0);

            // Start runner thread
            Task.Run(() => Runner.RunnerThread());

            // Calculate sizes of all boxes
            Config.PresetsValid(Config.Get("presets"));
            if (Global.ArgPreset >= 0)
            {
                Config.CurrentPreset = Math.Min(Global.ArgPreset, Config.PresetList.Count - 1);
                Config.ApplyPreset(Config.PresetList[Config.CurrentPreset]);
            }

            var minSize = Term.GetMinSize(Config.Get("shown_boxes"));
            if (Term.Height < minSize[1] || Term.Width < minSize[0])
            {
                TermResize(true);
                Global.Resized = false;
                Input.Interrupt = false;
            }

            Draw.CalcSizes();

            // Print out box outlines
            Console.WriteLine(Term.SyncStart + Cpu.Box + Mem.Box + Net.Box + Proc.Box + Term.SyncEnd);

            // Main loop
            uint updateMs = Config.GetInt("update_ms");
            var futureTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            try
            {
                while (true)
                {
                    // Check for exceptions in secondary thread and exit with fail signal if true
                    if (Global.ThreadException) CleanQuit(1);
                    else if (Global.ShouldQuit) CleanQuit(0);

                    // Make sure terminal size hasn't changed (in case of SIGWINCH not working properly)
                    TermResize(Global.Resized);

                    // Trigger secondary thread to redraw if terminal has been resized
                    if (Global.Resized)
                    {
                        Draw.CalcSizes();
                        Draw.UpdateClock(true);
                        Global.Resized = false;
                        if (Menu.Active) Menu.Process();
                        else Runner.Run("all", true, true);
                        while (Runner.Active) Thread.Sleep(10);
                    }

                    // Update clock if needed
                    if (Draw.UpdateClock() && !Menu.Active)
                    {
                        Runner.Run("clock");
                    }

                    // Start secondary collect & draw thread at the interval set by <update_ms> config value
                    if (DateTimeOffset.Now.ToUnixTimeMilliseconds() >= futureTime && !Global.Resized)
                    {
                        Runner.Run("all");
                        updateMs = Config.GetInt("update_ms");
                        futureTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + updateMs;
                    }

                    // Loop over input polling and input action processing
                    for (var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(); currentTime < futureTime; currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds())
                    {
                        // Check for external clock changes and for changes to the update timer
                        if (updateMs != Config.GetInt("update_ms"))
                        {
                            updateMs = Config.GetInt("update_ms");
                            futureTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + updateMs;
                        }
                        else if (futureTime - currentTime > updateMs)
                            futureTime = currentTime;

                        // Poll for input and process any input detected
                        else if (Input.Poll(Math.Min(1000, (int)(futureTime - currentTime))))
                        {
                            if (!Runner.Active) Config.Unlock();

                            if (Menu.Active) Menu.Process(Input.Get());
                            else Input.Process(Input.Get());
                        }

                        // Break the loop at 1000ms intervals or if input polling was interrupted
                        else break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in main loop -> " + e.Message);
                CleanQuit(1);
            }
        }

        static void CleanQuit(int exitCode)
        {
            if (Global.Quitting) return;
            Global.Quitting = true;
            Runner.Stop();

            Config.Write();

            if (Term.Initialized)
            {
                Term.Restore();
            }

            if (!string.IsNullOrEmpty(Global.ExitErrorMsg))
            {
                exitCode = 1;
                Console.WriteLine("ERROR: " + Global.ExitErrorMsg);
            }

            Console.WriteLine("Quitting! Runtime: " + Tools.SecToDhms(Tools.TimeS() - Global.StartTime));

            Environment.Exit(exitCode);
        }

        static void TermResize(bool force)
        {
            if (Input.Polling)
            {
                Global.Resized = true;
                Input.Interrupt = true;
                return;
            }

            if (!Term.Refresh(true) && !force) return;

            Global.Resized = true;
            if (Runner.Active) Runner.Stop();
            Term.Refresh();
            Config.Unlock();

            var boxes = Config.Get("shown_boxes");
            var minSize = Term.GetMinSize(boxes);

            while (!force || (Term.Width < minSize[0] || Term.Height < minSize[1]))
            {
                Thread.Sleep(100);
                if (Term.Width < minSize[0] || Term.Height < minSize[1])
                {
                    Console.WriteLine(Term.Clear + Global.BgBlack + Global.FgWhite + Mv.To((Term.Height / 2) - 2, (Term.Width / 2) - 11)
                        + "Terminal size too small:" + Mv.To((Term.Height / 2) - 1, (Term.Width / 2) - 10)
                        + " Width = " + (Term.Width < minSize[1] ? Global.FgRed : Global.FgGreen) + Term.Width
                        + Global.FgWhite + " Height = " + (Term.Height < minSize[0] ? Global.FgRed : Global.FgGreen) + Term.Height
                        + Mv.To((Term.Height / 2) + 1, (Term.Width / 2) - 12) + Global.FgWhite
                        + "Needed for current config:" + Mv.To((Term.Height / 2) + 2, (Term.Width / 2) - 10)
                        + "Width = " + minSize[0] + " Height = " + minSize[1]);
                    bool gotKey = false;
                    for (; !Term.Refresh() && !gotKey; gotKey = Input.Poll(10)) ;
                    if (gotKey)
                    {
                        var key = Input.Get();
                        if (key == "q")
                            CleanQuit(0);
                        else if (new[] { "1", "2", "3", "4" }.Contains(key))
                        {
                            Config.CurrentPreset = -1;
                            Config.ToggleBox(new[] { "cpu", "mem", "net", "proc" }[int.Parse(key) - 1]);
                            boxes = Config.Get("shown_boxes");
                        }
                    }
                    minSize = Term.GetMinSize(boxes);
                }
                else if (!Term.Refresh()) break;
            }

            Input.Interrupt = true;
        }
    }
}
