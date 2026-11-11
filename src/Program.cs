using System;
using System.Net;
using System.Threading;
using System.Windows;

namespace BlueFishPet
{
    internal static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                AppPaths.Log("未处理异常: " + e.ExceptionObject);
            };
            if (args != null && args.Length > 0 && args[0] == "--e2e")
            {
                try { E2E.Run(args); }
                catch (Exception ex)
                {
                    try
                    {
                        System.IO.File.WriteAllText(
                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "e2e-crash.txt"),
                            ex.ToString());
                    }
                    catch { }
                }
                return;
            }

            if (args != null && args.Length > 0 && args[0] == "--walktest")
            {
                try { WalkTest.Run(); }
                catch (Exception ex)
                {
                    try
                    {
                        System.IO.File.WriteAllText(
                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "walk-crash.txt"),
                            ex.ToString());
                    }
                    catch { }
                }
                return;
            }

            if (args != null && args.Length > 0 && args[0] == "--capture")
            {
                try { CaptureMode.Run(args); }
                catch (Exception ex)
                {
                    try
                    {
                        System.IO.File.WriteAllText(
                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "capture-error.txt"),
                            ex.ToString());
                    }
                    catch { }
                }
                return;
            }

            bool created;
            _mutex = new Mutex(true, "Local\\BlueFishPet_SingleInstance_7F2C", out created);
            if (!created)
            {
                PetApp.ActivateExisting();
                return;
            }

            try { System.Windows.Forms.Application.EnableVisualStyles(); }
            catch { }

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Ui.InstallScrollBarStyle();
            app.DispatcherUnhandledException += delegate(object s,
                System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                AppPaths.Log("界面异常: " + e.Exception);
                e.Handled = true;
            };
            PetApp pet = null;
            try
            {
                pet = new PetApp();
                pet.Start();
                app.Run();
            }
            catch (Exception ex)
            {
                AppPaths.Log("启动失败: " + ex);
                try
                {
                    MessageBox.Show("鱼鱼没能浮上来：\n" + ex.Message + "\n\n日志：" + AppPaths.LogFile,
                        "蓝色大肥鱼", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            }
            finally
            {
                try { if (pet != null) ConfigStore.Save(pet.Config); } catch { }
                try
                {
                    if (_mutex != null)
                    {
                        _mutex.ReleaseMutex();
                        _mutex.Dispose();
                    }
                }
                catch { }
            }
        }
    }
}

