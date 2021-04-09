using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUtilities;

namespace MacroExamples {
    public static class WindowTools {

        public static Animation SmoothMove(Window window, Area end) {
            Animation animation = new Animation(Curves.FExpo, 500);
            animation.Start(window, end);
            return animation;
        }

        public static bool IsInvalidWindow(Window win) {
            return win == null || !win.IsValid || !win.IsTopLevel || WinGroup.Desktop.Match(win);
        }
        public static bool GetValidWindow(out Window window) {
            window = Window.FromMouse;
            return !IsInvalidWindow(window);
        }

        public delegate void WindowIterator(Window win);
        public static void ForEachValidWindow(WindowIterator iterateFunc) {
            foreach (Window win in Window.GetWindows(w => !IsInvalidWindow(w))) {
                if (!IsInvalidWindow(win)) {
                    iterateFunc(win);
                }
            }
        }
    }
}
