using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BlueFishPet
{
    internal static class WalkTest
    {
        public static void Run()
        {
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Ui.InstallScrollBarStyle();
            var pet = new PetApp();
            pet.Config.WanderEnabled = true;
            pet.Config.FirstRun = false;
            pet.Config.AlwaysOnTop = false;
            pet.Config.WindowLeft = 320;
            pet.Config.WindowTop = 320;
            var main = new MainWindow(pet);
            pet.MainWindow = main;
            main.Show();
            double startLeft = 0;
            var log = new StringBuilder();
            var starter = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(900) };
            starter.Tick += delegate
            {
                starter.Stop();
                startLeft = main.Left;
                log.AppendLine("start=" + startLeft + " visible=" + main.IsVisible);
                main.StartWalk();
                log.AppendLine("walking=" + main.IsWalking);
            };
            starter.Start();
            var shot = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(1600) };
            shot.Tick += delegate
            {
                shot.Stop();
                log.AppendLine("mid=" + main.Left + " walking=" + main.IsWalking);
                try
                {
                    var root = main.Content as FrameworkElement;
                    root.UpdateLayout();
                    var rtb = new RenderTargetBitmap(390, 590, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(root);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots"));
                    using (var fs = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots", "09-walking.png"))) enc.Save(fs);
                }
                catch (Exception ex) { log.AppendLine("shot error " + ex.Message); }
            };
            shot.Start();
            var end = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(5.5) };
            end.Tick += delegate
            {
                end.Stop();
                log.AppendLine("end=" + main.Left + " walkEndedDiff=" + Math.Abs(main.Left - startLeft) + " walking=" + main.IsWalking);
                log.AppendLine(Math.Abs(main.Left - startLeft) > 25 && !main.IsWalking ? "WALK_OK" : "WALK_FAIL");
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "walk-result.txt"), log.ToString(), Encoding.UTF8);
                app.Shutdown();
            };
            end.Start();
            app.Run();
        }
    }
}
