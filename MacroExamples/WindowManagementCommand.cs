using MacroFramework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUtilities;

namespace MacroExamples {

    /// <summary>
    /// Maximizes, minimizes or closes a window based on an activator event
    /// </summary>
    public class WindomManagementCommands : Command {

        private Window currentWindow = Window.FromMouse;

        protected override void InitializeActivators(ref ActivatorContainer acts) {

            BindActivator ctrlClick = new BindActivator(new Bind(KKey.LCtrl, KKey.MouseLeft));
            BindActivator ctrlRightClick = new BindActivator(new Bind(KKey.LCtrl, KKey.MouseRight));

            // Maximize window on Ctrl + double click
            new RepeatActivator(ctrlClick, MaximizeWindow) { RepeatCount = 2, OnEachActivate = SetCurrentWindow }.AssignTo(acts);

            // Minimize window on Ctrl + right click
            new RepeatActivator(ctrlRightClick, MinimizeWindow) { RepeatCount = 1, OnEachActivate = SetCurrentWindow }.AssignTo(acts);

            // Close window on Ctrl + double right click
            new RepeatActivator(ctrlRightClick, CloseWindow) { RepeatCount = 2, OnEachActivate = SetCurrentWindow }.AssignTo(acts);
        }

        /// <summary>
        /// RepeatActivator is activated with slight delay so we need to set the current window at the right time i.e. when Ctrl+Click happens not after the repeat activator timeout. 
        /// </summary>
        private void SetCurrentWindow() {
            currentWindow = Window.FromMouse;
        }

        /// <summary>
        /// Check that the current window is a valid window and that the current window is not the Windows desktop
        /// </summary>
        private bool IsInvalidWindow() {
            return !currentWindow.IsValid || WinGroup.Desktop.Match(currentWindow);
        }

        private void MaximizeWindow() {
            if (IsInvalidWindow()) {
                return;
            }

            if (currentWindow.IsMaximized) {
                currentWindow.Restore();
            } else {
                currentWindow.Maximize();
            }
        }

        private void MinimizeWindow() {
            if (IsInvalidWindow()) {
                return;
            }

            if (currentWindow.IsMinimized) {
                currentWindow.Restore();
            } else {
                currentWindow.Minimize();
            }
        }

        private void CloseWindow() {
            if (IsInvalidWindow()) {
                return;
            }

            currentWindow.Close();
        }
    }
}
