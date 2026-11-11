using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BlueFishPet
{
    public class ChatWindow : Window
    {
        private readonly PetApp _app;
        private readonly Border _card;
        private readonly StackPanel _messages;
        private readonly ScrollViewer _scroll;
        private WrapPanel _suggestions;
        private readonly TextBox _input;
        private readonly TextBlock _placeholder;
        private readonly Border _send;
        private readonly Path _sendIcon;
        private readonly Rectangle _stopIcon;
        private readonly TextBlock _status;
        private readonly Ellipse _statusDot;
        private readonly Border _setupBanner;
        private readonly Border _thinkCard;
        private readonly TextBlock _thinkStage;
        private readonly TextBlock _thinkExcerpt;
        private readonly StackPanel _thinkDots;
        private readonly DispatcherTimer _renderTimer;
        private readonly DispatcherTimer _thinkTimer;

        private bool _busy;
        private bool _stickToBottom = true;
        private bool _placed;
        private ChatBubble _streamBubble;
        private string _streamText = "";
        private int _thinkStep;
        private int _suggestionRow;

        public ChatWindow(PetApp app)
        {
            _app = app;
            Width = 448;
            Height = 648;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Title = "和蓝色大肥鱼聊天";
            FontFamily = Ui.Font;
            UseLayoutRounding = true;

            var shell = new Grid();
            shell.Margin = new Thickness(14);
            _card = new Border();
            _card.CornerRadius = new CornerRadius(24);
            _card.Background = Ui.BA("#F2121D31", 248);
            _card.BorderBrush = Ui.BA("#5B8FC6", 100);
            _card.BorderThickness = new Thickness(1);
            _card.Effect = Ui.Shadow(32, 0.48, 9);
            shell.Children.Add(_card);
            var cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(78) });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _card.Child = cardGrid;

            // header
            var headerBg = new Border();
            headerBg.CornerRadius = new CornerRadius(24, 24, 0, 0);
            headerBg.Background = Ui.Linear("#1B3154", "#15233C", 90);
            Grid.SetRow(headerBg, 0);
            cardGrid.Children.Add(headerBg);
            var header = new Grid();
            header.Margin = new Thickness(16, 12, 12, 9);
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(header, 0);
            cardGrid.Children.Add(header);

            var avatar = BuildAvatar(46);
            avatar.Margin = new Thickness(0, 0, 11, 0);
            avatar.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(avatar, 0);
            header.Children.Add(avatar);

            var dragArea = new StackPanel();
            dragArea.VerticalAlignment = VerticalAlignment.Center;
            dragArea.Background = Brushes.Transparent;
            dragArea.Cursor = Cursors.SizeAll;
            dragArea.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                try { DragMove(); } catch { }
            };
            var title = Ui.T("蓝色大肥鱼", 16, "#F2FAFF", true);
            dragArea.Children.Add(title);
            var subtitleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            _statusDot = Ui.Dot("#7CE3A5", 6);
            _statusDot.VerticalAlignment = VerticalAlignment.Center;
            subtitleRow.Children.Add(_statusDot);
            _status = Ui.T(app.HasApiKey ? "已连接 DeepSeek" : "离线试用 · 还没接上大脑", 10, "#93A9C9", false);
            _status.Margin = new Thickness(5, 0, 0, 0);
            subtitleRow.Children.Add(_status);
            dragArea.Children.Add(subtitleRow);
            Grid.SetColumn(dragArea, 1);
            header.Children.Add(dragArea);

            var headerButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var settingsButton = SmallHeaderButton("设置");
            settingsButton.Margin = new Thickness(0, 0, 8, 0);
            settingsButton.MouseLeftButtonUp += delegate { _app.ShowSettings(); };
            headerButtons.Children.Add(settingsButton);
            var closeButton = SmallHeaderButton("×");
            closeButton.Width = 30;
            closeButton.Padding = new Thickness(0);
            closeButton.MouseLeftButtonUp += delegate { Hide(); };
            headerButtons.Children.Add(closeButton);
            Grid.SetColumn(headerButtons, 2);
            header.Children.Add(headerButtons);

            // messages
            _scroll = new ScrollViewer();
            _scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _scroll.Background = Brushes.Transparent;
            _scroll.Padding = new Thickness(0);
            _scroll.ScrollChanged += delegate(object s, ScrollChangedEventArgs e)
            {
                if (e.ExtentHeightChange == 0 && e.ViewportHeightChange == 0)
                    _stickToBottom = e.VerticalOffset >= e.ExtentHeight - e.ViewportHeight - 30;
            };
            _messages = new StackPanel();
            _messages.Margin = new Thickness(16, 10, 16, 12);
            _scroll.Content = _messages;
            Grid.SetRow(_scroll, 1);
            cardGrid.Children.Add(_scroll);

            // bottom controls
            var bottom = new StackPanel();
            bottom.Margin = new Thickness(14, 0, 14, 10);
            Grid.SetRow(bottom, 2);
            cardGrid.Children.Add(bottom);

            _setupBanner = new Border();
            _setupBanner.CornerRadius = new CornerRadius(11);
            _setupBanner.Background = Ui.BA("#3B2C17", 210);
            _setupBanner.BorderBrush = Ui.BA("#E8B75C", 110);
            _setupBanner.BorderThickness = new Thickness(1);
            _setupBanner.Padding = new Thickness(11, 7, 9, 7);
            _setupBanner.Margin = new Thickness(0, 0, 0, 8);
            _setupBanner.Cursor = Cursors.Hand;
            _setupBanner.MouseLeftButtonUp += delegate { _app.ShowSettings(); };
            var bannerGrid = new Grid();
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var bannerText = Ui.T("还没接上 DeepSeek 大脑 · 点这里填 API Key", 11.2, "#FFD998", false);
            bannerText.VerticalAlignment = VerticalAlignment.Center;
            bannerGrid.Children.Add(bannerText);
            var bannerArrow = Ui.T("去设置 →", 10.5, "#FFE4AF", true);
            bannerArrow.VerticalAlignment = VerticalAlignment.Center;
            bannerArrow.Margin = new Thickness(8, 0, 2, 0);
            Grid.SetColumn(bannerArrow, 1);
            bannerGrid.Children.Add(bannerArrow);
            _setupBanner.Child = bannerGrid;
            _setupBanner.Visibility = app.HasApiKey ? Visibility.Collapsed : Visibility.Visible;
            bottom.Children.Add(_setupBanner);

            var inputOuter = new Border();
            inputOuter.CornerRadius = new CornerRadius(17);
            inputOuter.Background = Ui.BA("#0A1426", 245);
            inputOuter.BorderBrush = Ui.BA("#4B7199", 105);
            inputOuter.BorderThickness = new Thickness(1);
            inputOuter.Padding = new Thickness(12, 8, 8, 8);
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _input = new TextBox();
            _input.Background = Brushes.Transparent;
            _input.BorderThickness = new Thickness(0);
            _input.Foreground = Ui.B("#EAF4FF");
            _input.CaretBrush = Ui.B("#7FE7FF");
            _input.FontFamily = Ui.Font;
            _input.FontSize = 13;
            _input.Padding = new Thickness(0, 3, 8, 3);
            _input.AcceptsReturn = true;
            _input.TextWrapping = TextWrapping.Wrap;
            _input.MinHeight = 40;
            _input.MaxHeight = 84;
            _input.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _input.PreviewKeyDown += OnInputKeyDown;
            _input.TextChanged += delegate
            {
                _placeholder.Visibility = string.IsNullOrEmpty(_input.Text) ? Visibility.Visible : Visibility.Collapsed;
                UpdateSendState();
            };
            _input.GotFocus += delegate { inputOuter.BorderBrush = Ui.BA("#63D2FF", 190); };
            _input.LostFocus += delegate { inputOuter.BorderBrush = Ui.BA("#4B7199", 105); };
            inputGrid.Children.Add(_input);
            _placeholder = Ui.T("和鱼鱼说点什么…  Enter 发送 · Shift+Enter 换行", 12, "#657E9F", false);
            _placeholder.Margin = new Thickness(1, 4, 8, 0);
            _placeholder.IsHitTestVisible = false;
            _placeholder.VerticalAlignment = VerticalAlignment.Top;
            _placeholder.TextWrapping = TextWrapping.Wrap;
            inputGrid.Children.Add(_placeholder);

            _send = new Border();
            _send.Width = 38;
            _send.Height = 38;
            _send.CornerRadius = new CornerRadius(13);
            _send.Background = Ui.Linear("#9DF1FF", "#3A7EF0", 90);
            _send.VerticalAlignment = VerticalAlignment.Bottom;
            _send.Cursor = Cursors.Hand;
            _send.MouseLeftButtonUp += delegate { OnSendClick(); };
            _send.MouseEnter += delegate { _send.Effect = Ui.Glow(Ui.C("#79DFFF"), 16, 0.5); };
            _send.MouseLeave += delegate { _send.Effect = null; };
            var sendGrid = new Grid();
            _sendIcon = Ui.P("M 0,10 L 17,10 M 11,4 L 17,10 L 11,16", Brushes.Transparent);
            _sendIcon.Stroke = Ui.B("#07162C");
            _sendIcon.StrokeThickness = 2.2;
            _sendIcon.HorizontalAlignment = HorizontalAlignment.Center;
            _sendIcon.VerticalAlignment = VerticalAlignment.Center;
            sendGrid.Children.Add(_sendIcon);
            _stopIcon = new Rectangle();
            _stopIcon.Width = 11;
            _stopIcon.Height = 11;
            _stopIcon.RadiusX = 2;
            _stopIcon.RadiusY = 2;
            _stopIcon.Fill = Ui.B("#07162C");
            _stopIcon.HorizontalAlignment = HorizontalAlignment.Center;
            _stopIcon.VerticalAlignment = VerticalAlignment.Center;
            _stopIcon.Visibility = Visibility.Collapsed;
            sendGrid.Children.Add(_stopIcon);
            _send.Child = sendGrid;
            Grid.SetColumn(_send, 1);
            inputGrid.Children.Add(_send);
            inputOuter.Child = inputGrid;
            bottom.Children.Add(inputOuter);

            var footer = Ui.T("思考过程会变成鱼鱼的小动作 · 对话只保存在这台电脑", 9.3, "#5F7897", false);
            footer.HorizontalAlignment = HorizontalAlignment.Center;
            footer.Margin = new Thickness(0, 5, 0, 0);
            bottom.Children.Add(footer);

            _thinkCard = BuildThinkCard();
            var thinkOuter = (Grid)_thinkCard.Child;
            var thinkContent = (StackPanel)thinkOuter.Children[1];
            var thinkHead = (StackPanel)thinkContent.Children[0];
            _thinkDots = (StackPanel)thinkHead.Children[1];
            _thinkStage = (TextBlock)thinkContent.Children[1];
            _thinkExcerpt = (TextBlock)thinkContent.Children[2];

            _renderTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _renderTimer.Interval = TimeSpan.FromMilliseconds(60);
            _renderTimer.Tick += delegate
            {
                _renderTimer.Stop();
                if (_streamBubble != null) _streamBubble.Update(_streamText);
                if (_stickToBottom) _scroll.ScrollToEnd();
            };

            _thinkTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _thinkTimer.Interval = TimeSpan.FromMilliseconds(420);
            _thinkTimer.Tick += delegate
            {
                _thinkStep++;
                for (int i = 0; i < _thinkDots.Children.Count; i++)
                {
                    _thinkDots.Children[i].Opacity = ((_thinkStep + i) % 3 == 0) ? 1.0 : 0.25;
                }
            };

            Content = shell;
            Loaded += delegate { if (!_placed) PlaceBesidePet(); };
            LocationChanged += delegate { if (_placed) _app.SaveChatPosition(Left, Top); };
            KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) Hide(); };
            BuildInitialMessages();
            UpdateSendState();
        }

        private Border BuildAvatar(double size)
        {
            var b = new Border();
            b.Width = size; b.Height = size;
            b.CornerRadius = new CornerRadius(size * 0.32);
            b.Background = Ui.Linear("#2C62C8", "#79C9FF", 120);
            b.Effect = Ui.Glow(Ui.C("#5CB8FF"), 12, 0.35);
            var c = new Canvas();
            c.Width = size; c.Height = size;
            var whale = Ui.P("M 9,25 C 9,18 16,14 25,16 C 35,18 38,25 33,30 C 28,35 14,34 10,30 Z", Ui.BA("#F4FCFF", 235));
            c.Children.Add(whale);
            var tail = Ui.P("M 11,26 L 3,21 L 6,31 Z", Ui.BA("#F4FCFF", 235));
            c.Children.Add(tail);
            var eye = new Ellipse { Width = 4, Height = 4, Fill = Ui.B("#1A3C7A") };
            Ui.SetPos(eye, 15, 21);
            c.Children.Add(eye);
            var spout = Ui.Stroke("M 24,14 C 24,9 29,9 29,4", Ui.BA("#D9F8FF", 220), 1.8);
            c.Children.Add(spout);
            b.Child = c;
            return b;
        }

        private Border SmallHeaderButton(string text)
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(9);
            b.Background = Ui.BA("#263E60", 200);
            b.BorderBrush = Ui.BA("#7EB4E0", 55);
            b.BorderThickness = new Thickness(1);
            b.Padding = new Thickness(9, 4, 9, 4);
            b.Cursor = Cursors.Hand;
            b.MouseEnter += delegate { b.Background = Ui.BA("#345579", 230); };
            b.MouseLeave += delegate { b.Background = Ui.BA("#263E60", 200); };
            var t = Ui.T(text, text == "×" ? 16 : 10.5, "#D6E9FF", false);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            return b;
        }

        private Border BuildThinkCard()
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(15);
            b.Background = Ui.BA("#14263F", 240);
            b.BorderBrush = Ui.BA("#64D5FF", 80);
            b.BorderThickness = new Thickness(1);
            b.Padding = new Thickness(12, 9, 12, 10);
            b.Margin = new Thickness(0, 4, 38, 8);
            b.HorizontalAlignment = HorizontalAlignment.Left;

            var outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var avatar = BuildAvatar(24);
            avatar.VerticalAlignment = VerticalAlignment.Top;
            avatar.Margin = new Thickness(0, 1, 4, 0);
            outer.Children.Add(avatar);

            var content = new StackPanel();
            var headRow = new StackPanel { Orientation = Orientation.Horizontal };
            var label = Ui.T("鱼鱼正在想", 10.5, "#8FE9FF", true);
            headRow.Children.Add(label);
            var dots = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < 3; i++)
            {
                var d = new Ellipse { Width = 4, Height = 4, Fill = Ui.B("#8FE9FF"), Opacity = 0.3 };
                d.Margin = new Thickness(i == 0 ? 0 : 3, 0, 0, 0);
                dots.Children.Add(d);
            }
            headRow.Children.Add(dots);
            content.Children.Add(headRow);
            var stage = Ui.T("把问题放进思考锅…", 12, "#D6E7FA", false);
            stage.Margin = new Thickness(0, 4, 0, 2);
            content.Children.Add(stage);
            var excerpt = Ui.T("", 10.8, "#8FA8C6", false);
            excerpt.LineHeight = 16;
            excerpt.MaxHeight = 68;
            excerpt.TextTrimming = TextTrimming.CharacterEllipsis;
            excerpt.Visibility = Visibility.Collapsed;
            content.Children.Add(excerpt);

            Grid.SetColumn(content, 1);
            outer.Children.Add(content);
            b.Child = outer;
            return b;
        }

        private void BuildInitialMessages()
        {
            var divider = BuildDivider("今天");
            _messages.Children.Add(divider);
            var history = _app.History;
            if (history.Count == 0)
            {
                string hello = "我是鱼鱼，DeepSeek 的蓝色大肥鱼。可以陪你写代码、拆问题、吐槽今天，也可以只是安静待着。\n\n";
                if (_app.HasApiKey) hello += "想聊什么？点下面的小纸片，或者直接说。";
                else hello += "不过我现在还没接上大脑……先去设置里填一个 API Key，我就能真的陪你聊了。";
                AddPetMessage(hello, false);
                AddSuggestions();
            }
            else
            {
                foreach (var m in history)
                {
                    if (m.Role == "user") AddUserMessage(m.Content, false);
                    else if (m.Role == "assistant") AddPetMessage(m.Content, false);
                }
            }
        }

        private Border BuildDivider(string text)
        {
            var b = new Border();
            b.Margin = new Thickness(0, 3, 0, 9);
            b.Background = Ui.BA("#3A5C82", 55);
            b.CornerRadius = new CornerRadius(8);
            b.Padding = new Thickness(8, 2, 8, 2);
            b.HorizontalAlignment = HorizontalAlignment.Center;
            var t = Ui.T(text, 9.5, "#7C96B4", false);
            b.Child = t;
            return b;
        }

        private void AddSuggestions()
        {
            if (_suggestionRow > 0) return;
            _suggestionRow++;
            var wrap = new WrapPanel();
            wrap.Margin = new Thickness(0, 2, 0, 8);
            _suggestions = wrap;
            string[] texts = new string[] { "陪我写代码", "今天吃什么", "讲个冷笑话" };
            foreach (var text in texts)
            {
                var chip = new Border();
                chip.CornerRadius = new CornerRadius(11);
                chip.Background = Ui.BA("#223A58", 220);
                chip.BorderBrush = Ui.BA("#6EB9EA", 55);
                chip.BorderThickness = new Thickness(1);
                chip.Padding = new Thickness(9, 4, 9, 5);
                chip.Margin = new Thickness(0, 0, 6, 6);
                chip.Cursor = Cursors.Hand;
                var t = Ui.T(text, 10.6, "#CBE4FA", false);
                chip.Child = t;
                string captured = text;
                chip.MouseLeftButtonUp += delegate { _input.Text = captured; _input.CaretIndex = captured.Length; SendCurrent(); };
                chip.MouseEnter += delegate { chip.Background = Ui.BA("#315477", 240); };
                chip.MouseLeave += delegate { chip.Background = Ui.BA("#223A58", 220); };
                wrap.Children.Add(chip);
            }
            _messages.Children.Add(wrap);
        }

        private ChatBubble AddBubble(string text, bool isUser, bool animate)
        {
            var row = new Grid();
            row.Margin = new Thickness(0, 0, 0, 9);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var bubble = new Border();
            bubble.CornerRadius = new CornerRadius(15);
            bubble.Padding = new Thickness(12, 8, 12, 9);
            bubble.MaxWidth = 305;
            if (isUser)
            {
                bubble.Background = Ui.Linear("#3D86F3", "#2B64CE", 90);
                bubble.BorderBrush = Ui.BA("#9ED2FF", 70);
                bubble.BorderThickness = new Thickness(1);
                bubble.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetColumn(bubble, 1);
            }
            else
            {
                bubble.Background = Ui.BA("#1A2D46", 244);
                bubble.BorderBrush = Ui.BA("#4E789E", 65);
                bubble.BorderThickness = new Thickness(1);
                bubble.HorizontalAlignment = HorizontalAlignment.Left;
                var avatar = BuildAvatar(25);
                avatar.VerticalAlignment = VerticalAlignment.Top;
                avatar.Margin = new Thickness(0, 2, 7, 0);
                row.Children.Add(avatar);
                Grid.SetColumn(bubble, 1);
            }
            var stack = new StackPanel();
            var meta = Ui.T(isUser ? "你" : "鱼鱼", 9.4, isUser ? "#DCEBFF" : "#86A8CA", true);
            meta.Margin = new Thickness(0, 0, 0, 3);
            stack.Children.Add(meta);
            var content = new StackPanel();
            stack.Children.Add(content);
            bubble.Child = stack;
            row.Children.Add(bubble);
            _messages.Children.Add(row);

            var cb = new ChatBubble();
            cb.Row = row;
            cb.Bubble = bubble;
            cb.Content = content;
            cb.Meta = meta;
            Markdown.Render(text, content);
            if (animate)
            {
                bubble.Opacity = 0;
                var tt = new TranslateTransform(0, 5);
                bubble.RenderTransform = tt;
                bubble.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(5, 0, TimeSpan.FromMilliseconds(180)));
            }
            if (animate) ScrollBottom();
            return cb;
        }

        public ChatBubble AddPetMessage(string text, bool animate)
        {
            return AddBubble(text, false, animate);
        }

        public void AddUserMessage(string text, bool animate)
        {
            RemoveSuggestions();
            AddBubble(text, true, animate);
            ScrollBottom();
        }

        private void RemoveSuggestions()
        {
            if (_suggestions == null) return;
            if (_suggestions.Parent is Panel) ((Panel)_suggestions.Parent).Children.Remove(_suggestions);
            _suggestions = null;
        }

        public ChatBubble BeginPetResponse()
        {
            HideThinking();
            _streamText = "";
            _streamBubble = AddBubble("", false, true);
            _streamBubble.Meta.Text = "鱼鱼 · 正在回答";
            return _streamBubble;
        }

        public void AppendPetChunk(string text)
        {
            if (_streamBubble == null) BeginPetResponse();
            _streamText += text;
            if (!_renderTimer.IsEnabled) _renderTimer.Start();
        }

        public void CompletePetResponse()
        {
            if (_renderTimer.IsEnabled)
            {
                _renderTimer.Stop();
                if (_streamBubble != null) _streamBubble.Update(_streamText);
            }
            if (_streamBubble != null)
            {
                _streamBubble.Meta.Text = "鱼鱼 · " + DateTime.Now.ToString("HH:mm");
                _streamBubble = null;
            }
            if (_stickToBottom) _scroll.ScrollToEnd();
        }

        public void CancelPetResponse()
        {
            if (_renderTimer.IsEnabled) _renderTimer.Stop();
            if (_streamBubble != null)
            {
                if (string.IsNullOrEmpty(_streamText))
                {
                    _streamBubble.Update("（鱼鱼把话咽回去了，尾巴一甩。）");
                }
                else
                {
                    _streamBubble.Update(_streamText + "\n\n（停在这里了。）");
                }
                _streamBubble.Meta.Text = "鱼鱼 · 已停下";
                _streamBubble = null;
            }
            HideThinking();
        }

        public void ShowThinking(string stage, string excerpt)
        {
            _thinkStage.Text = stage;
            if (string.IsNullOrWhiteSpace(excerpt))
            {
                _thinkExcerpt.Visibility = Visibility.Collapsed;
            }
            else
            {
                _thinkExcerpt.Visibility = Visibility.Visible;
                string body = excerpt.Trim();
                if (body.Length > 420) body = body.Substring(0, 420) + "…";
                _thinkExcerpt.Text = body;
            }
            if (_thinkCard.Parent == null)
            {
                _messages.Children.Add(_thinkCard);
                _thinkTimer.Start();
                ScrollBottom();
            }
        }

        public void UpdateThinking(string stage, string excerpt)
        {
            if (_thinkCard.Parent == null) ShowThinking(stage, excerpt);
            else
            {
                _thinkStage.Text = stage;
                if (!string.IsNullOrWhiteSpace(excerpt))
                {
                    _thinkExcerpt.Visibility = Visibility.Visible;
                    string body = excerpt.Trim();
                    if (body.Length > 420) body = body.Substring(0, 420) + "…";
                    _thinkExcerpt.Text = body;
                }
                if (_stickToBottom) _scroll.ScrollToEnd();
            }
        }

        public void HideThinking()
        {
            _thinkTimer.Stop();
            if (_thinkCard.Parent is Panel) ((Panel)_thinkCard.Parent).Children.Remove(_thinkCard);
        }

        public void AddPetNotice(string text)
        {
            var t = Ui.T(text, 10.2, "#7893B2", false);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.TextAlignment = TextAlignment.Center;
            t.Margin = new Thickness(20, 1, 20, 8);
            _messages.Children.Add(t);
            ScrollBottom();
        }

        public void AddErrorMessage(string text)
        {
            HideThinking();
            var bubble = AddBubble("呜……" + text, false, true);
            bubble.Meta.Text = "鱼鱼 · 遇到一点小问题";
            var retry = Ui.GhostButton("再试一次", 27);
            retry.Width = 92;
            retry.HorizontalAlignment = HorizontalAlignment.Left;
            retry.Margin = new Thickness(0, 7, 0, 0);
            ((PillButton)retry).Clicked += delegate { _app.RetryLast(); };
            bubble.Content.Children.Add(retry);
            ScrollBottom();
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;
            _sendIcon.Visibility = busy ? Visibility.Collapsed : Visibility.Visible;
            _stopIcon.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            _status.Text = busy ? "正在想 · 点方块可以打断" : (_app.HasApiKey ? "已连接 DeepSeek" : "离线试用 · 还没接上大脑");
            _statusDot.Fill = Ui.B(busy ? "#FFD36E" : (_app.HasApiKey ? "#7CE3A5" : "#FFC46B"));
            UpdateSendState();
        }

        public void RefreshConnection()
        {
            _setupBanner.Visibility = _app.HasApiKey ? Visibility.Collapsed : Visibility.Visible;
            if (!_busy)
            {
                _status.Text = _app.HasApiKey ? "已连接 DeepSeek" : "离线试用 · 还没接上大脑";
                _statusDot.Fill = Ui.B(_app.HasApiKey ? "#7CE3A5" : "#FFC46B");
            }
        }

        private void UpdateSendState()
        {
            if (_busy)
            {
                _send.Opacity = 1;
                _send.Cursor = Cursors.Hand;
                return;
            }
            bool has = !string.IsNullOrWhiteSpace(_input != null ? _input.Text : "");
            _send.Opacity = has ? 1 : 0.5;
            _send.Cursor = has ? Cursors.Hand : Cursors.Arrow;
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                if (_busy) return;
                SendCurrent();
            }
        }

        private void OnSendClick()
        {
            if (_busy) _app.CancelRequest();
            else SendCurrent();
        }

        private void SendCurrent()
        {
            if (_busy) return;
            string text = _input.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (text.Length > 6000) text = text.Substring(0, 6000);
            _input.Clear();
            _placeholder.Visibility = Visibility.Visible;
            UpdateSendState();
            _app.SendMessage(text);
        }

        private void ScrollBottom()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _scroll.ScrollToEnd();
                _stickToBottom = true;
            }), DispatcherPriority.Background);
        }

        public void FocusInput()
        {
            Dispatcher.BeginInvoke(new Action(delegate { _input.Focus(); }), DispatcherPriority.Input);
        }

        public void PlaceBesidePet()
        {
            _placed = true;
            var wa = SystemParameters.WorkArea;
            double left = _app.Config.ChatLeft;
            double top = _app.Config.ChatTop;
            bool valid = !double.IsNaN(left) && !double.IsNaN(top) &&
                         left >= wa.Left - 40 && left + Width <= wa.Right + 40 &&
                         top >= wa.Top - 40 && top + Height <= wa.Bottom + 40;
            if (!valid)
            {
                var main = _app.MainWindow;
                if (main != null)
                {
                    left = main.Left - Width - 8;
                    if (left < wa.Left + 6) left = main.Left + main.Width - 10;
                    top = main.Top + main.Height - Height;
                }
                else
                {
                    left = wa.Right - Width - 24;
                    top = wa.Bottom - Height - 24;
                }
            }
            if (left + Width > wa.Right - 4) left = wa.Right - Width - 4;
            if (left < wa.Left + 4) left = wa.Left + 4;
            if (top + Height > wa.Bottom - 4) top = wa.Bottom - Height - 4;
            if (top < wa.Top + 4) top = wa.Top + 4;
            Left = left;
            Top = top;
            _app.SaveChatPosition(Left, Top);
        }

        public void ScrollToBottom()
        {
            ScrollBottom();
        }
    }

    public class ChatBubble
    {
        public Grid Row;
        public Border Bubble;
        public StackPanel Content;
        public TextBlock Meta;

        public void Update(string text)
        {
            Markdown.Render(text, Content);
        }
    }
}

