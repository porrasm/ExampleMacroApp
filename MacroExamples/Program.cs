using MacroFramework;
using MacroFramework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MacroExamples {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            Macros.Start(GetMySetup());
        }

        private static MacroSetup GetMySetup() {
            MacroSetup setup = MacroSetup.GetDefaultSetup();

            setup.CommandAssembly = Assembly.GetExecutingAssembly();

            // configure these, now LCtrl is blocked since its reserved for the GeneralBindKey. I recommend CapsLock, no one needs that key anyway.
            setup.Settings.GeneralBindKey = KKey.LCtrl;
            setup.Settings.CommandKey = KKey.None;

            setup.Settings.MainLoopTimestep = 15;

            setup.Settings.AllowKeyboardHook = true;
            setup.Settings.AllowMouseHook = true;

            return setup;
        }
    }
}
