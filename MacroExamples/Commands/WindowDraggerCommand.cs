using MacroFramework.Commands;
using MacroFramework.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUtilities;

namespace MacroExamples {

    /// <summary>
    /// Allows the user to drag the window without using the top border bar
    /// </summary>
    public class WindowDraggerCommand : Command {
        #region fields
        public static bool IsDraggging { get; private set; }
        private Window win;

        private const double OPACITY_FACTOR = 0.5;
        private const int OPACITY_FADE_TIME = 300;

        private bool windowWasMaximized;
        private bool windowWasAlwaysOnTop;
        private Coord mouseStart;
        private long dragStartTime;

        private Coord mousePos;
        private Coord mouseOffset;

        private int transparencyID;

        private LinkedList<Coord> coordHistory = new LinkedList<Coord>();
        #endregion

        protected override void InitializeActivators(ref ActivatorContainer acts) {
            acts.AddActivator(new BindHoldActivator(new Bind(BindSettings.OnPress, KKey.GeneralBindKey, KKey.MouseLeft), OnDragStart, OnDragUpdate, OnDragEnd));
        }

        #region drag
        private void OnDragStart() {
            win = Window.FromMouse;
            if (InvalidWindow) {
                return;
            }
            dragStartTime = Timer.Milliseconds;
            mouseStart = Mouse.Position;
            mousePos = mouseStart;
            windowWasMaximized = win.IsMaximized;
            windowWasAlwaysOnTop = win.IsAlwaysOnTop;


            Console.WriteLine("On Drag start");
        }
        private void OnDragUpdate() {
            if (InvalidWindow) {
                return;
            }
            mousePos = Mouse.Position;
            if (!IsDraggging) {
                if (CheckDragStart()) {
                    ActivateDrag();
                } else {
                    return;
                }
            }

            Drag();
        }

        private bool CheckDragStart() {
            if (mouseStart.Distance(mousePos) > 100) {
                Console.WriteLine("Mouse moved far");
                return true;
            }
            return Timer.PassedFrom(dragStartTime) > 500;
        }

        private void ActivateDrag() {
            WindowDockerCommands.PauseDock(win);
            if (windowWasMaximized) {
                win.Restore();
            }
            if (!windowWasAlwaysOnTop) {
                win.SetAlwaysOnTop(true);
            }

            coordHistory.Clear();
            coordHistory.AddFirst(Mouse.Position);

            mouseOffset = win.Area.Center - Mouse.Position;
            IsDraggging = true;
            SetOpacity(OPACITY_FACTOR);
        }

        private void Drag() {
            Area area = win.Area;
            area.Center = mousePos + mouseOffset;
            win.Move(area);
            coordHistory.AddFirst(area.Center);
            if (coordHistory.Count > 3) {
                coordHistory.RemoveLast();
            }
        }

        private void OnDragEnd() {
            ;
            if (InvalidWindow) {
                return;
            }
            IsDraggging = false;
            SetOpacity(1);

            if (windowWasMaximized) {
                win.Maximize();
            }
            if (!windowWasAlwaysOnTop) {
                win.SetAlwaysOnTop(false);
            }
            Console.WriteLine("On Drag end");

            WindowDockerCommands.ResumeDock(win);
        }

        private double GetVelocity => coordHistory.First.Value.Distance(coordHistory.Last.Value);

        private bool InvalidWindow => !win.IsValid || WinGroup.Desktop.Match(win);

        private async Task SetOpacity(double target) {

            int id = ++transparencyID;
            long startTime = Timer.Milliseconds;
            double startOpacity = win.Opacity;

            Console.WriteLine("Start opacity " + startOpacity);
            while (id == transparencyID && Timer.PassedFrom(startTime) is var passed && passed < OPACITY_FADE_TIME) {
                double perc = Percentage(passed, 0, OPACITY_FADE_TIME);
                double opacity = Lerp(startOpacity, target, perc);
                win.SetOpacity(opacity);
                await Task.Delay(1);
            }
            Console.WriteLine("final opacity " + target);
            win.SetOpacity(target);
        }

        public static double Percentage(double x, double min, double max) {
            return (x - min) / (max - min);
        }
        public static double Lerp(double a, double b, double t) {
            return a * (1 - t) + b * t;
        }
        #endregion
    }
}
