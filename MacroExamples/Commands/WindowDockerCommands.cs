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
    /// Docks windows to the screen edge, try out to test, binds are set as Ctrl + any arrow key
    /// </summary>
    public class WindowDockerCommands : Command {

        #region fields
        private static List<WindowDocker> dockers = new List<WindowDocker>();
        #endregion

        public static void PauseDock(Window win) {
            foreach (WindowDocker docker in dockers) {
                if (docker.WinState.Window == win) {
                    docker.Pause();
                }
            }
        }

        public static void ResumeDock(Window win) {
            foreach (WindowDocker docker in dockers) {
                if (docker.WinState.Window == win) {
                    docker.Resume();
                }
            }
        }

        public static bool UndockWindow(Window win, bool resetPos = false) {
            bool found = false;
            Console.WriteLine("Undock " + win.Title);
            for (int i = 0; i < dockers.Count; i++) {
                WindowDocker docker = dockers[i];
                if (docker.WinState.Window == win) {
                    docker.Undock(resetPos);
                    dockers.RemoveAt(i);
                    i--;
                    found = true;
                }
            }

            Console.WriteLine("found: " + found);
            return found;
        }

        public override void OnStart() {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }

        public override void OnClose() {
            OnProcessExit(null, null);
        }

        public void OnProcessExit(object sender, EventArgs e) {
            foreach (WindowDocker dock in dockers) {
                dock.Undock(true);
            }
        }

        #region binds
        [TimerActivator(50, TimeUnit.Milliseconds)]
        private void Update() {
            for (int i = 0; i < dockers.Count; i++) {
                WindowDocker docker = dockers[i];
                if (docker.IsUndocked) {
                    dockers.RemoveAt(i);
                    i--;
                } else {
                    docker.Update();
                }
            }
        }

        [BindActivator(KKey.GeneralBindKey, KKey.Up)]
        private void DockWindowUp() {
            if (WindowTools.GetValidWindow(out Window win)) {
                Console.WriteLine("DOcking window up");
                UndockWindow(win);
                AddDocker(new WindowDocker(win, WindowDocker.Direction.Up));
            }
        }

        [BindActivator(KKey.GeneralBindKey, KKey.Down)]
        private void DockWindowDown() {
            if (WindowTools.GetValidWindow(out Window win)) {
                Console.WriteLine("DOcking window down");
                UndockWindow(win);
                AddDocker(new WindowDocker(win, WindowDocker.Direction.Down));
            }
        }

        [BindActivator(KKey.GeneralBindKey, KKey.Right)]
        private void DockWindowRight() {
            if (WindowTools.GetValidWindow(out Window win)) {
                Console.WriteLine("DOcking window up");
                UndockWindow(win);
                AddDocker(new WindowDocker(win, WindowDocker.Direction.Right));
            }
        }

        [BindActivator(KKey.GeneralBindKey, KKey.Left)]
        private void DockWindowLeft() {
            if (WindowTools.GetValidWindow(out Window win)) {
                Console.WriteLine("DOcking window down");
                UndockWindow(win);
                AddDocker(new WindowDocker(win, WindowDocker.Direction.Left));
            }
        }

        [KeyActivator(KKey.MouseMiddle)]
        private void UndockCurrentWindow() {
            if (!WindowTools.IsInvalidWindow(Window.FromMouse)) {
                UndockWindow(Window.FromMouse);
            }
        }

        public static void AddDocker(WindowDocker docker) {
            for (int i = 0; i < dockers.Count; i++) {
                if (docker.WinState == dockers[i].WinState) {
                    Console.WriteLine("Remove old docker");
                    dockers.RemoveAt(i);
                }
            }
            docker.Start();
            dockers.Add(docker);
        }
        #endregion
    }

    public class WindowDocker {
        #region fields
        private double PEEK_OPACITY = 0.5;
        private const int PEEK_SIZE = 50;
        private const int PEEK_TIMEOUT = 250;
        public enum Direction { Up, Right, Down, Left }
        public enum ShowState { Hidden, Peek, PeekTimeoutDisable, Visible }

        public WindowState WinState { get; private set; }
        public Direction DockDirection { get; private set; }

        public bool IsPaused { get; private set; }
        public bool IsUndocked { get; private set; }

        private Area hiddenArea, peekArea, visibleArea;

        private int timeout;
        private long lastMouseOnTopTimeStamp;
        private long peekActivateTimestamp;
        private ShowState showState;


        private Animation currentAnimation;
        private Coord mousePos;
        #endregion

        public WindowDocker(Window window, Direction direction, int timeout = 500) {
            this.WinState = new WindowState(window);
            if (window.IsMaximized) {
                Area area = window.Area;
                window.Restore();
                window.Move(area);
            }

            DockDirection = direction;
            this.timeout = timeout;
        }

        public void Start() {
            Init();
            WinState.Window.SetAlwaysOnTop(true);
        }

        private void Init() {
            showState = ShowState.Visible;
            SetHideAndShowPos();
            MoveTo(visibleArea);
            IsPaused = false;
            lastMouseOnTopTimeStamp = Timer.Milliseconds;
        }

        #region management
        public void Update() {
            if (IsPaused) {
                return;
            }
            if (!WinState.Window.Exists) {
                Undock(false);
                return;
            } else if (WinState.Window.IsMaximized || WinState.Window.IsMinimized) {
                WinState.Window.Restore();
            }

            mousePos = Mouse.Position;
            if (visibleArea.Contains(mousePos)) {
                lastMouseOnTopTimeStamp = Timer.Milliseconds;
                RevealUpdate();
            } else {
                HideUpdate();
            }
        }

        private void RevealUpdate() {
            if (peekArea.Contains(mousePos)) {
                if (showState == ShowState.Hidden) {
                    Peek();
                } else if (showState == ShowState.Peek && Timer.PassedFrom(peekActivateTimestamp) >= PEEK_TIMEOUT) {
                    SetHidden(ShowState.PeekTimeoutDisable);
                }
            } else {
                if (showState == ShowState.Peek) {
                    SetVisible();
                } else if (showState == ShowState.PeekTimeoutDisable) {
                    showState = ShowState.Hidden;
                }
            }
        }
        private void HideUpdate() {
            if (showState != ShowState.Hidden && Timer.PassedFrom(lastMouseOnTopTimeStamp) > timeout) {
                SetHidden(ShowState.Hidden);
            } else if (showState == ShowState.PeekTimeoutDisable) {
                showState = ShowState.Hidden;
            }
        }

        public void Pause() {
            IsPaused = true;
        }

        public void Resume() {
            Init();
        }

        public void Undock(bool resetPos) {
            if (IsUndocked) {
                return;
            }
            Console.WriteLine("Undock docker: " + WinState.Window.Exe);
            IsUndocked = true;
            if (WindowTools.IsInvalidWindow(WinState.Window)) {
                if (resetPos) {
                    WinState.Window.Move(visibleArea);
                }
            }
            if (!resetPos) {
                WinState.PrevArea = Area.Zero;
            }
            WinState.Restore();
        }
        #endregion

        #region position
        private void SetHideAndShowPos() {
            Area area = WinState.Window.Area;
            Area monitorArea = DockDirection == Direction.Up || DockDirection == Direction.Down ? Monitor.FromArea(area).Area : Monitor.Screen;

            area = area.ClampWithin(monitorArea, false);
            hiddenArea = area;
            visibleArea = area;
            peekArea = area;

            if (DockDirection == Direction.Up) {
                hiddenArea.Bottom = monitorArea.Top + 1;
                peekArea.Bottom = monitorArea.Top + PEEK_SIZE;
                visibleArea.Top = monitorArea.Top;
            } else if (DockDirection == Direction.Down) {
                hiddenArea.Top = monitorArea.Bottom - 1;
                peekArea.Top = monitorArea.Bottom - PEEK_SIZE;
                visibleArea.Bottom = monitorArea.Bottom;
            } else if (DockDirection == Direction.Left) {
                hiddenArea.Right = monitorArea.Left + 1;
                peekArea.Right = monitorArea.Left + PEEK_SIZE;
                visibleArea.Left = monitorArea.Left;
            } else if (DockDirection == Direction.Right) {
                hiddenArea.Left = monitorArea.Right - 1;
                peekArea.Left = monitorArea.Right - PEEK_SIZE;
                visibleArea.Right = monitorArea.Right;
            }
        }

        private void CancelMove() {
            currentAnimation?.Stop();
            currentAnimation = null;
        }
        private void MoveTo(Area area) {
            CancelMove();
            currentAnimation = WindowTools.SmoothMove(WinState.Window, area);
        }

        public void SetVisible() {
            Console.WriteLine("Set visible");
            WinState.Window.SetAlwaysOnTop(true);
            if (!WinState.WasClickThrough) {
                WinState.Window.SetClickThrough(false);
            }
            WinState.RestoreOpacity();

            MoveTo(visibleArea);
            showState = ShowState.Visible;
        }
        public void SetHidden(ShowState state) {
            Console.WriteLine("Set hidden");
            WinState.Window.SetAlwaysOnTop(true);
            WinState.Window.SetClickThrough(true);
            WinState.Window.SetOpacity(PEEK_OPACITY);

            MoveTo(hiddenArea);
            showState = state;
        }
        public void Peek() {
            Console.WriteLine("Set peek");
            WinState.Window.SetAlwaysOnTop(true);
            WinState.Window.SetClickThrough(true);
            WinState.Window.SetOpacity(PEEK_OPACITY);
            MoveTo(peekArea);
            peekActivateTimestamp = Timer.Milliseconds;
            showState = ShowState.Peek;
        }

        #endregion
    }

    public class WindowState {
        #region fields
        public Window Window { get; private set; }
        public bool WasAlwaysOnTop { get; set; }
        public bool WasMinimized { get; set; }
        public bool WasMaximized { get; set; }
        public bool WasClickThrough { get; set; }
        public double PrevOpacity { get; set; }
        public Area PrevArea { get; set; }
        #endregion

        public WindowState(Window win) {
            this.Window = win;
            WasAlwaysOnTop = win.IsAlwaysOnTop;
            WasMinimized = win.IsMinimized;
            WasMaximized = win.IsMaximized;
            WasClickThrough = win.IsClickThrough;
            PrevOpacity = win.Opacity;
            PrevArea = win.Area;
        }

        public void Restore() {
            RestoreState();
            RestoreOpacity();
            RestoreArea();
        }

        public void RestoreState() {
            if (WasMinimized && !Window.IsMinimized) {
                Window.Minimize();
            }
            if (WasMaximized && !Window.IsMaximized) {
                Window.Maximize();
            }
            if (!WasMaximized && !WasMinimized) {
                Window.Restore();
            }
            Window.SetAlwaysOnTop(WasAlwaysOnTop);
            RestoreClickThrough();
        }

        public void RestoreClickThrough() {
            Window.SetClickThrough(WasClickThrough);
        }
        public void RestoreOpacity() {
            Window.SetOpacity(PrevOpacity);
        }
        public void RestoreArea() {
            if (PrevArea != Area.Zero) {
                Window.Move(PrevArea);
            }
        }
    }
}
