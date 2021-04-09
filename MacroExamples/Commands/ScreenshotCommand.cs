using MacroFramework.Commands;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MacroExamples {

    /// <summary>
    /// Takes a screenshot of an area from the screen and saves it to the clipboard
    /// </summary>
    public class ScreenshotCommand : Command {

        private const string DOWNLOAD_PATH = "YOUR_DOWNLOADS_PATH";
        private Point startPoint;

        protected override void InitializeActivators(ref ActivatorContainer acts) {

            // take screenshot with ctrl + s
            Bind screenDownBind = new Bind(KKey.LCtrl, KKey.S) { Settings = BindSettings.OnPress };
            Bind screenUpBind = new Bind(KKey.LCtrl, KKey.S) { Settings = BindSettings.Default };

            new BindActivator(screenDownBind, StartScreenShot);
            new BindActivator(screenUpBind, EndScreenShot);
        }

        private void StartScreenShot() {
            Console.WriteLine("Start screen");
            startPoint = Cursor.Position;
        }

        private void EndScreenShot() {
            Console.WriteLine("End screen");
            Point end = Cursor.Position;

            if (end == startPoint) {
                return;
            }

            Rectangle rect = new Rectangle(Math.Min(startPoint.X, end.X),
               Math.Min(startPoint.Y, end.Y),
               Math.Abs(startPoint.X - end.X),
               Math.Abs(startPoint.Y - end.Y));

            Console.WriteLine("Took screenshot");

            Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

            Clipboard.SetImage(bmp);
        }

        /// <summary>
        /// Saves the captured image in the downloads folder
        /// </summary>
        [BindActivator(KKey.LCtrl, KKey.S, KKey.D)]
        private void SaveClipboard() {
            SaveClipboardImage("clipboard_" + MacroFramework.Tools.Timer.Milliseconds);
        }

        private void SaveClipboardImage(string name) {
            if (Clipboard.ContainsImage()) {
                Image image = Clipboard.GetImage();
                image.Save(DOWNLOAD_PATH + "/" + name + ".png", ImageFormat.Png);
            }
        }
    }
}
