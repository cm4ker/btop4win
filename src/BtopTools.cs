using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace btop4win
{
    public static class Term
    {
        public static bool Initialized { get; private set; } = false;
        public static int Width { get; private set; } = 0;
        public static int Height { get; private set; } = 0;
        public static string CurrentTty { get; private set; } = string.Empty;
        private static uint outSavedMode;
        private static uint inSavedMode;

        public static bool Refresh(bool onlyCheck)
        {
            var csbi = new CONSOLE_SCREEN_BUFFER_INFO();
            if (!GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), out csbi))
                return false;

            if (Width != csbi.srWindow.Right - csbi.srWindow.Left + 1 || Height != csbi.srWindow.Bottom - csbi.srWindow.Top + 1)
            {
                if (!onlyCheck)
                {
                    Width = csbi.srWindow.Right - csbi.srWindow.Left + 1;
                    Height = csbi.srWindow.Bottom - csbi.srWindow.Top + 1;
                }
                return true;
            }
            return false;
        }

        public static int[] GetMinSize(string boxes)
        {
            bool cpu = boxes.Contains("cpu");
            bool mem = boxes.Contains("mem");
            bool net = boxes.Contains("net");
            bool proc = boxes.Contains("proc");
            int width = 0;
            if (mem) width = Mem.MinWidth;
            else if (net) width = Mem.MinWidth;
            width += (proc ? Proc.MinWidth : 0);
            if (cpu && width < Cpu.MinWidth) width = Cpu.MinWidth;

            int height = (cpu ? Cpu.MinHeight : 0);
            if (proc) height += Proc.MinHeight;
            else height += (mem ? Mem.MinHeight : 0) + (net ? Net.MinHeight : 0);

            return new int[] { width, height };
        }

        public static void SetModes()
        {
            var handleOut = GetStdHandle(STD_OUTPUT_HANDLE);
            var handleIn = GetStdHandle(STD_INPUT_HANDLE);

            uint outConsoleMode = outSavedMode;
            outConsoleMode |= (ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN);
            SetConsoleMode(handleOut, outConsoleMode);
            SetConsoleOutputCP(65001);

            uint inConsoleMode = 0;
            inConsoleMode = ENABLE_WINDOW_INPUT | ENABLE_MOUSE_INPUT | ENABLE_INSERT_MODE | ENABLE_EXTENDED_FLAGS;
            inConsoleMode &= ~ENABLE_ECHO_INPUT;
            SetConsoleMode(handleIn, inConsoleMode);
        }

        public static bool Init()
        {
            if (!Initialized)
            {
                var handleOut = GetStdHandle(STD_OUTPUT_HANDLE);
                var handleIn = GetStdHandle(STD_INPUT_HANDLE);
                Initialized = GetConsoleMode(handleOut, out outSavedMode) && GetConsoleMode(handleIn, out inSavedMode);

                if (Initialized)
                {
                    SetModes();

                    Console.SetIn(new StreamReader(Console.OpenStandardInput(), Console.InputEncoding, false, 1024, false));
                    Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding, 1024, false) { AutoFlush = true });

                    Refresh();

                    Console.Write(AltScreen + HideCursor);
                    Global.Resized = false;
                }
            }
            return Initialized;
        }

        public static void Restore()
        {
            if (Initialized)
            {
                var handleOut = GetStdHandle(STD_OUTPUT_HANDLE);
                var handleIn = GetStdHandle(STD_INPUT_HANDLE);

                Console.Write(Clear + Fx.Reset + NormalScreen + ShowCursor);

                SetConsoleMode(handleOut, outSavedMode);
                SetConsoleMode(handleIn, inSavedMode);

                Initialized = false;
            }
        }

        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        private const uint ENABLE_WINDOW_INPUT = 0x0008;
        private const uint ENABLE_MOUSE_INPUT = 0x0010;
        private const uint ENABLE_INSERT_MODE = 0x0020;
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        private const uint ENABLE_ECHO_INPUT = 0x0004;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }
    }

    public static class Tools
    {
        public static int ServiceCommand(string name, ServiceCommands command)
        {
            var scManager = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scManager == IntPtr.Zero)
            {
                Logger.Error("Tools::ServiceCommand(): OpenSCManager() failed with error code: " + Marshal.GetLastWin32Error());
                return -1;
            }

            var scItem = OpenService(scManager, name, SERVICE_ALL_ACCESS);
            if (scItem == IntPtr.Zero)
            {
                Logger.Error("Tools::ServiceCommand(): OpenService() failed with error code: " + Marshal.GetLastWin32Error());
                CloseServiceHandle(scManager);
                return -1;
            }

            var itemStat = new SERVICE_STATUS_PROCESS();
            uint bytesNeeded;
            if (!QueryServiceStatusEx(scItem, SC_STATUS_PROCESS_INFO, ref itemStat, (uint)Marshal.SizeOf(itemStat), out bytesNeeded))
            {
                Logger.Error("Tools::ServiceCommand(): QueryServiceStatusEx() failed with error code: " + Marshal.GetLastWin32Error());
                CloseServiceHandle(scItem);
                CloseServiceHandle(scManager);
                return -1;
            }

            uint desiredState = 0;
            uint controlCommand = 0;

            switch (command)
            {
                case ServiceCommands.SCstart:
                    desiredState = SERVICE_RUNNING;
                    break;
                case ServiceCommands.SCstop:
                    desiredState = SERVICE_STOPPED;
                    controlCommand = SERVICE_CONTROL_STOP;
                    break;
                case ServiceCommands.SCcontinue:
                    desiredState = SERVICE_RUNNING;
                    controlCommand = SERVICE_CONTROL_CONTINUE;
                    break;
                case ServiceCommands.SCpause:
                    desiredState = SERVICE_PAUSED;
                    controlCommand = SERVICE_CONTROL_PAUSE;
                    break;
                case ServiceCommands.SCchange:
                    controlCommand = SERVICE_CONTROL_PARAMCHANGE;
                    break;
                default:
                    return -1;
            }

            if (desiredState != 0 && itemStat.dwCurrentState == desiredState)
            {
                CloseServiceHandle(scItem);
                CloseServiceHandle(scManager);
                return 0;
            }

            if (command == ServiceCommands.SCstart)
            {
                if (!StartService(scItem, 0, null))
                {
                    CloseServiceHandle(scItem);
                    CloseServiceHandle(scManager);
                    return Marshal.GetLastWin32Error();
                }
            }
            else
            {
                var scStat = new SERVICE_STATUS();
                if (!ControlService(scItem, controlCommand, ref scStat))
                {
                    CloseServiceHandle(scItem);
                    CloseServiceHandle(scManager);
                    return Marshal.GetLastWin32Error();
                }
            }

            CloseServiceHandle(scItem);
            CloseServiceHandle(scManager);
            return 0;
        }

        public static int ServiceSetStart(string name, int startType)
        {
            var scManager = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scManager == IntPtr.Zero)
            {
                Logger.Error("Tools::ServiceCommand(): OpenSCManager() failed with error code: " + Marshal.GetLastWin32Error());
                return -1;
            }

            var scItem = OpenService(scManager, name, SERVICE_ALL_ACCESS);
            if (scItem == IntPtr.Zero)
            {
                Logger.Error("Tools::ServiceCommand(): OpenService() failed with error code: " + Marshal.GetLastWin32Error());
                CloseServiceHandle(scManager);
                return -1;
            }

            if (!ChangeServiceConfig(scItem, SERVICE_NO_CHANGE, (uint)startType, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, null, null, null))
            {
                CloseServiceHandle(scItem);
                CloseServiceHandle(scManager);
                return Marshal.GetLastWin32Error();
            }

            CloseServiceHandle(scItem);
            CloseServiceHandle(scManager);
            return 0;
        }

        public static int WideUlen(string str)
        {
            int chars = 0;
            try
            {
                var wStr = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(str));
                foreach (var c in wStr)
                {
                    chars += Wcwidth(c);
                }
            }
            catch
            {
                return Ulen(str);
            }

            return chars;
        }

        public static int WideUlen(string wStr)
        {
            int chars = 0;
            foreach (var c in wStr)
            {
                chars += Wcwidth(c);
            }
            return chars;
        }

        public static string Uresize(string str, int len, bool wide)
        {
            if (len < 1 || string.IsNullOrEmpty(str)) return "";
            if (wide)
            {
                try
                {
                    var wStr = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(str));
                    while (WideUlen(wStr) > len)
                        wStr = wStr.Substring(0, wStr.Length - 1);
                    return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(wStr));
                }
                catch
                {
                    return Uresize(str, len, false);
                }
            }
            else
            {
                for (int x = 0, i = 0; i < str.Length; i++)
                {
                    if ((str[i] & 0xC0) != 0x80) x++;
                    if (x >= len + 1)
                    {
                        str = str.Substring(0, i);
                        break;
                    }
                }
            }
            return str;
        }

        public static string Luresize(string str, int len, bool wide)
        {
            if (len < 1 || string.IsNullOrEmpty(str)) return "";
            for (int x = 0, lastPos = 0, i = str.Length - 1; i > 0; i--)
            {
                if (wide && (str[i] & 0xC0) > 0xef)
                {
                    x += 2;
                    lastPos = Math.Max(0, i - 1);
                }
                else if ((str[i] & 0xC0) != 0x80)
                {
                    x++;
                    lastPos = i;
                }
                if (x >= len)
                {
                    str = str.Substring(lastPos);
                    break;
                }
            }
            return str;
        }

        public static string SReplace(string str, string from, string to)
        {
            return str.Replace(from, to);
        }

        public static string Ltrim(string str, string tStr)
        {
            return str.TrimStart(tStr.ToCharArray());
        }

        public static string Rtrim(string str, string tStr)
        {
            return str.TrimEnd(tStr.ToCharArray());
        }

        public static string Ltrim2(string str, string tStr)
        {
            return str.TrimStart(tStr.ToCharArray());
        }

        public static string Rtrim2(string str, string tStr)
        {
            return str.TrimEnd(tStr.ToCharArray());
        }

        public static List<string> Ssplit(string str, char delim)
        {
            return str.Split(delim).ToList();
        }

        public static string Ljust(string str, int x, bool utf, bool wide, bool limit)
        {
            if (utf)
            {
                if (limit && Ulen(str, wide) > x) return Uresize(str, x, wide);
                return str + new string(' ', Math.Max(x - Ulen(str), 0));
            }
            else
            {
                if (limit && str.Length > x) return str.Substring(0, x);
                return str + new string(' ', Math.Max(x - str.Length, 0));
            }
        }

        public static string Rjust(string str, int x, bool utf, bool wide, bool limit)
        {
            if (utf)
            {
                if (limit && Ulen(str, wide) > x) return Uresize(str, x, wide);
                return new string(' ', Math.Max(x - Ulen(str), 0)) + str;
            }
            else
            {
                if (limit && str.Length > x) return str.Substring(0, x);
                return new string(' ', Math.Max(x - str.Length, 0)) + str;
            }
        }

        public static string Cjust(string str, int x, bool utf, bool wide, bool limit)
        {
            if (utf)
            {
                if (limit && Ulen(str, wide) > x) return Uresize(str, x, wide);
                return new string(' ', (int)Math.Ceiling((double)(x - Ulen(str)) / 2)) + str + new string(' ', (int)Math.Floor((double)(x - Ulen(str)) / 2));
            }
            else
            {
                if (limit && str.Length > x) return str.Substring(0, x);
                return new string(' ', (int)Math.Ceiling((double)(x - str.Length) / 2)) + str + new string(' ', (int)Math.Floor((double)(x - str.Length) / 2));
            }
        }

        public static string Trans(string str)
        {
            var oldStr = str;
            var newStr = new StringBuilder();
            newStr.EnsureCapacity(str.Length);
            int pos;
            while ((pos = oldStr.IndexOf(' ')) != -1)
            {
                newStr.Append(oldStr.Substring(0, pos));
                int x = 0;
                while (pos + x < oldStr.Length && oldStr[pos + x] == ' ') x++;
                newStr.Append(Mv.R(x));
                oldStr = oldStr.Substring(pos + x);
            }
            return newStr.Length == 0 ? str : newStr.ToString() + oldStr;
        }

        public static string SecToDhms(int seconds, bool noDays, bool noSeconds)
        {
            int days = seconds / 86400; seconds %= 86400;
            int hours = seconds / 3600; seconds %= 3600;
            int minutes = seconds / 60; seconds %= 60;
            return (noDays || days == 0 ? "" : days + "d ") +
                   (hours < 10 ? "0" : "") + hours + ':' +
                   (minutes < 10 ? "0" : "") + minutes +
                   (noSeconds ? "" : ":" + (seconds < 10 ? "0" : "") + seconds);
        }

        public static string FloatingHumanizer(long value, bool shorten, int start, bool bit, bool perSecond)
        {
            string outStr;
            int mult = bit ? 8 : 1;
            bool mega = Config.GetBool("base_10_sizes");
            var mebiUnitsBit = new[] { "bit", "Kib", "Mib", "Gib", "Tib", "Pib", "Eib", "Zib", "Yib", "Bib", "GEb" };
            var mebiUnitsByte = new[] { "Byte", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB", "BiB", "GEB" };
            var megaUnitsBit = new[] { "bit", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb", "Zb", "Yb", "Bb", "Gb" };
            var megaUnitsByte = new[] { "Byte", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "GB" };
            var units = bit ? (mega ? megaUnitsBit : mebiUnitsBit) : (mega ? megaUnitsByte : mebiUnitsByte);

            value *= 100 * mult;

            if (mega)
            {
                while (value >= 100000)
                {
                    value /= 1000;
                    if (value < 100)
                    {
                        outStr = value.ToString();
                        break;
                    }
                    start++;
                }
            }
            else
            {
                while (value >= 102400)
                {
                    value >>= 10;
                    if (value < 100)
                    {
                        outStr = value.ToString();
                        break;
                    }
                    start++;
                }
            }
            if (string.IsNullOrEmpty(outStr))
            {
                outStr = value.ToString();
                if (!mega && outStr.Length == 4 && start > 0) { outStr = outStr.Substring(0, 2) + "." + outStr.Substring(2); }
                else if (outStr.Length == 3 && start > 0) outStr = outStr.Substring(0, 1) + "." + outStr.Substring(1);
                else if (outStr.Length >= 2) outStr = outStr.Substring(0, outStr.Length - 2);
            }
            if (shorten)
            {
                var fPos = outStr.IndexOf('.');
                if (fPos == 1 && outStr.Length > 3) outStr = Math.Round(float.Parse(outStr) * 10) / 10 + "";
                else if (fPos != -1) outStr = Math.Round(float.Parse(outStr)) + "";
                if (outStr.Length > 3) { outStr = (outStr[0] - '0' + 1) + ""; start++; }
                outStr += units[start][0];
            }
            else outStr += " " + units[start];

            if (perSecond) outStr += bit ? "ps" : "/s";
            return outStr;
        }

        public static string StrfTime(string strf)
        {
            return DateTime.Now.ToString(strf);
        }

        public static void AtomicWait(AtomicBoolean atom, bool old)
        {
            while (atom.Get() == old) ;
        }

        public static void AtomicWaitFor(AtomicBoolean atom, bool old, long waitMs)
        {
            var startTime = TimeMs();
            while (atom.Get() == old && (TimeMs() - startTime < waitMs)) Thread.Sleep(1);
        }

        public class AtomicLock : IDisposable
        {
            private AtomicBoolean atom;

            public AtomicLock(AtomicBoolean atom, bool wait)
            {
                this.atom = atom;
                if (wait)
                {
                    while (!this.atom.CompareAndSet(false, true)) ;
                }
                else
                {
                    this.atom.Set(true);
                }
            }

            public void Dispose()
            {
                this.atom.Set(false);
            }
        }

        public static string ReadFile(string path, string fallback)
        {
            if (!File.Exists(path)) return fallback;
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Logger.Error("ReadFile() : Exception when reading " + path + " : " + e.Message);
                return fallback;
            }
        }

        public static List<string> VReadFile(string path)
        {
            var outList = new List<string>();
            if (!File.Exists(path)) return outList;

            try
            {
                outList = File.ReadAllLines(path).ToList();
            }
            catch (Exception e)
            {
                Logger.Error("VReadFile() : Exception when reading " + path + " : " + e.Message);
            }
            return outList;
        }

        public static Tuple<long, string> CelsiusTo(long celsius, string scale)
        {
            switch (scale)
            {
                case "celsius":
                    return new Tuple<long, string>(celsius, "°C");
                case "fahrenheit":
                    return new Tuple<long, string>((long)Math.Round(celsius * 1.8 + 32), "°F");
                case "kelvin":
                    return new Tuple<long, string>((long)Math.Round(celsius + 273.15), "K ");
                case "rankine":
                    return new Tuple<long, string>((long)Math.Round(celsius * 1.8 + 491.67), "°R");
                default:
                    return new Tuple<long, string>(0, "");
            }
        }

        public static string Hostname()
        {
            return Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "unknown";
        }

        public static string Username()
        {
            return Environment.GetEnvironmentVariable("USERNAME") ?? "unknown";
        }

        public static bool ExecCmd(string cmd, out string ret)
        {
            ret = string.Empty;
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C " + cmd,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                ret = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Logger.Error("ExecCmd() : Exception when executing " + cmd + " : " + e.Message);
                return false;
            }
        }

        private static int Wcwidth(char c)
        {
            // Implement wcwidth logic here
            return 1;
        }

        private static int Ulen(string str, bool wide = false)
        {
            return wide ? WideUlen(str) : str.Count(c => (c & 0xC0) != 0x80);
        }

        private static long TimeMs()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }

    public enum ServiceCommands
    {
        SCstart,
        SCstop,
        SCcontinue,
        SCpause,
        SCchange
    }

    public static class Logger
    {
        private static bool busy = false;
        private static bool first = true;
        private static string tdf = "%Y/%m/%d (%T) | ";
        private static int loglevel;
        private static string logfile;

        public static void Set(string level)
        {
            loglevel = Array.IndexOf(logLevels, level);
        }

        public static void LogWrite(int level, string msg)
        {
            if (loglevel < level || string.IsNullOrEmpty(logfile)) return;
            lock (busy)
            {
                try
                {
                    if (File.Exists(logfile) && new FileInfo(logfile).Length > 1024 << 10)
                    {
                        var oldLog = logfile + ".1";
                        if (File.Exists(oldLog)) File.Delete(oldLog);
                        File.Move(logfile, oldLog);
                    }

                    using (var writer = new StreamWriter(logfile, true))
                    {
                        if (first)
                        {
                            first = false;
                            writer.WriteLine("\n" + Tools.StrfTime(tdf) + "===> btop++ v." + Global.Version);
                        }
                        writer.WriteLine(Tools.StrfTime(tdf) + logLevels[level] + ": " + msg);
                    }
                }
                catch (Exception e)
                {
                    logfile = null;
                    throw new Exception("Exception in Logger::LogWrite() : " + e.Message);
                }
            }
        }

        public static void Error(string msg)
        {
            LogWrite(1, msg);
        }

        public static void Warning(string msg)
        {
            LogWrite(2, msg);
        }

        public static void Info(string msg)
        {
            LogWrite(3, msg);
        }

        public static void Debug(string msg)
        {
            LogWrite(4, msg);
        }

        private static readonly string[] logLevels = { "DISABLED", "ERROR", "WARNING", "INFO", "DEBUG" };
    }
}
