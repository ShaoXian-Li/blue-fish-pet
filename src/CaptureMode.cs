using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BlueFishPet
{
    internal static class CaptureMode
    {
        private static string TraceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "capture-trace.txt");

        private static void Trace(string text)
        {
            try { File.AppendAllText(TraceFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + text + "\r\n"); }
            catch { }
        }

        public static void Run(string[] args)
        {
            Trace("run start");
            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
            if (args.Length > 1) outDir = args[1];
            try { Directory.CreateDirectory(outDir); } catch { }

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Ui.InstallScrollBarStyle();
            var pet = new PetApp();
            pet.Config.FirstRun = false;
            pet.Config.ApiKeyCipher = "";
            pet.Config.AlwaysOnTop = false;
            pet.Config.WindowLeft = -2400;
            pet.Config.WindowTop = -2400;
            Trace("pet app created");

            var main = new MainWindow(pet);
            pet.MainWindow = main;
            main.Left = -2400; main.Top = -2400;
            main.Show();
            Trace("main shown");
            var chat = new ChatWindow(pet);
            pet.Chat = chat;
            chat.Left = -2400; chat.Top = -2400;
            chat.Show();
            Trace("chat shown");
            var settings = new SettingsWindow(pet);
            settings.Left = -2400; settings.Top = -2400;
            settings.Show();
            Trace("settings shown");

            var seq = new DispatcherTimer(DispatcherPriority.Normal);
            seq.Interval = TimeSpan.FromMilliseconds(500);
            int step = 0;
            seq.Tick += delegate
            {
                step++;
                Trace("step " + step);
                if (step == 1)
                {
                    main.Pet.SetMood(PetMood.Idle);
                    main.ShowBubble("今天也要好好吃白饭。", 0);
                }
                else if (step == 2)
                {
                    Capture(main.Content as FrameworkElement, 390, 590, Path.Combine(outDir, "01-idle.png"));
                    main.SetThinking("正在翻找脑内的小鱼干…", "用户在问桌宠能不能接入 API。先确认设置、密钥和流式输出，再决定怎么回答。");
                }
                else if (step == 3)
                {
                    Capture(main.Content as FrameworkElement, 390, 590, Path.Combine(outDir, "02-thinking.png"));
                    main.Pet.SetMood(PetMood.Happy);
                    main.ShowBubble("答完啦，吃口白饭。", 0);
                }
                else if (step == 4)
                {
                    Capture(main.Content as FrameworkElement, 390, 590, Path.Combine(outDir, "03-happy.png"));
                    main.Pet.SetMood(PetMood.Eating);
                    main.ShowBubble("白饭！是白饭！鱼鱼开动了——", 0);
                    chat.AddUserMessage("帮我写一个快速排序，顺便说说它为什么快。", false);
                    chat.SetBusy(true);
                    chat.ShowThinking("把问题放进思考锅…", "先给出 Python 实现，再解释分治和基准选择。要短一点，别把用户淹了。");
                }
                else if (step == 5)
                {
                    Capture(main.Content as FrameworkElement, 390, 590, Path.Combine(outDir, "08-eating.png"));
                    main.Pet.SetMood(PetMood.Idle);
                    Capture(chat.Content as FrameworkElement, 448, 648, Path.Combine(outDir, "04-chat-thinking.png"));
                    chat.HideThinking();
                    chat.BeginPetResponse();
                    chat.AppendPetChunk("可以，鱼鱼给你写一个短的。\n\n```python\n");
                    chat.AppendPetChunk("def quick_sort(a):\n    if len(a) <= 1:\n        return a\n    pivot = a[len(a) // 2]\n    return (quick_sort([x for x in a if x < pivot])\n            + [x for x in a if x == pivot]\n            + quick_sort([x for x in a if x > pivot]))\n```\n\n");
                    chat.AppendPetChunk("它快在每次把问题劈成两半，平均只要 O(n log n) 次比较；但如果每次都挑到极端基准，就会退化成 O(n²)。");
                }
                else if (step == 6)
                {
                    chat.CompletePetResponse();
                    chat.SetBusy(false);
                    Capture(chat.Content as FrameworkElement, 448, 648, Path.Combine(outDir, "05-chat.png"));
                    Capture(settings.Content as FrameworkElement, 500, 780, Path.Combine(outDir, "06-settings.png"));
                    main.Pet.SetMood(PetMood.Walking);
                    main.Pet.SetFacing(1);
                    main.ShowBubble("出去走走。", 0);
                }
                else if (step == 7)
                {
                    Capture(main.Content as FrameworkElement, 390, 590, Path.Combine(outDir, "09-walking.png"));
                    seq.Stop();
                    app.Shutdown();
                }
            };
            seq.Start();
            app.Run();
        }

        private static void Capture(FrameworkElement element, int width, int height, string file)
        {
            try
            {
                element.UpdateLayout();
                var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(element);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (var fs = File.Create(file)) enc.Save(fs);
            }
            catch (Exception ex)
            {
                File.WriteAllText(file + ".error.txt", ex.ToString());
            }
        }
    }
}

