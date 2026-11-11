using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BlueFishPet
{
    public class MainWindow : Window
    {
        private readonly PetApp _app;
        private readonly PetView _pet;
        private readonly Border _bubble;
        private readonly TextBlock _bubbleLabel;
        private readonly TextBlock _bubbleText;
        private readonly StackPanel _thinkDots;
        private readonly Border _status;
        private readonly Ellipse _statusDot;
        private readonly TextBlock _statusText;
        private readonly PillButton _feedButton;
        private readonly DispatcherTimer _bubbleTimer;
        private readonly DispatcherTimer _idleTimer;
        private readonly DispatcherTimer _thinkTimer;
        private readonly Random _rng = new Random();
        private const double BubbleOffsetY = 168;

        private bool _dragging;
        private bool _dragMoved;
        private bool _dragLineShown;
        private Point _dragStart;
        private double _startLeft;
        private double _startTop;
        private DateTime _lastInteraction = DateTime.Now;
        private bool _thinking;
        private bool _speaking;
        private int _thinkStep;
        private bool _allowClose;
        private readonly DispatcherTimer _wanderTimer;
        private readonly DispatcherTimer _walkTimer;
        private readonly DispatcherTimer _supportTimer;
        private bool _walking;
        private int _walkDirection;
        private double _walkTargetLeft;
        private DateTime _lastWanderEnd = DateTime.Now;

        private IntPtr _hwnd;
        private bool _flying;
        private bool _grounded;
        private double _vx;
        private double _vy;
        private PetPlatform _groundPlatform;
        private readonly List<PetPlatform> _platforms = new List<PetPlatform>();
        private DateTime _lastPlatformRefresh = DateTime.MinValue;
        private DateTime _lastMotionTick = DateTime.Now;
        private double _dragVX;
        private double _dragVY;
        private Point _dragLastPos;
        private DateTime _dragLastTime;
        private const double FeetOffset = 491;
        private const double Gravity = 1250;
        private const double WalkSpeed = 92;

        private sealed class PetPlatform
        {
            public double Top;
            public double Left;
            public double Right;
            public bool IsBottom;
            public IntPtr Hwnd;
        }

        public PetView Pet { get { return _pet; } }

        public MainWindow(PetApp app)
        {
            _app = app;
            Width = 390;
            Height = 590;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = app.Config.AlwaysOnTop;
            ShowActivated = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Title = "蓝色大肥鱼";

            var root = new Grid();
            root.Background = null;
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(142) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(400) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });

            _pet = new PetView();
            _pet.HorizontalAlignment = HorizontalAlignment.Center;
            _pet.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(_pet, 1);
            root.Children.Add(_pet);

            _bubble = new Border();
            _bubble.CornerRadius = new CornerRadius(19);
            _bubble.Background = Ui.BA("#0D1930", 238);
            _bubble.BorderBrush = Ui.BA("#65D4FF", 80);
            _bubble.BorderThickness = new Thickness(1);
            _bubble.Padding = new Thickness(14, 11, 14, 12);
            _bubble.Margin = new Thickness(16, 0, 16, 8);
            _bubble.MaxWidth = 336;
            _bubble.HorizontalAlignment = HorizontalAlignment.Center;
            _bubble.VerticalAlignment = VerticalAlignment.Bottom;
            _bubble.Effect = Ui.Shadow(18, 0.35, 5);
            _bubble.Visibility = Visibility.Collapsed;
            // The pet is small now; pull the speech bubble down close to its head.
            _bubble.RenderTransform = new TranslateTransform(0, BubbleOffsetY);
            _bubble.Cursor = Cursors.Hand;
            _bubble.MouseLeftButtonUp += delegate { OpenChatFromBubble(); };
            Grid.SetRow(_bubble, 0);
            root.Children.Add(_bubble);

            var bubbleStack = new StackPanel();
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(Ui.Dot("#7FE7FF", 6));
            _bubbleLabel = Ui.T("鱼鱼说", 10.5, "#8FE9FF", true);
            _bubbleLabel.Margin = new Thickness(7, 0, 0, 0);
            header.Children.Add(_bubbleLabel);
            _thinkDots = BuildThinkDots();
            _thinkDots.Margin = new Thickness(8, 0, 0, 0);
            _thinkDots.Visibility = Visibility.Collapsed;
            header.Children.Add(_thinkDots);
            bubbleStack.Children.Add(header);

            _bubbleText = Ui.T("", 12.6, "#EAF4FF", false);
            _bubbleText.LineHeight = 19;
            _bubbleText.Margin = new Thickness(0, 5, 0, 0);
            _bubbleText.MaxWidth = 305;
            bubbleStack.Children.Add(_bubbleText);
            _bubble.Child = bubbleStack;

            _status = new Border();
            _status.CornerRadius = new CornerRadius(11);
            _status.Background = Ui.BA("#0C172A", 190);
            _status.BorderBrush = Ui.BA("#6FA9D8", 55);
            _status.BorderThickness = new Thickness(1);
            _status.Padding = new Thickness(10, 3, 10, 3);
            _status.HorizontalAlignment = HorizontalAlignment.Left;
            _status.VerticalAlignment = VerticalAlignment.Top;
            _status.Cursor = Cursors.Hand;
            _status.MouseLeftButtonUp += delegate { OnStatusClick(); };
            var statusStack = new StackPanel { Orientation = Orientation.Horizontal };
            _statusDot = Ui.Dot("#7CE3A5", 6);
            _statusDot.VerticalAlignment = VerticalAlignment.Center;
            statusStack.Children.Add(_statusDot);
            _statusText = Ui.T("正在连接…", 10.2, "#AFC7E1", false);
            _statusText.Margin = new Thickness(6, 0, 0, 0);
            statusStack.Children.Add(_statusText);
            _status.Child = statusStack;
            _status.Margin = new Thickness(8, 278, 0, 0);
            Grid.SetRow(_status, 1);
            root.Children.Add(_status);

            _feedButton = new PillButton("喂白饭", false);
            _feedButton.Width = 80;
            _feedButton.Height = 26;
            _feedButton.Padding = new Thickness(10, 0, 10, 0);
            _feedButton.HorizontalAlignment = HorizontalAlignment.Right;
            _feedButton.VerticalAlignment = VerticalAlignment.Top;
            _feedButton.Margin = new Thickness(0, 278, 8, 0);
            _feedButton.ToolTip = "喂一碗白饭";
            _feedButton.Clicked += delegate { Feed(); };
            Grid.SetRow(_feedButton, 1);
            root.Children.Add(_feedButton);

            Content = root;

            _bubbleTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _bubbleTimer.Tick += delegate { HideBubble(); };

            _idleTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _idleTimer.Interval = TimeSpan.FromSeconds(5);
            _idleTimer.Tick += delegate { OnIdleTick(); };
            _idleTimer.Start();

            _wanderTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _wanderTimer.Interval = TimeSpan.FromMilliseconds(900);
            _wanderTimer.Tick += delegate { OnWanderTick(); };
            _wanderTimer.Start();

            _walkTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _walkTimer.Interval = TimeSpan.FromMilliseconds(16);
            _walkTimer.Tick += delegate { OnWalkTick(); };

            _supportTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _supportTimer.Interval = TimeSpan.FromMilliseconds(300);
            _supportTimer.Tick += delegate { CheckSupport(); };
            _supportTimer.Start();

            _thinkTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _thinkTimer.Interval = TimeSpan.FromMilliseconds(520);
            _thinkTimer.Tick += delegate
            {
                _thinkStep++;
                for (int i = 0; i < _thinkDots.Children.Count; i++)
                {
                    var el = _thinkDots.Children[i] as UIElement;
                    if (el != null) el.Opacity = ((_thinkStep + i) % 3 == 0) ? 1.0 : 0.28;
                }
            };

            _pet.MouseLeftButtonDown += OnPetMouseDown;
            _pet.MouseMove += OnPetMouseMove;
            _pet.MouseLeftButtonUp += OnPetMouseUp;
            _pet.MouseRightButtonUp += OnPetRightClick;
            MouseRightButtonUp += OnWindowRightClick;

            Loaded += delegate
            {
                PlaceInitial();
                RefreshPlatformsIfNeeded(true);
                BeginFall();
                SetStatus("DeepSeek · 未配置", "#FFC46B");
                if (_app.Config.FirstRun)
                {
                    ShowBubble("嗨，我是蓝色大肥鱼。先点右下角「设置」，把你的 DeepSeek API Key 交给我吧。", 0);
                }
                else if (_app.SavedKeyInvalid)
                {
                    SetStatus("DeepSeek · Key 格式不对", "#FF8E8E");
                    ShowBubble("你保存的 Key 像一段中文。这里要填 DeepSeek 的 sk-... Key，不是问题描述。", 0);
                }
                else if (_app.HasApiKey)
                {
                    SetStatus("DeepSeek · 已就绪", "#7CE3A5");
                }
            };
            SourceInitialized += OnSourceInitialized;
            LocationChanged += delegate { _app.SaveWindowPosition(Left, Top); };
            Deactivated += delegate { if (!_app.ChatVisible && !_walking) _pet.SetMood(_thinking ? PetMood.Thinking : PetMood.Idle); };
        }

        private StackPanel BuildThinkDots()
        {
            var p = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse();
                dot.Width = 4; dot.Height = 4;
                dot.Fill = Ui.B("#7FE7FF");
                dot.Margin = new Thickness(i == 0 ? 0 : 3, 0, 0, 0);
                dot.Opacity = 0.3;
                p.Children.Add(dot);
            }
            return p;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            HwndSource.FromHwnd(hwnd).AddHook(WndProc);
            _hwnd = hwnd;
            Native.MakeToolWindow(hwnd);
            Native.RegisterHotKey(hwnd, 0xB001, Native.MOD_CONTROL | Native.MOD_ALT, Native.VK_F);
            _app.AttachMainHandle(hwnd);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_NCHITTEST)
            {
                try
                {
                    int x = (short)(lParam.ToInt64() & 0xFFFF);
                    int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
                    if (IsInteractive(new Point(x, y)))
                    {
                        handled = true;
                        return new IntPtr(Native.HTCLIENT);
                    }
                    handled = true;
                    return new IntPtr(Native.HTTRANSPARENT);
                }
                catch { }
            }
            else if (msg == Native.WM_HOTKEY && wParam.ToInt32() == 0xB001)
            {
                _app.TogglePetVisible();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private bool IsInteractive(Point screen)
        {
            if (_bubble.Visibility == Visibility.Visible && _bubble.IsVisible)
            {
                try
                {
                    Point local = _bubble.PointFromScreen(screen);
                    if (local.X >= -5 && local.X <= _bubble.ActualWidth + 5 &&
                        local.Y >= -5 && local.Y <= _bubble.ActualHeight + 5) return true;
                }
                catch { }
            }
            if (_status.IsVisible)
            {
                try
                {
                    Point local = _status.PointFromScreen(screen);
                    if (local.X >= -4 && local.X <= _status.ActualWidth + 4 &&
                        local.Y >= -4 && local.Y <= _status.ActualHeight + 4) return true;
                }
                catch { }
            }
            if (_feedButton != null && _feedButton.IsVisible)
            {
                try
                {
                    Point local = _feedButton.PointFromScreen(screen);
                    if (local.X >= -4 && local.X <= _feedButton.ActualWidth + 4 &&
                        local.Y >= -4 && local.Y <= _feedButton.ActualHeight + 4) return true;
                }
                catch { }
            }
            try
            {
                Point petPoint = _pet.PointFromScreen(screen);
                if (_pet.HitTestPet(petPoint)) return true;
            }
            catch { }
            return false;
        }

        private Point CursorInDip()
        {
            var p = Native.CursorPosition();
            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                    return source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y));
            }
            catch { }
            return new Point(p.X, p.Y);
        }

        private void PlaceInitial()
        {
            var wa = SystemParameters.WorkArea;
            double left = _app.Config.WindowLeft;
            double top = _app.Config.WindowTop;
            bool valid = !double.IsNaN(left) && !double.IsNaN(top) &&
                         left > wa.Left - Width + 80 && left < wa.Right - 80 &&
                         top > wa.Top - 80 && top < wa.Bottom - 80;
            if (!valid)
            {
                left = wa.Right - Width - 26;
                top = wa.Bottom - Height - 8;
            }
            Left = left;
            Top = top;
            _app.SaveWindowPosition(Left, Top);
        }

        public void SetStatus(string text, string dotColor)
        {
            string shortText = text ?? "";
            if (shortText.StartsWith("DeepSeek · ")) shortText = shortText.Substring(11);
            _statusText.Text = shortText;
            _status.ToolTip = text;
            _statusDot.Fill = Ui.B(dotColor);
        }

        public void ShowBubble(string text, double seconds)
        {
            ShowBubble(text, seconds, false);
        }

        public void ShowBubble(string text, double seconds, bool thinking)
        {
            _thinking = thinking;
            _bubbleLabel.Text = thinking ? "鱼鱼在想" : "鱼鱼说";
            _bubbleLabel.Foreground = Ui.B(thinking ? "#FFD98A" : "#8FE9FF");
            _bubbleText.Text = text;
            _thinkDots.Visibility = thinking ? Visibility.Visible : Visibility.Collapsed;
            if (thinking) _thinkTimer.Start(); else _thinkTimer.Stop();
            _bubble.Visibility = Visibility.Visible;
            _bubble.Opacity = 0;
            var tt = new TranslateTransform(0, BubbleOffsetY + 8);
            _bubble.RenderTransform = tt;
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(190));
            var rise = new DoubleAnimation(BubbleOffsetY + 8, BubbleOffsetY, TimeSpan.FromMilliseconds(220));
            rise.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            _bubble.BeginAnimation(OpacityProperty, fade);
            tt.BeginAnimation(TranslateTransform.YProperty, rise);
            _bubbleTimer.Stop();
            if (seconds > 0)
            {
                _bubbleTimer.Interval = TimeSpan.FromSeconds(seconds);
                _bubbleTimer.Start();
            }
            _lastInteraction = DateTime.Now;
        }

        public void HideBubble()
        {
            _bubbleTimer.Stop();
            _thinkTimer.Stop();
            if (_bubble.Visibility != Visibility.Visible) return;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(240));
            fade.Completed += delegate
            {
                _bubble.Visibility = Visibility.Collapsed;
                _bubble.BeginAnimation(OpacityProperty, null);
            };
            _bubble.BeginAnimation(OpacityProperty, fade);
        }

        public void SetThinking(string text, string excerpt)
        {
            StopWalk();
            _thinking = true;
            _speaking = false;
            _pet.SetMood(PetMood.Thinking);
            string body = string.IsNullOrWhiteSpace(excerpt) ? text : excerpt.Trim();
            if (body.Length > 150) body = "…" + body.Substring(body.Length - 150);
            ShowBubble(body, 0, true);
            SetStatus("DeepSeek · 思考中", "#FFD36E");
        }

        public void UpdateThinkingText(string stage, string excerpt)
        {
            if (!_thinking) return;
            string body = string.IsNullOrWhiteSpace(excerpt) ? stage : excerpt.Trim();
            if (body.Length > 150) body = "…" + body.Substring(body.Length - 150);
            _bubbleLabel.Text = "鱼鱼在想";
            _bubbleLabel.Foreground = Ui.B("#FFD98A");
            _bubbleText.Text = body;
            _thinkDots.Visibility = Visibility.Visible;
            _thinkTimer.Start();
            if (_bubble.Visibility != Visibility.Visible) _bubble.Visibility = Visibility.Visible;
        }

        public void SetSpeaking()
        {
            StopWalk();
            _thinking = false;
            _speaking = true;
            _thinkTimer.Stop();
            _thinkDots.Visibility = Visibility.Collapsed;
            _pet.SetMood(PetMood.Speaking);
            SetStatus("DeepSeek · 正在回答", "#7FE7FF");
        }

        public void SetAnswerDone(string mood)
        {
            StopWalk();
            _thinking = false;
            _speaking = false;
            _app.ApplyMood(mood);
            SetStatus(_app.HasApiKey ? "DeepSeek · 已就绪" : "DeepSeek · 未配置",
                _app.HasApiKey ? "#7CE3A5" : "#FFC46B");
            HideBubble();
        }

        public void SetError(string message)
        {
            StopWalk();
            _thinking = false;
            _speaking = false;
            _pet.SetMood(PetMood.Sad);
            ShowBubble(message, 5);
            SetStatus("DeepSeek · 连接异常", "#FF8E8E");
        }

        public void ReactToUser(string text)
        {
            _lastInteraction = DateTime.Now;
            _pet.SetMood(PetMood.Listening);
            var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(700) };
            t.Tick += delegate { t.Stop(); if (!_thinking && !_speaking) _pet.SetMood(PetMood.Idle); };
            t.Start();
        }

        public void Feed()
        {
            StopWalk();
            _lastInteraction = DateTime.Now;
            _pet.SetMood(PetMood.Eating);
            var chew = new DoubleAnimation(-1.6, 1.6, TimeSpan.FromMilliseconds(260));
            chew.AutoReverse = true;
            chew.RepeatBehavior = RepeatBehavior.Forever;
            _pet.BeginChew(chew);
            ShowBubble("白饭！是白饭！鱼鱼开动了——", 4.2);
            var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(4.6) };
            t.Tick += delegate
            {
                t.Stop();
                if (!_thinking && !_speaking) _pet.SetMood(PetMood.Happy);
                ShowBubble("嗝…勉强三分饱。", 4.0);
                var t2 = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(4.2) };
                t2.Tick += delegate { t2.Stop(); if (!_thinking && !_speaking) _pet.SetMood(PetMood.Idle); };
                t2.Start();
            };
            t.Start();
            if (_app.ChatVisible) _app.AppendPetNotice("（鱼鱼捧起一碗白饭，吃得脸颊鼓鼓的。）");
        }

        public void Sleep()
        {
            StopWalk();
            _lastInteraction = DateTime.Now.AddMinutes(-10);
            _pet.SetMood(PetMood.Sleepy);
            ShowBubble("呼…白饭…再吃最后一碗…", 5);
        }

        public void Wake()
        {
            _lastInteraction = DateTime.Now;
            if (_pet.Mood == PetMood.Sleepy)
            {
                _pet.SetMood(PetMood.Surprised);
                ShowBubble("唔！我没睡，我在思考。", 3.2);
                var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1.2) };
                t.Tick += delegate
                {
                    t.Stop();
                    if (!_thinking && !_speaking) _pet.SetMood(PetMood.Idle);
                };
                t.Start();
            }
        }

        public void OnAnswerMood(string mood)
        {
            _app.ApplyMood(mood);
        }

        private void OnWanderTick()
        {
            if (_walking || _flying || !IsVisible || _dragging || _thinking || _speaking || _app.Busy || _app.ChatVisible) return;
            if (_pet.Mood == PetMood.Sleepy || _pet.Mood == PetMood.Lying) return;
            if (!_app.Config.WanderEnabled) return;
            if ((DateTime.Now - _lastInteraction).TotalSeconds < 9) return;
            if ((DateTime.Now - _lastWanderEnd).TotalSeconds < 5) return;
            if (_rng.NextDouble() < 0.42) StartWalk();
        }

        public void StartWalk()
        {
            StartWalk(false);
        }

        public void StartWalk(bool force)
        {
            if (_walking || _flying || !IsVisible) return;
            if (!force && (_app.Busy || _app.ChatVisible || !_app.Config.WanderEnabled)) return;
            RefreshPlatformsIfNeeded(true);
            if (!_grounded || _groundPlatform == null)
            {
                PetPlatform at = PlatformAt(Left + Width / 2.0, Top + FeetOffset);
                if (at == null)
                {
                    BeginFall();
                    return;
                }
                _groundPlatform = at;
                _grounded = true;
                Top = at.Top - FeetOffset;
            }
            PetPlatform p = _groundPlatform;
            double min = p.Left + 4;
            double max = p.Right - Width - 4;
            if (max < min) { min = p.Left; max = Math.Max(min, p.Right - Width); }
            int dir = _rng.Next(2) == 0 ? -1 : 1;
            if (Left <= min + 16) dir = 1;
            if (Left >= max - 16) dir = -1;
            double distance = 70 + _rng.Next(150);
            double target = Math.Max(min, Math.Min(max, Left + dir * distance));
            // Sometimes deliberately walk off a window edge and fall to a lower platform.
            if (!p.IsBottom && _rng.Next(2) == 0)
            {
                target = dir > 0 ? p.Right + Width / 2.0 + 40 : p.Left - Width / 2.0 - 40;
            }
            else if (Math.Abs(target - Left) < 30)
            {
                dir = -dir;
                target = Math.Max(min, Math.Min(max, Left + dir * distance));
            }
            if (Math.Abs(target - Left) < 12) return;
            _walkDirection = target > Left ? 1 : -1;
            _walkTargetLeft = target;
            _walking = true;
            _flying = false;
            _vx = _walkDirection * WalkSpeed;
            _vy = 0;
            _lastInteraction = DateTime.Now;
            _pet.SetFacing(_walkDirection);
            _pet.SetMood(PetMood.Walking);
            UpdateChrome();
            StartMotion();
            if (force) ShowBubble("走咯，跟上。", 2.4);
            else if (_rng.Next(3) == 0) ShowBubble("出去走走。", 2.2);
        }

        private static bool HasWord(string text, params string[] words)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < words.Length; i++)
                if (text.IndexOf(words[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public void ReactToKeywords(string text)
        {
            string t = (text ?? "").Trim();
            if (t.Length == 0) return;

            // Negations first, otherwise "不要走" would match "走".
            if (HasWord(t, "睡不着", "失眠"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Sad);
                ShowBubble("睡不着的话，鱼鱼陪你数白饭。", 3.2);
                return;
            }
            if (HasWord(t, "别走", "不要走", "不用走", "停下", "停一停", "站住", "别动", "回来"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Idle);
                ShowBubble("好，鱼鱼不动。", 2.2);
                return;
            }
            if (HasWord(t, "别睡", "不要睡", "不用睡", "醒醒", "起床", "别吃", "不吃", "不饿"))
            {
                StopWalk();
                Wake();
                _pet.SetMood(PetMood.Idle);
                ShowBubble("好，鱼鱼醒着。", 2.2);
                return;
            }

            if (HasWord(t, "睡", "困", "晚安", "休息"))
            {
                Sleep();
                return;
            }
            if (HasWord(t, "吃", "饭", "白饭", "饿", "喂"))
            {
                Feed();
                return;
            }
            if (HasWord(t, "走", "散步", "溜达", "逛逛", "出去"))
            {
                StartWalk(true);
                return;
            }
            if (HasWord(t, "摸", "摸摸", "拍", "揉"))
            {
                Wake();
                _pet.PlayWave();
                ShowBubble("唔…摸头要收费的，收白饭。", 3.0);
                return;
            }
            if (HasWord(t, "躺", "趴"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Lying);
                ShowBubble("躺一下，就一下。", 2.8);
                return;
            }
            if (HasWord(t, "哭", "委屈", "难过", "伤心", "呜呜"))
            {
                StopWalk();
                _pet.SetMood(HasWord(t, "大哭", "哇") ? PetMood.Cry : PetMood.Sad);
                return;
            }
            if (HasWord(t, "生气", "气死", "讨厌", "哼"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Angry);
                return;
            }
            if (HasWord(t, "害羞", "脸红", "不好意思"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Shy);
                return;
            }
            if (HasWord(t, "震惊", "吓", "惊", "真的假的"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Surprised);
                return;
            }
            if (HasWord(t, "开心", "高兴", "好耶", "棒", "喜欢"))
            {
                StopWalk();
                _pet.SetMood(PetMood.Happy);
                return;
            }
        }

        public void StopWalk()
        {
            if (!_walking && !_flying) return;
            bool keepGround = _grounded && _groundPlatform != null;
            _walking = false;
            _flying = false;
            _vx = 0;
            _vy = 0;
            _grounded = keepGround;
            if (keepGround && _groundPlatform != null) Top = _groundPlatform.Top - FeetOffset;
            _walkTimer.Stop();
            _pet.SetMood(PetMood.Idle);
            UpdateChrome();
            _lastWanderEnd = DateTime.Now;
            _app.SaveWindowPosition(Left, Top);
        }

        public bool IsWalking { get { return _walking; } }

        private void StartMotion()
        {
            _lastMotionTick = DateTime.Now;
            if (!_walkTimer.IsEnabled) _walkTimer.Start();
        }

        private void BeginFall()
        {
            if (_flying) return;
            _walking = false;
            _flying = true;
            _grounded = false;
            _groundPlatform = null;
            _vx = 0;
            _vy = 0;
            _pet.SetMood(PetMood.Surprised);
            UpdateChrome();
            StartMotion();
        }

        private void BeginThrow(double vx, double vy)
        {
            if (!IsVisible) return;
            _walking = false;
            _flying = true;
            _grounded = false;
            _groundPlatform = null;
            _vx = Math.Max(-2200, Math.Min(2200, vx));
            _vy = Math.Max(-2200, Math.Min(2200, vy));
            if (Math.Abs(_vx) < 40 && Math.Abs(_vy) < 40) _vy = 80;
            _pet.SetFacing(_vx >= 0 ? 1 : -1);
            _pet.SetMood(PetMood.Surprised);
            UpdateChrome();
            StartMotion();
            ShowBubble("哇——", 1.4);
        }

        private void RefreshPlatformsIfNeeded(bool force)
        {
            if (!force && (DateTime.Now - _lastPlatformRefresh).TotalMilliseconds < 650) return;
            _lastPlatformRefresh = DateTime.Now;
            _platforms.Clear();
            var wa = SystemParameters.WorkArea;
            _platforms.Add(new PetPlatform { Top = wa.Bottom, Left = wa.Left, Right = wa.Right, IsBottom = true });

            Matrix deviceToDip = Matrix.Identity;
            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                    deviceToDip = source.CompositionTarget.TransformFromDevice;
            }
            catch { }

            try
            {
                uint myPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                Native.EnumWindows(delegate(IntPtr h, IntPtr l)
                {
                    if (h == IntPtr.Zero || h == _hwnd) return true;
                    if (!Native.IsWindowVisible(h) || Native.IsIconic(h)) return true;
                    uint pid;
                    Native.GetWindowThreadProcessId(h, out pid);
                    if (pid == myPid) return true;
                    int ex = Native.GetWindowLong(h, Native.GWL_EXSTYLE);
                    if ((ex & Native.WS_EX_TOOLWINDOW) != 0) return true;
                    string cls = Native.ClassName(h);
                    if (cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" ||
                        cls == "Progman" || cls == "WorkerW" || cls == "DV2ControlHost") return true;
                    if (Native.GetWindowTextLength(h) <= 0) return true;
                    Native.RECT r;
                    if (!Native.GetFrameRect(h, out r)) return true;
                    Point tl = deviceToDip.Transform(new Point(r.Left, r.Top));
                    Point br = deviceToDip.Transform(new Point(r.Right, r.Bottom));
                    double top = tl.Y;
                    if (top <= wa.Top + 6 || top >= wa.Bottom - 20) return true;
                    double left = Math.Max(wa.Left, tl.X);
                    double right = Math.Min(wa.Right, br.X);
                    if (right - left < 100) return true;
                    if (!IsWindowExposed(h, r)) return true;
                    _platforms.Add(new PetPlatform { Top = top, Left = left, Right = right, Hwnd = h });
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        private bool IsWindowExposed(IntPtr hwnd, Native.RECT rect)
        {
            try
            {
                int x = (rect.Left + rect.Right) / 2;
                int y = rect.Top + 8;
                // WindowFromPoint would always return our topmost pet window at this point.
                // EnumWindows walks top-to-bottom in Z order, so find the first non-own window.
                IntPtr top = IntPtr.Zero;
                uint myPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                Native.EnumWindows(delegate(IntPtr h, IntPtr l)
                {
                    if (h == IntPtr.Zero || h == _hwnd) return true;
                    if (!Native.IsWindowVisible(h) || Native.IsIconic(h)) return true;
                    uint pid;
                    Native.GetWindowThreadProcessId(h, out pid);
                    if (pid == myPid) return true;
                    Native.RECT r;
                    if (!Native.GetFrameRect(h, out r)) return true;
                    if (x < r.Left || x > r.Right || y < r.Top || y > r.Bottom) return true;
                    top = h;
                    return false;
                }, IntPtr.Zero);
                return top == hwnd;
            }
            catch { return false; }
        }

        private PetPlatform PlatformByHwnd(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;
            for (int i = 0; i < _platforms.Count; i++)
                if (_platforms[i].Hwnd == hwnd) return _platforms[i];
            return null;
        }

        private void CheckSupport()
        {
            if (!_grounded || _groundPlatform == null || _walking || _flying || _dragging) return;
            if (_groundPlatform.IsBottom) return;
            RefreshPlatformsIfNeeded(true);
            PetPlatform old = _groundPlatform;
            PetPlatform p = PlatformByHwnd(old.Hwnd);
            if (p == null || Math.Abs(p.Top - old.Top) > 2 ||
                Math.Abs(p.Left - old.Left) > 2 || Math.Abs(p.Right - old.Right) > 2)
            {
                BeginFall();
                return;
            }
            double center = Left + Width / 2.0;
            double feet = Top + FeetOffset;
            if (center < p.Left - 2 || center > p.Right + 2 || Math.Abs(feet - p.Top) > 14)
                BeginFall();
        }

        private PetPlatform PlatformAt(double centerX, double feet)
        {
            PetPlatform best = null;
            for (int i = 0; i < _platforms.Count; i++)
            {
                PetPlatform p = _platforms[i];
                if (centerX < p.Left || centerX > p.Right) continue;
                if (Math.Abs(feet - p.Top) > 16) continue;
                if (best == null || p.Top < best.Top) best = p;
            }
            return best;
        }

        private PetPlatform FindLanding(double beforeFeet, double afterFeet, double centerX)
        {
            PetPlatform best = null;
            for (int i = 0; i < _platforms.Count; i++)
            {
                PetPlatform p = _platforms[i];
                if (centerX < p.Left || centerX > p.Right) continue;
                if (p.Top < beforeFeet - 2 || p.Top > afterFeet + 2) continue;
                if (best == null || p.Top < best.Top) best = p;
            }
            return best;
        }

        private void LandOn(PetPlatform p)
        {
            Top = p.Top - FeetOffset;
            _groundPlatform = p;
            _grounded = true;
            if (p.IsBottom && Math.Abs(_vy) > 220)
            {
                // Bounce once more off the bottom edge, then settle.
                _grounded = false;
                _flying = true;
                _vy = -Math.Abs(_vy) * 0.42;
                _pet.SetMood(PetMood.Surprised);
                return;
            }
            _flying = false;
            _vy = 0;
            _vx *= p.IsBottom ? 0.55 : 0.70;
            if (Math.Abs(_vx) < 16) _vx = 0;
            _pet.SetFacing(_vx >= 0 ? 1 : -1);
            _pet.SetMood(PetMood.Shy);
            UpdateChrome();
            _lastWanderEnd = DateTime.Now;
            _app.SaveWindowPosition(Left, Top);
            if (Math.Abs(_vx) < 16) _walkTimer.Stop();
            var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(900) };
            t.Tick += delegate
            {
                t.Stop();
                if (!_thinking && !_speaking && !_walking && _pet.Mood == PetMood.Shy)
                    _pet.SetMood(PetMood.Idle);
            };
            t.Start();
        }

        private void UpdateChrome()
        {
            // Controls live beside the pet instead of underneath its feet, so they
            // stay visible even when the pet is standing on the taskbar.
            _status.Visibility = Visibility.Visible;
            if (_feedButton != null) _feedButton.Visibility = Visibility.Visible;
        }

        private void OnWalkTick()
        {
            if (!_walking && !_flying) { _walkTimer.Stop(); return; }
            DateTime now = DateTime.Now;
            double dt = (now - _lastMotionTick).TotalSeconds;
            _lastMotionTick = now;
            if (dt <= 0 || dt > 0.08) dt = 0.016;
            RefreshPlatformsIfNeeded(false);

            if (_walking)
            {
                if (!_grounded || _groundPlatform == null) { BeginFall(); return; }
                PetPlatform p = _groundPlatform;
                if (!p.IsBottom)
                {
                    PetPlatform current = PlatformByHwnd(p.Hwnd);
                    if (current == null)
                    {
                        BeginFall();
                        return;
                    }
                    p = current;
                    _groundPlatform = p;
                    Top = p.Top - FeetOffset;
                }
                var waWalk = SystemParameters.WorkArea;
                double rawNext = Left + _vx * dt;
                double next = Math.Max(waWalk.Left, Math.Min(waWalk.Right - Width, rawNext));
                bool screenBlocked = Math.Abs(next - rawNext) > 0.1;
                if (screenBlocked) _walkTargetLeft = next;
                double center = next + Width / 2.0;
                if (!p.IsBottom && screenBlocked) center = p.Right + 3;
                if (center < p.Left - 2 || center > p.Right + 2)
                {
                    Left = next;
                    _walking = false;
                    _flying = true;
                    _grounded = false;
                    _vy = 0;
                    _pet.SetMood(PetMood.Surprised);
                    ShowBubble("啊——", 1.2);
                    StartMotion();
                    return;
                }
                bool reached = (_walkDirection > 0 && next >= _walkTargetLeft) ||
                               (_walkDirection < 0 && next <= _walkTargetLeft);
                if (reached)
                {
                    Left = _walkTargetLeft;
                    StopWalk();
                    return;
                }
                Left = next;
                return;
            }

            var wa = SystemParameters.WorkArea;
            double beforeFeet = Top + FeetOffset;
            _vx *= (1 - 0.30 * dt);
            _vy += Gravity * dt;
            Left += _vx * dt;
            Top += _vy * dt;

            if (Left < wa.Left) { Left = wa.Left; _vx = Math.Abs(_vx) * 0.55; }
            if (Left + Width > wa.Right) { Left = wa.Right - Width; _vx = -Math.Abs(_vx) * 0.55; }
            if (Top < wa.Top) { Top = wa.Top; _vy = Math.Abs(_vy) * 0.52; }

            double feet = Top + FeetOffset;
            if (_vy >= 0)
            {
                PetPlatform land = FindLanding(beforeFeet, feet, Left + Width / 2.0);
                if (land != null)
                {
                    LandOn(land);
                    return;
                }
            }
            if (feet > wa.Bottom + 80)
            {
                PetPlatform bottom = null;
                for (int i = 0; i < _platforms.Count; i++)
                    if (_platforms[i].IsBottom) { bottom = _platforms[i]; break; }
                if (bottom != null) LandOn(bottom);
            }
        }

        private void OnIdleTick()
        {
            if (!IsVisible) return;
            if (_walking || _flying || _dragging) return;
            var idle = DateTime.Now - _lastInteraction;
            if (idle.TotalMinutes > 4.2 && _pet.Mood != PetMood.Sleepy && !_thinking && !_speaking)
            {
                Sleep();
                return;
            }
            if (_thinking || _speaking || _app.ChatVisible) return;
            if (_pet.Mood == PetMood.Sleepy || _pet.Mood == PetMood.Lying) return;
            if (idle.TotalSeconds > 9 && _pet.Mood == PetMood.Idle && _rng.NextDouble() < 0.55)
            {
                PlayIdleFidget();
                return;
            }
            if (idle.TotalSeconds < 30) return;
            _lastInteraction = DateTime.Now;
            string[] lines = new string[]
            {
                "今天也要好好吃白饭。",
                "我在这里，不吵你。",
                "工作辛苦了，摸摸鱼头吗？",
                "刚才是不是有人在想我？",
                "鱼鱼正在待机，电量：一碗白饭。",
                "要不要聊两句？我脑子已经热好了。"
            };
            ShowBubble(lines[_rng.Next(lines.Length)], 5.2);
            if (_rng.Next(3) == 0) _pet.PlayWave();
        }

        private void PlayIdleFidget()
        {
            int pick = _rng.Next(7);
            PetMood mood;
            string line;
            if (pick == 0) { mood = PetMood.Happy; line = "嘿嘿，今天也很有精神。"; }
            else if (pick == 1) { mood = PetMood.Shy; line = "别一直看鱼鱼啦…"; }
            else if (pick == 2) { mood = PetMood.Surprised; line = "诶？刚才是不是有动静？"; }
            else if (pick == 3) { mood = PetMood.Thinking; line = "鱼鱼在偷偷想事情。"; }
            else if (pick == 4) { mood = PetMood.Lying; line = "摸鱼一下，就一下。"; }
            else if (pick == 5) { mood = PetMood.Received; line = "收到收到。"; }
            else { mood = PetMood.Angry; line = "哼，白饭怎么还没来。"; }

            _pet.SetMood(mood);
            if (_rng.Next(2) == 0) ShowBubble(line, 2.6);
            int ms = 1600 + _rng.Next(1000);
            var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(ms) };
            t.Tick += delegate
            {
                t.Stop();
                if (!_thinking && !_speaking && !_walking && !_app.ChatVisible && _pet.Mood == mood)
                    _pet.SetMood(PetMood.Idle);
            };
            t.Start();
        }

        private void OnPetMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            StopWalk();
            if (_pet.Mood == PetMood.Sleepy) Wake();
            _lastInteraction = DateTime.Now;
            _dragging = true;
            _dragMoved = false;
            _dragLineShown = false;
            _flying = false;
            _grounded = false;
            _walkTimer.Stop();
            _dragStart = CursorInDip();
            _dragLastPos = _dragStart;
            _dragLastTime = DateTime.UtcNow;
            _dragVX = 0;
            _dragVY = 0;
            _startLeft = Left;
            _startTop = Top;
            _pet.CaptureMouse();
            e.Handled = true;
        }

        private void OnPetMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || e.LeftButton != MouseButtonState.Pressed) return;
            var now = CursorInDip();
            double dx = now.X - _dragStart.X;
            double dy = now.Y - _dragStart.Y;
            if (!_dragMoved && (Math.Abs(dx) > 4 || Math.Abs(dy) > 4))
            {
                _dragMoved = true;
                _pet.SetMood(PetMood.Shy);
                if (!_dragLineShown)
                {
                    _dragLineShown = true;
                    ShowBubble("呜…轻一点，鱼鱼要被拎起来了。", 2.6);
                }
            }
            if (_dragMoved)
            {
                Left = _startLeft + dx;
                Top = _startTop + dy;
                DateTime nowTime = DateTime.UtcNow;
                double dtv = (nowTime - _dragLastTime).TotalSeconds;
                if (dtv > 0.012)
                {
                    double nvx = (now.X - _dragLastPos.X) / dtv;
                    double nvy = (now.Y - _dragLastPos.Y) / dtv;
                    _dragVX = _dragVX * 0.50 + nvx * 0.50;
                    _dragVY = _dragVY * 0.50 + nvy * 0.50;
                    _dragLastPos = now;
                    _dragLastTime = nowTime;
                }
            }
        }

        private void OnPetMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            _dragging = false;
            _pet.ReleaseMouseCapture();
            _lastInteraction = DateTime.Now;
            if (!_dragMoved)
            {
                _pet.PlayWave();
                _app.ShowChat(true);
            }
            else
            {
                double still = (DateTime.UtcNow - _dragLastTime).TotalSeconds;
                double speed = Math.Sqrt(_dragVX * _dragVX + _dragVY * _dragVY);
                if (still > 0.12) speed = 0;
                if (speed > 280) BeginThrow(_dragVX, _dragVY);
                else BeginFall();
            }
            e.Handled = true;
        }

        private void OpenChatFromBubble()
        {
            StopWalk();
            _lastInteraction = DateTime.Now;
            _app.ShowChat(true);
        }

        private void OnStatusClick()
        {
            StopWalk();
            if (!_app.HasApiKey) _app.ShowSettings();
            else _app.ShowChat(true);
        }

        private void OnPetRightClick(object sender, MouseButtonEventArgs e)
        {
            StopWalk();
            ShowContextMenu();
            e.Handled = true;
        }

        private void OnWindowRightClick(object sender, MouseButtonEventArgs e)
        {
            StopWalk();
            ShowContextMenu();
            e.Handled = true;
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu();
            menu.Background = Ui.BA("#101C31", 248);
            menu.Foreground = Ui.B("#EAF4FF");
            menu.BorderBrush = Ui.BA("#5D94C8", 90);
            menu.BorderThickness = new Thickness(1);
            menu.FontFamily = Ui.Font;
            menu.FontSize = 12.5;
            menu.Padding = new Thickness(3);
            menu.Items.Add(MenuItem("和我聊天", delegate { _app.ShowChat(true); }));
            menu.Items.Add(MenuItem("喂一碗白饭", delegate { Feed(); }));
            menu.Items.Add(MenuItem("散散步", delegate { StartWalk(); }));
            menu.Items.Add(MenuItem("停下", delegate { StopWalk(); }));
            menu.Items.Add(MenuItem("摸摸鱼头", delegate { _pet.PlayWave(); ShowBubble("唔…再摸要收费了，收白饭。", 3.5); }));
            menu.Items.Add(MenuItem("让她睡一会", delegate { Sleep(); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(MenuItem("设置", delegate { _app.ShowSettings(); }));
            menu.Items.Add(MenuItem("隐藏桌宠", delegate { _app.TogglePetVisible(); }));
            menu.Items.Add(MenuItem("退出", delegate { _app.Quit(); }));
            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        private MenuItem MenuItem(string text, RoutedEventHandler click)
        {
            var mi = new MenuItem();
            mi.Header = text;
            mi.Foreground = Ui.B("#EAF4FF");
            mi.Background = Brushes.Transparent;
            mi.Padding = new Thickness(10, 6, 18, 6);
            mi.Click += click;
            return mi;
        }

        public void SetSpeechOnly(string text, double seconds)
        {
            ShowBubble(text, seconds);
        }

        public void SetTopmost(bool value)
        {
            Topmost = value;
        }

        public void RefreshConnectionState()
        {
            if (_thinking || _speaking) return;
            if (_app.SavedKeyInvalid)
                SetStatus("DeepSeek · Key 格式不对", "#FF8E8E");
            else
                SetStatus(_app.HasApiKey ? "DeepSeek · 已就绪" : "DeepSeek · 未配置",
                    _app.HasApiKey ? "#7CE3A5" : "#FFC46B");
        }

        public void AllowClose()
        {
            _allowClose = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                Hide();
                _app.HideChat();
            }
            base.OnClosing(e);
        }
    }
}

