using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BlueFishPet
{
    internal static class E2E
    {
        private static string ResultFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "e2e-result.txt");
        private static StringBuilder Log = new StringBuilder();

        private static void Say(string s)
        {
            Log.AppendLine(s);
            try { File.WriteAllText(ResultFile, Log.ToString(), Encoding.UTF8); } catch { }
        }

        public static void Run(string[] args)
        {
            Say("e2e start");
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Ui.InstallScrollBarStyle();
            var pet = new PetApp();
            pet.Config.FirstRun = false;
            pet.Config.ApiBase = "http://127.0.0.1:8765";
            pet.Config.Model = "deepseek-chat";
            pet.Config.AlwaysOnTop = false;
            pet.Config.WindowLeft = -2400;
            pet.Config.WindowTop = -2400;
            pet.Config.ChatLeft = -2400;
            pet.Config.ChatTop = -2400;

            var main = new MainWindow(pet);
            pet.MainWindow = main;
            main.Left = -2400; main.Top = -2400;
            main.Show();
            var chat = new ChatWindow(pet);
            pet.Chat = chat;
            chat.Left = -2400; chat.Top = -2400;
            chat.Show();
            Say("windows ready, key=" + pet.HasApiKey);

            var starter = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(500) };
            starter.Tick += delegate
            {
                starter.Stop();
                Say("history before=" + pet.History.Count);
                pet.SendMessage("写个短例子");
                Say("sent, busy=" + pet.Busy);
            };
            starter.Start();

            var poll = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(250) };
            int ticks = 0;
            poll.Tick += delegate
            {
                ticks++;
                if (pet.Busy && ticks < 120) return;
                poll.Stop();
                var extra = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(500) };
                extra.Tick += delegate
                {
                    extra.Stop();
                    string last = "";
                    for (int i = pet.History.Count - 1; i >= 0; i--)
                    {
                        if (pet.History[i].Role == "assistant") { last = pet.History[i].Content; break; }
                    }
                    Say("history after=" + pet.History.Count);
                    Say("assistant=" + last.Replace("\r", "").Replace("\n", "\\n"));
                    Say("busy=" + pet.Busy + " mood=" + main.Pet.Mood);
                    bool ok = last.Contains("鱼鱼给你写一个短的")
                              && !last.Contains("[[mood:")
                              && last.Contains("def qs")
                              && main.Pet.Mood == PetMood.Happy;
                    Say(ok ? "E2E_OK" : "E2E_FAIL");
                    try
                    {
                        var root = chat.Content as FrameworkElement;
                        root.UpdateLayout();
                        var rtb = new RenderTargetBitmap(448, 648, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(root);
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(rtb));
                        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
                        Directory.CreateDirectory(dir);
                        using (var fs = File.Create(Path.Combine(dir, "07-live-api.png"))) enc.Save(fs);
                    }
                    catch (Exception ex) { Say("capture error " + ex.Message); }
                    app.Shutdown();
                };
                extra.Start();
            };
            poll.Start();
            app.Run();
        }
    }
}

