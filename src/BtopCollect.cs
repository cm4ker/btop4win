using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;

namespace btop4win
{
    public static class BtopCollect
    {
        private static bool locked = false;
        private static bool writelock = false;
        private static bool write_new;

        private static readonly List<string[]> descriptions = new List<string[]>
        {
            new string[] { "color_theme", "#* Name of a btop++/bpytop/bashtop formatted \".theme\" file, \"Default\" and \"TTY\" for builtin themes.\n#* Themes should be placed in \"themes\" folder in same folder as btop4win.exe" },
            new string[] { "theme_background", "#* If the theme set background should be shown, set to False if you want terminal background transparency." },
            new string[] { "truecolor", "#* Sets if 24-bit truecolor should be used, will convert 24-bit colors to 256 color (6x6x6 color cube) if false." },
            new string[] { "force_tty", "#* Set to true to force tty mode regardless if a real tty has been detected or not.\n#* Will force 16-color mode and TTY theme, set all graph symbols to \"tty\" and swap out other non tty friendly symbols." },
            new string[] { "presets", "#* Define presets for the layout of the boxes. Preset 0 is always all boxes shown with default settings. Max 9 presets.\n#* Format: \"box_name:P:G,box_name:P:G\" P=(0 or 1) for alternate positions, G=graph symbol to use for box.\n#* Use withespace \" \" as separator between different presets.\n#* Example: \"cpu:0:default,mem:0:tty,proc:1:default cpu:0:braille,proc:0:tty\"" },
            new string[] { "vim_keys", "#* Set to True to enable \"h,j,k,l,g,G\" keys for directional control in lists.\n#* Conflicting keys for h:\"help\" and k:\"kill\" is accessible while holding shift." },
            new string[] { "enable_ohmr", "#* Enables monitoring of CPU temps, accurate CPU clock and GPU via Libre Hardware Monitor.\n#* Needs the my DLL's from (https://github.com/aristocratos/LHM-CppExport) installed in same folder as btop4win.exe." },
            new string[] { "show_gpu", "#* Also show gpu stats in cpu and mem box. Needs Libre Hardware Monitor Report enabled." },
            new string[] { "selected_gpu", "#* Which GPU to display if multiple is detected." },
            new string[] { "gpu_mem_override", "#* Manually set and override the GPU total memory shown when not correctly detected. Value in MiB. Example: \"gpu_mem_override = 1024\" for 1 GiB." },
            new string[] { "rounded_corners", "#* Rounded corners on boxes, is ignored if TTY mode is ON." },
            new string[] { "graph_symbol", "#* Default symbols to use for graph creation, \"braille\", \"block\" or \"tty\".\n#* \"braille\" offers the highest resolution but might not be included in all fonts.\n#* \"block\" has half the resolution of braille but uses more common characters.\n#* \"tty\" uses only 3 different symbols but will work with most fonts and should work in a real TTY.\n#* Note that \"tty\" only has half the horizontal resolution of the other two, so will show a shorter historical view." },
            new string[] { "graph_symbol_cpu", "# Graph symbol to use for graphs in cpu box, \"default\", \"braille\", \"block\" or \"tty\"." },
            new string[] { "graph_symbol_mem", "# Graph symbol to use for graphs in cpu box, \"default\", \"braille\", \"block\" or \"tty\"." },
            new string[] { "graph_symbol_net", "# Graph symbol to use for graphs in cpu box, \"default\", \"braille\", \"block\" or \"tty\"." },
            new string[] { "graph_symbol_proc", "# Graph symbol to use for graphs in cpu box, \"default\", \"braille\", \"block\" or \"tty\"." },
            new string[] { "shown_boxes", "#* Manually set which boxes to show. Available values are \"cpu mem net proc\", separate values with whitespace." },
            new string[] { "update_ms", "#* Update time in milliseconds, recommended 2000 ms or above for better sample times for graphs." },
            new string[] { "proc_sorting", "#* Processes sorting, \"pid\" \"program\" \"arguments\" \"threads\" \"user\" \"memory\" \"cpu lazy\" \"cpu direct\",\n#* \"cpu lazy\" sorts top process over time (easier to follow), \"cpu direct\" updates top process directly." },
            new string[] { "proc_services", "#* Show services in the process box instead of processes." },
            new string[] { "services_sorting", "#* Services sorting, \"service\" \"caption\" \"status\" \"memory\" \"cpu lazy\" \"cpu direct\",\n#* \"cpu lazy\" sorts top service over time (easier to follow), \"cpu direct\" updates top service directly." },
            new string[] { "proc_reversed", "#* Reverse sorting order, True or False." },
            new string[] { "proc_tree", "#* Show processes as a tree." },
            new string[] { "proc_colors", "#* Use the cpu graph colors in the process list." },
            new string[] { "proc_gradient", "#* Use a darkening gradient in the process list." },
            new string[] { "proc_per_core", "#* If process cpu usage should be of the core it's running on or usage of the total available cpu power." },
            new string[] { "proc_mem_bytes", "#* Show process memory as bytes instead of percent." },
            new string[] { "proc_left", "#* Show proc box on left side of screen instead of right." },
            new string[] { "cpu_graph_upper", "#* Sets the CPU stat shown in upper half of the CPU graph, \"total\" is always available.\n#* Select from a list of detected attributes from the options menu." },
            new string[] { "cpu_graph_lower", "#* Sets the CPU stat shown in lower half of the CPU graph, \"total\" is always available.\n#* Select from a list of detected attributes from the options menu." },
            new string[] { "cpu_invert_lower", "#* Toggles if the lower CPU graph should be inverted." },
            new string[] { "cpu_single_graph", "#* Set to True to completely disable the lower CPU graph." },
            new string[] { "cpu_bottom", "#* Show cpu box at bottom of screen instead of top." },
            new string[] { "show_uptime", "#* Shows the system uptime in the CPU box." },
            new string[] { "check_temp", "#* Show cpu temperature." },
            new string[] { "cpu_sensor", "#* Which sensor to use for cpu temperature, use options menu to select from list of available sensors." },
            new string[] { "show_coretemp", "#* Show temperatures for cpu cores also if check_temp is True and sensors has been found." },
            new string[] { "temp_scale", "#* Which temperature scale to use, available values: \"celsius\", \"fahrenheit\", \"kelvin\" and \"rankine\"." },
            new string[] { "base_10_sizes", "#* Use base 10 for bits/bytes sizes, KB = 1000 instead of KiB = 1024." },
            new string[] { "clock_format", "#* Draw a clock at top of screen, formatting according to strftime, empty string to disable.\n#* Special formatting: /host = hostname | /user = username | /uptime = system uptime" },
            new string[] { "background_update", "#* Update main ui in background when menus are showing, set this to false if the menus is flickering too much for comfort." },
            new string[] { "custom_cpu_name", "#* Custom cpu model name, empty string to disable." },
            new string[] { "disks_filter", "#* Optional filter for shown disks, should be full path of a mountpoint, separate multiple values with whitespace \" \".\n#* Begin line with \"exclude=\" to change to exclude filter, otherwise defaults to \"most include\" filter. Example: disks_filter=\"exclude=D:\\ E:\\\"." },
            new string[] { "mem_graphs", "#* Show graphs instead of meters for memory values." },
            new string[] { "mem_below_net", "#* Show mem box below net box instead of above." },
            new string[] { "show_page", "#* If page memory should be shown in memory box." },
            new string[] { "show_disks", "#* If mem box should be split to also show disks info." },
            new string[] { "only_physical", "#* Filter out non physical disks. Set this to False to include network disks, RAM disks and similar." },
            new string[] { "disk_free_priv", "#* Set to true to show available disk space for privileged users." },
            new string[] { "show_io_stat", "#* Toggles if io activity % (disk busy time) should be shown in regular disk usage view." },
            new string[] { "io_mode", "#* Toggles io mode for disks, showing big graphs for disk read/write speeds." },
            new string[] { "io_graph_combined", "#* Set to True to show combined read/write io graphs in io mode." },
            new string[] { "io_graph_speeds", "#* Set the top speed for the io graphs in MiB/s (100 by default), use format \"device:\\speed\" separate disks with whitespace \" \".\n#* Example: \"C:\\100 D:\\20 G:\\1\"." },
            new string[] { "net_download", "#* Set fixed values for network graphs in Mebibits. Is only used if net_auto is also set to False." },
            new string[] { "net_upload", "" },
            new string[] { "net_auto", "#* Use network graphs auto rescaling mode, ignores any values set above and rescales down to 10 Kibibytes at the lowest." },
            new string[] { "net_sync", "#* Sync the auto scaling for download and upload to whichever currently has the highest scale." },
            new string[] { "net_iface", "#* Starts with the Network Interface specified here." },
            new string[] { "show_battery", "#* Show battery stats in top right if battery is present." },
            new string[] { "log_level", "#* Set loglevel for \"~/.config/btop/btop.log\" levels are: \"ERROR\" \"WARNING\" \"INFO\" \"DEBUG\".\n#* The level set includes all lower levels, i.e. \"DEBUG\" will show all logging info." }
        };

        private static readonly Dictionary<string, string> strings = new Dictionary<string, string>
        {
            { "color_theme", "Default" },
            { "shown_boxes", "cpu mem net proc" },
            { "graph_symbol", "tty" },
            { "presets", "cpu:1:default,proc:0:default cpu:0:default,mem:0:default,net:0:default cpu:0:block,net:0:tty" },
            { "graph_symbol_cpu", "default" },
            { "graph_symbol_mem", "default" },
            { "graph_symbol_net", "default" },
            { "graph_symbol_proc", "default" },
            { "proc_sorting", "cpu lazy" },
            { "services_sorting", "cpu lazy" },
            { "cpu_graph_upper", "total" },
            { "cpu_graph_lower", "gpu" },
            { "selected_gpu", "Auto" },
            { "temp_scale", "celsius" },
            { "clock_format", "%X" },
            { "custom_cpu_name", "" },
            { "disks_filter", "" },
            { "io_graph_speeds", "" },
            { "net_iface", "" },
            { "log_level", "WARNING" },
            { "proc_filter", "" },
            { "proc_command", "" },
            { "selected_name", "" },
            { "selected_status", "" },
            { "detailed_name", "" }
        };

        private static readonly Dictionary<string, bool> bools = new Dictionary<string, bool>
        {
            { "theme_background", true },
            { "truecolor", true },
            { "rounded_corners", false },
            { "proc_services", false },
            { "proc_reversed", false },
            { "proc_tree", false },
            { "proc_colors", true },
            { "proc_gradient", true },
            { "proc_per_core", false },
            { "proc_mem_bytes", true },
            { "proc_left", false },
            { "cpu_invert_lower", true },
            { "cpu_single_graph", false },
            { "cpu_bottom", false },
            { "show_uptime", true },
            { "check_temp", true },
            { "enable_ohmr", true },
            { "show_gpu", true },
            { "show_coretemp", true },
            { "background_update", true },
            { "mem_graphs", true },
            { "mem_below_net", false },
            { "show_page", true },
            { "show_disks", true },
            { "only_physical", true },
            { "show_io_stat", true },
            { "io_mode", false },
            { "base_10_sizes", false },
            { "io_graph_combined", false },
            { "net_auto", true },
            { "net_sync", false },
            { "show_battery", true },
            { "vim_keys", false },
            { "tty_mode", false },
            { "disk_free_priv", false },
            { "force_tty", false },
            { "lowcolor", false },
            { "show_detailed", false },
            { "proc_filtering", false }
        };

        private static readonly Dictionary<string, int> ints = new Dictionary<string, int>
        {
            { "update_ms", 1500 },
            { "net_download", 100 },
            { "net_upload", 100 },
            { "detailed_pid", 0 },
            { "selected_pid", 0 },
            { "selected_depth", 0 },
            { "proc_start", 0 },
            { "proc_selected", 0 },
            { "proc_last_selected", 0 },
            { "gpu_mem_override", 0 }
        };

        private static bool _locked(string name)
        {
            if (!write_new && descriptions.Any(a => a[0] == name))
                write_new = true;
            return locked;
        }

        public static void Lock()
        {
            while (writelock) Thread.Sleep(10);
            locked = true;
        }

        public static void Unlock()
        {
            if (!locked) return;
            while (Runner.Active) Thread.Sleep(10);
            writelock = true;
            try
            {
                if (Proc.Shown)
                {
                    ints["selected_pid"] = Proc.SelectedPid;
                    strings["selected_name"] = Proc.SelectedName;
                    strings["selected_status"] = Proc.SelectedStatus;
                    ints["proc_start"] = Proc.Start;
                    ints["proc_selected"] = Proc.Selected;
                    ints["selected_depth"] = Proc.SelectedDepth;
                }

                locked = false;
            }
            catch (Exception e)
            {
                Global.ExitErrorMsg = "Exception during Config::Unlock() : " + e.Message;
                Environment.Exit(1);
            }
            writelock = false;
        }

        public static void Load(string confFile, List<string> loadWarnings)
        {
            if (string.IsNullOrEmpty(confFile))
                return;
            else if (!File.Exists(confFile))
            {
                write_new = true;
                return;
            }

            using (var reader = new StreamReader(confFile))
            {
                if (reader.Peek() != '#' || !reader.ReadLine().Contains(Global.Version))
                    write_new = true;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();
                    if (line.StartsWith("#"))
                        continue;

                    var parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;

                    var name = parts[0].Trim();
                    var value = parts[1].Trim().Trim('"');

                    if (bools.ContainsKey(name))
                    {
                        if (bool.TryParse(value, out var boolValue))
                            bools[name] = boolValue;
                        else
                            loadWarnings.Add("Got an invalid bool value for config name: " + name);
                    }
                    else if (ints.ContainsKey(name))
                    {
                        if (int.TryParse(value, out var intValue))
                            ints[name] = intValue;
                        else
                            loadWarnings.Add("Got an invalid integer value for config name: " + name);
                    }
                    else if (strings.ContainsKey(name))
                    {
                        strings[name] = value;
                    }
                }
            }
        }

        public static void Write()
        {
            if (string.IsNullOrEmpty(Config.ConfFile) || !write_new) return;

            using (var writer = new StreamWriter(Config.ConfFile, false))
            {
                writer.WriteLine("#? Config file for btop4win v. " + Global.Version);
                foreach (var desc in descriptions)
                {
                    writer.WriteLine();
                    if (!string.IsNullOrEmpty(desc[1]))
                        writer.WriteLine(desc[1]);
                    writer.Write(desc[0] + " = ");
                    if (strings.ContainsKey(desc[0]))
                        writer.WriteLine("\"" + strings[desc[0]] + "\"");
                    else if (ints.ContainsKey(desc[0]))
                        writer.WriteLine(ints[desc[0]]);
                    else if (bools.ContainsKey(desc[0]))
                        writer.WriteLine(bools[desc[0]] ? "True" : "False");
                }
            }
        }

        public static void SetWinDebug()
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr thisProc = GetCurrentProcess();

            if (!OpenProcessToken(thisProc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
            {
                if (Marshal.GetLastWin32Error() == ERROR_NO_TOKEN)
                {
                    if (!ImpersonateSelf(SecurityImpersonation))
                        throw new InvalidOperationException("SetWinDebug() -> ImpersonateSelf() failed with ID: " + Marshal.GetLastWin32Error());
                    if (!OpenProcessToken(thisProc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                        throw new InvalidOperationException("SetWinDebug() -> OpenProcessToken() failed with ID: " + Marshal.GetLastWin32Error());
                }
                else
                    throw new InvalidOperationException("SetWinDebug() -> OpenProcessToken() failed with ID: " + Marshal.GetLastWin32Error());
            }

            TOKEN_PRIVILEGES tpriv = new TOKEN_PRIVILEGES();
            TOKEN_PRIVILEGES old_tpriv = new TOKEN_PRIVILEGES();
            LUID luid;
            uint tprivSize = (uint)Marshal.SizeOf(typeof(TOKEN_PRIVILEGES));

            if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out luid))
                throw new InvalidOperationException("SetWinDebug() -> LookupPrivilegeValue() failed with ID: " + Marshal.GetLastWin32Error());

            tpriv.PrivilegeCount = 1;
            tpriv.Privileges = new LUID_AND_ATTRIBUTES[1];
            tpriv.Privileges[0].Luid = luid;
            tpriv.Privileges[0].Attributes = 0;

            if (!AdjustTokenPrivileges(hToken, false, ref tpriv, tprivSize, ref old_tpriv, out tprivSize))
                throw new InvalidOperationException("SetWinDebug() -> AdjustTokenPrivileges() [get] failed with ID: " + Marshal.GetLastWin32Error());

            old_tpriv.PrivilegeCount = 1;
            old_tpriv.Privileges[0].Luid = luid;
            old_tpriv.Privileges[0].Attributes |= SE_PRIVILEGE_ENABLED;

            if (!AdjustTokenPrivileges(hToken, false, ref old_tpriv, tprivSize, IntPtr.Zero, IntPtr.Zero))
                throw new InvalidOperationException("SetWinDebug() -> AdjustTokenPrivileges() [set] failed with ID: " + Marshal.GetLastWin32Error());

            RevertToSelf();
        }

        public static string Bstr2Str(IntPtr source)
        {
            if (source == IntPtr.Zero) return "";
            return Marshal.PtrToStringBSTR(source);
        }

        public static void WMIInit()
        {
            IntPtr wbemLocator = IntPtr.Zero;
            IntPtr wbemServices = IntPtr.Zero;

            if (CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED) != 0)
                throw new InvalidOperationException("WMIInit() -> CoInitializeEx() failed");

            if (CoInitializeSecurity(IntPtr.Zero, -1, IntPtr.Zero, IntPtr.Zero, RPC_C_AUTHN_LEVEL_DEFAULT, RPC_C_IMP_LEVEL_IMPERSONATE, IntPtr.Zero, EOAC_NONE, IntPtr.Zero) != 0)
                throw new InvalidOperationException("WMIInit() -> CoInitializeSecurity() failed");

            if (CoCreateInstance(ref CLSID_WbemLocator, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref IID_IWbemLocator, out wbemLocator) != 0)
                throw new InvalidOperationException("WMIInit() -> CoCreateInstance() failed");

            if (Marshal.QueryInterface(wbemLocator, ref IID_IWbemServices, out wbemServices) != 0)
                throw new InvalidOperationException("WMIInit() -> QueryInterface() failed");

            if (Marshal.QueryInterface(wbemServices, ref IID_IWbemServices, out wbemServices) != 0)
                throw new InvalidOperationException("WMIInit() -> QueryInterface() failed");

            if (CoSetProxyBlanket(wbemServices, RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, IntPtr.Zero, RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, IntPtr.Zero, EOAC_NONE) != 0)
                throw new InvalidOperationException("WMIInit() -> CoSetProxyBlanket() failed");
        }

        public static void OHMRCollect()
        {
            // Implement OHMRCollect logic here
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, ref TOKEN_PRIVILEGES PreviousState, out uint ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ImpersonateSelf(int ImpersonationLevel);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool RevertToSelf();

        [DllImport("ole32.dll", SetLastError = true)]
        private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

        [DllImport("ole32.dll", SetLastError = true)]
        private static extern int CoInitializeSecurity(IntPtr pSecDesc, int cAuthSvc, IntPtr asAuthSvc, IntPtr pReserved1, uint dwAuthnLevel, uint dwImpLevel, IntPtr pAuthList, uint dwCapabilities, IntPtr pReserved3);

        [DllImport("ole32.dll", SetLastError = true)]
        private static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, out IntPtr ppv);

        [DllImport("ole32.dll", SetLastError = true)]
        private static extern int CoSetProxyBlanket(IntPtr pProxy, uint dwAuthnSvc, uint dwAuthzSvc, IntPtr pServerPrincName, uint dwAuthnLevel, uint dwImpLevel, IntPtr pAuthInfo, uint dwCapabilities);

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const int ERROR_NO_TOKEN = 1008;
        private const int SecurityImpersonation = 2;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";
        private const uint COINIT_MULTITHREADED = 0x0;
        private const uint RPC_C_AUTHN_LEVEL_DEFAULT = 0;
        private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
        private const uint EOAC_NONE = 0;
        private const uint RPC_C_AUTHN_WINNT = 10;
        private const uint RPC_C_AUTHZ_NONE = 0;
        private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
        private static readonly Guid CLSID_WbemLocator = new Guid("4590F811-1D3A-11D0-891F-00AA004B2E24");
        private static readonly Guid IID_IWbemLocator = new Guid("DC12A687-737F-11CF-884D-00AA004B2E24");
        private static readonly Guid IID_IWbemServices = new Guid("9556DC99-828C-11CF-A37E-00AA003240C7");

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }
    }
}
