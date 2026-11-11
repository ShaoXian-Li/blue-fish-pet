using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace BlueFishPet
{
    public class SettingsWindow : Window
    {
        private readonly PetApp _app;
        private readonly InputField _key;
        private readonly InputField _base;
        private readonly InputField _model;
        private readonly ToggleSwitch _topmost;
        private readonly ToggleSwitch _showOnStart;
        private readonly ToggleSwitch _thought;
        private readonly ToggleSwitch _wander;
        private readonly ToggleSwitch _autoStart;
        private readonly TextBlock _testStatus;
        private readonly PillButton _testButton;
        private readonly TextBlock _keyState;
        private bool _clearKey;
        private bool _testing;

        public SettingsWindow(PetApp app)
        {
            _app = app;
            Width = 500;
            Height = 780;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            Owner = app.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = Ui.Font;
            Title = "鱼鱼的脑袋设置";

            var shell = new Grid();
            shell.Margin = new Thickness(18);
            var card = new Border();
            card.CornerRadius = new CornerRadius(24);
            card.Background = Ui.BA("#F5121D32", 250);
            card.BorderBrush = Ui.BA("#5D93C8", 100);
            card.BorderThickness = new Thickness(1);
            card.Effect = Ui.Shadow(34, 0.5, 10);
            shell.Children.Add(card);

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(66) });
            card.Child = root;

            var headerBg = new Border();
            headerBg.CornerRadius = new CornerRadius(24, 24, 0, 0);
            headerBg.Background = Ui.Linear("#1B3154", "#15233C", 90);
            Grid.SetRow(headerBg, 0);
            root.Children.Add(headerBg);
            var header = new Grid();
            header.Margin = new Thickness(20, 12, 14, 8);
            Grid.SetRow(header, 0);
            root.Children.Add(header);
            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var title = Ui.T("鱼鱼的脑袋设置", 17, "#F2FAFF", true);
            titleStack.Children.Add(title);
            var sub = Ui.T("接上 DeepSeek，鱼鱼才会真的开始思考。", 10.5, "#8FA8C6", false);
            sub.Margin = new Thickness(0, 3, 0, 0);
            titleStack.Children.Add(sub);
            header.Children.Add(titleStack);
            var close = new Border();
            close.Width = 30; close.Height = 30;
            close.CornerRadius = new CornerRadius(9);
            close.Background = Ui.BA("#263E60", 200);
            close.Cursor = Cursors.Hand;
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Top;
            var closeText = Ui.T("×", 17, "#D6E9FF", false);
            closeText.HorizontalAlignment = HorizontalAlignment.Center;
            closeText.VerticalAlignment = VerticalAlignment.Center;
            close.Child = closeText;
            close.MouseLeftButtonUp += delegate { Close(); };
            header.Children.Add(close);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll.Padding = new Thickness(20, 4, 20, 8);
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            var body = new StackPanel();
            scroll.Content = body;

            body.Children.Add(Section("大脑接入"));
            _key = new InputField("sk-... 只保存到本机", true, false);
            _key.Height = 42;
            body.Children.Add(_key);
            var keyRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 5, 0, 2) };
            _keyState = Ui.T(app.HasApiKey ? "● 已保存加密 Key" : "○ 尚未配置 API Key", 10, app.HasApiKey ? "#7CE3A5" : "#FFC46B", false);
            keyRow.Children.Add(_keyState);
            var clear = Ui.T("清除已保存的 Key", 9.8, "#8FB9DC", true);
            clear.Margin = new Thickness(14, 0, 0, 0);
            clear.Cursor = Cursors.Hand;
            clear.MouseLeftButtonUp += delegate
            {
                _clearKey = !_clearKey;
                _keyState.Text = _clearKey ? "● 保存后将清除 Key" : (app.HasApiKey ? "● 已保存加密 Key" : "○ 尚未配置 API Key");
                _keyState.Foreground = Ui.B(_clearKey ? "#FF9A9A" : (app.HasApiKey ? "#7CE3A5" : "#FFC46B"));
            };
            keyRow.Children.Add(clear);
            body.Children.Add(keyRow);
            var keyNote = Ui.T("Key 用 Windows 当前用户加密保存，只留在本机；聊天记录不会上传到别处。", 9.4, "#6D88A8", false);
            keyNote.LineHeight = 14;
            keyNote.Margin = new Thickness(2, 1, 2, 6);
            body.Children.Add(keyNote);

            var getKey = Ui.T("还没有 Key？去 platform.deepseek.com 申请 →", 10.2, "#7FE7FF", false);
            getKey.Cursor = Cursors.Hand;
            getKey.Margin = new Thickness(2, 0, 2, 8);
            getKey.MouseLeftButtonUp += delegate
            {
                try { Process.Start(new ProcessStartInfo("https://platform.deepseek.com/api_keys") { UseShellExecute = true }); }
                catch { }
            };
            body.Children.Add(getKey);

            body.Children.Add(FieldLabel("API 地址（兼容 OpenAI 协议）"));
            _base = new InputField("https://api.deepseek.com", false, false);
            _base.Height = 42;
            _base.Text = app.Config.ApiBase;
            body.Children.Add(_base);
            var baseNote = Ui.T("一般保持默认。也可以填自建代理，例如 https://你的域名/v1。", 9.4, "#6D88A8", false);
            baseNote.Margin = new Thickness(2, 4, 2, 6);
            body.Children.Add(baseNote);

            body.Children.Add(FieldLabel("模型"));
            _model = new InputField("deepseek-chat", false, false);
            _model.Height = 42;
            _model.Text = app.Config.Model;
            body.Children.Add(_model);
            var chips = new WrapPanel { Margin = new Thickness(0, 7, 0, 2) };
            chips.Children.Add(ModelChip("deepseek-chat", "日常对话，快"));
            chips.Children.Add(ModelChip("deepseek-reasoner", "深度思考，会展示思路"));
            body.Children.Add(chips);

            var testRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 5) };
            _testButton = new PillButton("测试连接", false);
            _testButton.Width = 94;
            _testButton.Height = 30;
            _testButton.Clicked += delegate { RunTest(); };
            testRow.Children.Add(_testButton);
            _testStatus = Ui.T("", 10.5, "#8FA8C6", false);
            _testStatus.Margin = new Thickness(10, 0, 0, 0);
            _testStatus.VerticalAlignment = VerticalAlignment.Center;
            testRow.Children.Add(_testStatus);
            body.Children.Add(testRow);

            body.Children.Add(Section("桌宠习惯"));
            _topmost = ToggleRow(body, "始终显示在最前面", "不被其他窗口盖住，摸鱼时也看得见。", app.Config.AlwaysOnTop);
            _showOnStart = ToggleRow(body, "启动时显示鱼鱼", "关掉后只在托盘里待命。", app.Config.ShowOnStart);
            _thought = ToggleRow(body, "显示思考气泡", "把推理片段变成鱼鱼头上的小气泡。", app.Config.ThoughtBubble);
            _wander = ToggleRow(body, "允许自由散步", "闲下来时，她会自己在桌面上慢慢走走。", app.Config.WanderEnabled);
            _autoStart = ToggleRow(body, "开机自动启动", "登录 Windows 后自动把鱼鱼叫起来。", app.Config.AutoStart);

            var footer = new Grid();
            footer.Margin = new Thickness(20, 8, 20, 14);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var hintRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var hint = Ui.T("修改会立即生效。", 9.6, "#6D88A8", false);
            hint.VerticalAlignment = VerticalAlignment.Center;
            hintRow.Children.Add(hint);
            var clearHistory = Ui.T("清空聊天记录", 9.6, "#E79AA7", true);
            clearHistory.Margin = new Thickness(10, 0, 0, 0);
            clearHistory.VerticalAlignment = VerticalAlignment.Center;
            clearHistory.Cursor = Cursors.Hand;
            clearHistory.MouseLeftButtonUp += delegate
            {
                _app.ClearHistory();
                clearHistory.Text = "已清空";
                clearHistory.Foreground = Ui.B("#7CE3A5");
            };
            hintRow.Children.Add(clearHistory);
            footer.Children.Add(hintRow);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new PillButton("取消", false);
            cancel.Width = 76; cancel.Height = 34;
            cancel.Margin = new Thickness(0, 0, 8, 0);
            cancel.Clicked += delegate { Close(); };
            buttons.Children.Add(cancel);
            var save = new PillButton("保存并回去", true);
            save.Width = 112; save.Height = 34;
            save.Clicked += delegate { Save(); };
            buttons.Children.Add(save);
            Grid.SetColumn(buttons, 1);
            footer.Children.Add(buttons);

            Content = shell;
            KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); };
            Loaded += delegate { };
        }

        private TextBlock Section(string text)
        {
            var t = Ui.T(text, 11, "#8FE9FF", true);
            t.Margin = new Thickness(2, 10, 0, 7);
            return t;
        }

        private TextBlock FieldLabel(string text)
        {
            var t = Ui.T(text, 10.5, "#A8BDD7", true);
            t.Margin = new Thickness(2, 0, 0, 5);
            return t;
        }

        private Border ModelChip(string value, string description)
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(10);
            b.Background = Ui.BA("#1B2D46", 220);
            b.BorderBrush = Ui.BA("#5D93C8", 65);
            b.BorderThickness = new Thickness(1);
            b.Padding = new Thickness(8, 4, 8, 4);
            b.Margin = new Thickness(0, 0, 6, 5);
            b.Cursor = Cursors.Hand;
            var sp = new StackPanel();
            sp.Children.Add(Ui.T(value, 9.8, "#BFDDF5", true));
            sp.Children.Add(Ui.T(description, 8.8, "#6D88A8", false));
            b.Child = sp;
            b.MouseLeftButtonUp += delegate { _model.Text = value; };
            b.MouseEnter += delegate { b.Background = Ui.BA("#2A4768", 230); };
            b.MouseLeave += delegate { b.Background = Ui.BA("#1B2D46", 220); };
            return b;
        }

        private ToggleSwitch ToggleRow(StackPanel parent, string title, string desc, bool value)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, 0);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var sp = new StackPanel();
            sp.Children.Add(Ui.T(title, 11.6, "#E5F1FF", false));
            var d = Ui.T(desc, 9.2, "#718BA9", false);
            d.Margin = new Thickness(0, 2, 0, 0);
            sp.Children.Add(d);
            grid.Children.Add(sp);
            var toggle = new ToggleSwitch();
            toggle.IsHitTestVisible = false; // the whole row is the click target
            toggle.Checked = value;
            toggle.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(toggle, 1);
            grid.Children.Add(toggle);
            var hit = new Border();
            hit.Background = Brushes.Transparent;
            hit.CornerRadius = new CornerRadius(9);
            hit.Padding = new Thickness(3);
            hit.Margin = new Thickness(0, 2, 0, 2);
            hit.Child = grid;
            hit.MouseLeftButtonUp += delegate
            {
                if (toggle.Checked) toggle.Checked = false;
                else toggle.Checked = true;
            };
            parent.Children.Add(hit);
            return toggle;
        }

        private void RunTest()
        {
            if (_testing) return;
            string key = _key.Text.Trim();
            if (string.IsNullOrEmpty(key) && !_clearKey) key = _app.GetApiKey();
            if (string.IsNullOrEmpty(key))
            {
                _testStatus.Text = "先填一个 Key 再测试。";
                _testStatus.Foreground = Ui.B("#FFB2B2");
                return;
            }
            string baseUrl = _base.Text.Trim();
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                _testStatus.Text = "API 地址要以 http(s):// 开头。";
                _testStatus.Foreground = Ui.B("#FFB2B2");
                return;
            }
            _testing = true;
            _testButton.Enabled = false;
            _testButton.Text = "测试中…";
            _testStatus.Text = "正在敲 DeepSeek 的门…";
            _testStatus.Foreground = Ui.B("#FFD36E");
            string model = _model.Text.Trim();
            if (string.IsNullOrEmpty(model)) model = "deepseek-chat";
            _app.Api.Test(key, baseUrl, model, delegate(string result)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _testing = false;
                    _testButton.Enabled = true;
                    _testButton.Text = "测试连接";
                    _testStatus.Text = result;
                    _testStatus.Foreground = Ui.B(result.StartsWith("连接成功") ? "#7CE3A5" : "#FF9A9A");
                }));
            });
        }

        private void Save()
        {
            string baseUrl = _base.Text.Trim();
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                _testStatus.Text = "API 地址要以 http(s):// 开头。";
                _testStatus.Foreground = Ui.B("#FFB2B2");
                return;
            }
            string key = ApiKeyUtil.Trim(_key.Text);
            bool keyProblemShown = false;
            if (!_clearKey && !string.IsNullOrEmpty(key))
            {
                string keyProblem = ApiKeyUtil.Problem(key);
                if (keyProblem != "")
                {
                    // Still save the non-key settings below; just do not overwrite the key.
                    _testStatus.Text = keyProblem;
                    _testStatus.Foreground = Ui.B("#FF9A9A");
                    keyProblemShown = true;
                }
            }
            if (_clearKey)
            {
                _app.Config.ApiKeyCipher = "";
                _app.RefreshKey();
            }
            else if (!keyProblemShown && !string.IsNullOrEmpty(key))
            {
                try
                {
                    _app.Config.ApiKeyCipher = ConfigStore.ProtectKey(key);
                    _app.RefreshKey();
                }
                catch (Exception ex)
                {
                    _testStatus.Text = "保存 Key 失败：" + ex.Message;
                    _testStatus.Foreground = Ui.B("#FF9A9A");
                    return;
                }
            }
            _app.Config.ApiBase = baseUrl.TrimEnd('/');
            _app.Config.Model = string.IsNullOrWhiteSpace(_model.Text) ? "deepseek-chat" : _model.Text.Trim();
            _app.Config.AlwaysOnTop = _topmost.Checked;
            _app.Config.ShowOnStart = _showOnStart.Checked;
            _app.Config.ThoughtBubble = _thought.Checked;
            _app.Config.WanderEnabled = _wander.Checked;
            _app.Config.AutoStart = _autoStart.Checked;
            _app.Config.FirstRun = false;
            try
            {
                ApplyAutoStart(_app.Config.AutoStart);
            }
            catch (Exception ex) { AppPaths.Log("设置开机启动失败: " + ex.Message); }
            _app.SaveConfig();
            _app.ApplySettings();
            Close();
        }

        private static void ApplyAutoStart(bool enabled)
        {
            using (var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (run == null) return;
                if (enabled)
                    run.SetValue("BlueFishPet", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                else
                    run.DeleteValue("BlueFishPet", false);
            }
        }
    }
}

