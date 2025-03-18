using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Concurrent;

namespace BtopMenu
{
    public static class Menu
    {
        public static bool Active { get; set; } = false;
        public static string Output { get; set; } = string.Empty;
        public static int SignalToSend { get; set; } = 0;
        public static bool Redraw { get; set; } = true;

        public static Dictionary<string, Input.MouseLoc> MouseMappings { get; set; } = new Dictionary<string, Input.MouseLoc>();

        public class MsgBox
        {
            private string boxContents;
            private string buttonLeft;
            private string buttonRight;
            private int height;
            private int width;
            private int boxType;
            private int selected;
            private int x;
            private int y;

            public enum BoxTypes { OK, YES_NO, NO_YES }
            public enum MsgReturn { Invalid, Ok_Yes, No_Esc, Select }

            public MsgBox() { }

            public MsgBox(int width, int boxType, List<string> content, string title)
            {
                this.width = width;
                this.boxType = boxType;
                var ttyMode = Config.GetBool("tty_mode");
                var rounded = Config.GetBool("rounded_corners");
                var rightUp = ttyMode || !rounded ? Symbols.RightUp : Symbols.RoundRightUp;
                var leftUp = ttyMode || !rounded ? Symbols.LeftUp : Symbols.RoundLeftUp;
                var rightDown = ttyMode || !rounded ? Symbols.RightDown : Symbols.RoundRightDown;
                var leftDown = ttyMode || !rounded ? Symbols.LeftDown : Symbols.RoundLeftDown;
                height = content.Count + 7;
                x = Term.Width / 2 - width / 2;
                y = Term.Height / 2 - height / 2;
                if (boxType == 2) selected = 1;

                buttonLeft = leftUp + new string(Symbols.HLine, 6) + Mv.Left(7) + Mv.Down(2) + leftDown + new string(Symbols.HLine, 6) + Mv.Left(7) + Mv.Up(1) + Symbols.VLine;
                buttonRight = Symbols.VLine + Mv.Left(7) + Mv.Up(1) + new string(Symbols.HLine, 6) + rightUp + Mv.Left(7) + Mv.Down(2) + new string(Symbols.HLine, 6) + rightDown + Mv.Up(2);

                boxContents = Draw.CreateBox(x, y, width, height, Theme.GetColor("hi_fg"), true, title) + Mv.Down(1);
                foreach (var line in content)
                {
                    boxContents += Mv.Save + Mv.Right(Math.Max(0, (width / 2) - (Fx.Uncolor(line).Length / 2) - 1)) + line + Mv.Restore + Mv.Down(1);
                }
            }

            public string Draw()
            {
                string output;
                int pos = width / 2 - (boxType == 0 ? 6 : 14);
                var firstColor = selected == 0 ? Theme.GetColor("hi_fg") : Theme.GetColor("div_line");
                output = Mv.Down(1) + Mv.Right(pos) + Fx.Bold + firstColor + buttonLeft + (selected == 0 ? Theme.GetColor("title") : Theme.GetColor("main_fg") + Fx.Unbold)
                    + (boxType == 0 ? "    Ok    " : "    Yes    ") + firstColor + buttonRight;
                MouseMappings["button1"] = new Input.MouseLoc { Line = y + height - 4, Col = x + pos + 1, Height = 3, Width = 12 + (boxType > 0 ? 1 : 0) };
                if (boxType > 0)
                {
                    var secondColor = selected == 1 ? Theme.GetColor("hi_fg") : Theme.GetColor("div_line");
                    output += Mv.Right(2) + secondColor + buttonLeft + (selected == 1 ? Theme.GetColor("title") : Theme.GetColor("main_fg") + Fx.Unbold)
                        + "    No    " + secondColor + buttonRight;
                    MouseMappings["button2"] = new Input.MouseLoc { Line = y + height - 4, Col = x + pos + 15 + (boxType > 0 ? 1 : 0), Height = 3, Width = 12 };
                }
                return boxContents + output + Fx.Reset;
            }

            public int Input(string key)
            {
                if (string.IsNullOrEmpty(key)) return (int)MsgReturn.Invalid;

                if (key == "escape" || key == "backspace" || key == "q" || key == "button2")
                {
                    return (int)MsgReturn.No_Esc;
                }
                else if (key == "button1" || (boxType == 0 && key.ToUpper() == "O"))
                {
                    return (int)MsgReturn.Ok_Yes;
                }
                else if (key == "enter" || key == "space")
                {
                    return selected + 1;
                }
                else if (boxType == 0)
                {
                    return (int)MsgReturn.Invalid;
                }
                else if (key.ToUpper() == "Y")
                {
                    return (int)MsgReturn.Ok_Yes;
                }
                else if (key.ToUpper() == "N")
                {
                    return (int)MsgReturn.No_Esc;
                }
                else if (key == "right" || key == "tab")
                {
                    if (++selected > 1) selected = 0;
                    return (int)MsgReturn.Select;
                }
                else if (key == "left" || key == "shift_tab")
                {
                    if (--selected < 0) selected = 1;
                    return (int)MsgReturn.Select;
                }

                return (int)MsgReturn.Invalid;
            }

            public void Clear()
            {
                boxContents = string.Empty;
                buttonLeft = string.Empty;
                buttonRight = string.Empty;
                MouseMappings.Remove("button1");
                MouseMappings.Remove("button2");
            }
        }

        public enum Menus
        {
            SizeError,
            SignalConfig,
            SignalSend,
            SignalPause,
            SignalReturn,
            Options,
            Help,
            Main
        }

        public static BitArray MenuMask { get; set; } = new BitArray(8);

        public static void Process(string key = "")
        {
            if (MenuMask.Cast<bool>().All(b => !b))
            {
                Active = false;
                Global.Overlay = string.Empty;
                Redraw = false;
                Output = string.Empty;
                MouseMappings.Clear();
                return;
            }

            if (Redraw)
            {
                Redraw = false;
                // Call the appropriate function based on the current menu
                // This is a placeholder, replace with actual function calls
                switch (CurrentMenu)
                {
                    case Menus.SizeError:
                        SizeError(key);
                        break;
                    case Menus.SignalConfig:
                        SignalConfig(key);
                        break;
                    case Menus.SignalSend:
                        SignalSend(key);
                        break;
                    case Menus.SignalPause:
                        SignalPause(key);
                        break;
                    case Menus.SignalReturn:
                        SignalReturn(key);
                        break;
                    case Menus.Options:
                        OptionsMenu(key);
                        break;
                    case Menus.Help:
                        HelpMenu(key);
                        break;
                    case Menus.Main:
                        MainMenu(key);
                        break;
                }
            }
        }

        public static void Show(int menu)
        {
            MenuMask.Set(menu, true);
            Process();
        }

        private static void SizeError(string key)
        {
            // Implement the logic for SizeError menu
        }

        private static void SignalConfig(string key)
        {
            // Implement the logic for SignalConfig menu
        }

        private static void SignalSend(string key)
        {
            // Implement the logic for SignalSend menu
        }

        private static void SignalPause(string key)
        {
            // Implement the logic for SignalPause menu
        }

        private static void SignalReturn(string key)
        {
            // Implement the logic for SignalReturn menu
        }

        private static void OptionsMenu(string key)
        {
            // Implement the logic for OptionsMenu
        }

        private static void HelpMenu(string key)
        {
            // Implement the logic for HelpMenu
        }

        private static void MainMenu(string key)
        {
            // Implement the logic for MainMenu
        }
    }
}
