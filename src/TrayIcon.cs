using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace BlueFishPet
{
    public class TrayIcon : IDisposable
    {
        private readonly PetApp _app;
        private readonly NotifyIcon _icon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _showItem;
        private bool _disposed;

        public TrayIcon(PetApp app)
        {
            _app = app;
            _menu = new ContextMenuStrip();
            _menu.BackColor = Color.FromArgb(17, 28, 49);
            _menu.ForeColor = Color.FromArgb(234, 244, 255);
            _menu.Font = new Font("Microsoft YaHei UI", 9f);
            _menu.ShowImageMargin = false;
            _menu.Padding = new Padding(3);
            _menu.Renderer = new DarkMenuRenderer();

            _showItem = new ToolStripMenuItem("隐藏鱼鱼");
            _showItem.Click += delegate { OnUi(delegate { _app.TogglePetVisible(); }); };
            _menu.Items.Add(_showItem);
            _menu.Items.Add(MakeItem("和我聊天", delegate { OnUi(delegate { _app.ShowChat(true); }); }));
            _menu.Items.Add(MakeItem("喂一碗白饭", delegate { OnUi(delegate { _app.MainWindow.Feed(); }); }));
            _menu.Items.Add(MakeItem("散散步", delegate { OnUi(delegate { _app.MainWindow.StartWalk(); }); }));
            _menu.Items.Add(MakeItem("让她停下", delegate { OnUi(delegate { _app.MainWindow.StopWalk(); }); }));
            _menu.Items.Add(MakeItem("摸摸鱼头", delegate
            {
                OnUi(delegate
                {
                    _app.MainWindow.Pet.PlayWave();
                    _app.MainWindow.ShowBubble("唔…再摸要收费了，收白饭。", 3.5);
                });
            }));
            _menu.Items.Add(MakeItem("让她睡一会", delegate { OnUi(delegate { _app.MainWindow.Sleep(); }); }));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(MakeItem("设置", delegate { OnUi(delegate { _app.ShowSettings(); }); }));
            _menu.Items.Add(MakeItem("打开设置文件位置", delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", AppPaths.DataDir); } catch { }
            }));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(MakeItem("退出", delegate { OnUi(delegate { _app.Quit(); }); }));

            _icon = new NotifyIcon();
            _icon.Text = "蓝色大肥鱼 · DeepSeek 桌宠";
            _icon.Icon = LoadIcon();
            _icon.Visible = true;
            _icon.ContextMenuStrip = _menu;
            _icon.DoubleClick += delegate { OnUi(delegate { _app.ShowChat(true); }); };
        }

        private ToolStripMenuItem MakeItem(string text, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text);
            item.ForeColor = Color.FromArgb(234, 244, 255);
            item.BackColor = Color.Transparent;
            item.Padding = new Padding(8, 5, 8, 5);
            item.Click += handler;
            return item;
        }

        private void OnUi(Action action)
        {
            var dispatcher = System.Windows.Application.Current != null ? System.Windows.Application.Current.Dispatcher : null;
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(action);
        }

        private static Icon LoadIcon()
        {
            try
            {
                string exe = Assembly.GetEntryAssembly() != null ? Assembly.GetEntryAssembly().Location : null;
                if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
                {
                    var icon = Icon.ExtractAssociatedIcon(exe);
                    if (icon != null) return icon;
                }
            }
            catch { }
            return SystemIcons.Application;
        }

        public void ShowWelcome()
        {
            try
            {
                _icon.BalloonTipTitle = "蓝色大肥鱼已就位";
                _icon.BalloonTipText = "点我聊天；右键可以喂白饭。第一次使用请先在设置里填 DeepSeek API Key。";
                _icon.BalloonTipIcon = ToolTipIcon.Info;
                _icon.ShowBalloonTip(5000);
            }
            catch { }
        }

        public void RefreshVisibility(bool mainVisible)
        {
            _showItem.Text = mainVisible ? "隐藏鱼鱼" : "显示鱼鱼";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _icon.Visible = false; _icon.Dispose(); } catch { }
            try { _menu.Dispose(); } catch { }
        }

        private class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkColors()) { }
        }

        private class DarkColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected { get { return Color.FromArgb(39, 71, 106); } }
            public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(39, 71, 106); } }
            public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(39, 71, 106); } }
            public override Color MenuItemBorder { get { return Color.FromArgb(76, 151, 207); } }
            public override Color MenuBorder { get { return Color.FromArgb(60, 94, 130); } }
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(17, 28, 49); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(17, 28, 49); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(17, 28, 49); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(17, 28, 49); } }
            public override Color SeparatorDark { get { return Color.FromArgb(45, 69, 98); } }
            public override Color SeparatorLight { get { return Color.FromArgb(22, 37, 60); } }
        }
    }
}

