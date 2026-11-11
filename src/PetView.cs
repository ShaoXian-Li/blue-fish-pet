using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BlueFishPet
{
    public enum PetMood
    {
        Idle,
        Listening,
        Thinking,
        Speaking,
        Happy,
        Eating,
        Shy,
        Sleepy,
        Surprised,
        Walking,
        Sad,
        Angry,
        Cry,
        Lying,
        Received
    }

    public class PetView : Canvas
    {
        private Canvas _art;
        private Canvas _aura;
        private Canvas _spriteLayer;
        private Image _spriteImage;
        private RotateTransform _spriteTilt;
        private ScaleTransform _spriteFlip;
        private Dictionary<string, BitmapImage> _sprites;
        private Dictionary<string, byte[]> _spriteAlpha;
        private byte[] _spriteAlphaCurrent;
        private bool _spriteAvailable;
        private Canvas _tailGroup;
        private Canvas _hairGroup;
        private Canvas _bodyGroup;
        private Canvas _leftArmGroup;
        private Canvas _rightArmGroup;
        private Canvas _headGroup;
        private Canvas _effects;
        private Canvas _bowlGroup;
        private Canvas _thoughtDots;
        private Canvas _hearts;
        private Canvas _stars;
        private Canvas _zzz;
        private Canvas _sweat;
        private Canvas _surprise;
        private Canvas _eyesHappy;
        private Canvas _eyesSleepy;
        private Canvas _eyesWide;
        private Canvas _eyesNormal;
        private Canvas _bangsGroup;
        private Path _mouthSmile;
        private Path _mouthOpen;
        private Path _mouthWavy;
        private Path _mouthSleep;
        private Ellipse _tongue;
        private Ellipse _blushL;
        private Ellipse _blushR;
        private Ellipse _auraGlow;
        private RotateTransform _tailRotate;
        private RotateTransform _hairRotate;
        private RotateTransform _headRotate;
        private RotateTransform _ahogeRotate;
        private RotateTransform _leftArmRotate;
        private RotateTransform _rightArmRotate;
        private ScaleTransform _breathe;
        private TranslateTransform _bob;
        private TranslateTransform _eyeLook;
        private ScaleTransform _eyeScale;
        private ScaleTransform _mouthScale;
        private ScaleTransform _bowlScale;
        private TranslateTransform _bowlMove;
        private RotateTransform _bowlRotate;
        private DispatcherTimer _blinkTimer;
        private DispatcherTimer _speakTimer;
        private DispatcherTimer _eatTimer;
        private Random _rng = new Random();

        private PetMood _mood = PetMood.Idle;
        private bool _speaking;
        private bool _loaded;
        private int _eatStep;
        private int _speakStep;

        public PetMood Mood { get { return _mood; } }

        public PetView()
        {
            Width = 360;
            Height = 400;
            ClipToBounds = false;
            // A transparent background makes the whole pet canvas hit-testable;
            // otherwise only the shadow/effects catch mouse events and the head is dead.
            Background = Brushes.Transparent;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(this, EdgeMode.Unspecified);

            _breathe = new ScaleTransform(1, 1, 180, 360);
            _bob = new TranslateTransform(0, 0);
            var rootGroup = new TransformGroup();
            rootGroup.Children.Add(_breathe);
            rootGroup.Children.Add(_bob);
            RenderTransform = rootGroup;

            _aura = new Canvas();
            _aura.Width = 360; _aura.Height = 400;
            Children.Add(_aura);

            _art = new Canvas();
            _art.Width = 360; _art.Height = 400;
            Children.Add(_art);

            _tailGroup = new Canvas();
            _tailRotate = new RotateTransform(0, 238, 315);
            _tailGroup.RenderTransform = _tailRotate;
            _art.Children.Add(_tailGroup);

            _hairGroup = new Canvas();
            _hairRotate = new RotateTransform(0, 180, 110);
            _hairGroup.RenderTransform = _hairRotate;
            _art.Children.Add(_hairGroup);

            _bodyGroup = new Canvas();
            _art.Children.Add(_bodyGroup);

            _headGroup = new Canvas();
            _headRotate = new RotateTransform(0, 180, 208);
            _headGroup.RenderTransform = _headRotate;
            _art.Children.Add(_headGroup);

            _bowlGroup = new Canvas();
            _bowlScale = new ScaleTransform(1, 1, 180, 310);
            _bowlMove = new TranslateTransform(0, 0);
            _bowlRotate = new RotateTransform(0, 180, 310);
            var bowlGroup = new TransformGroup();
            bowlGroup.Children.Add(_bowlScale);
            bowlGroup.Children.Add(_bowlRotate);
            bowlGroup.Children.Add(_bowlMove);
            _bowlGroup.RenderTransform = bowlGroup;
            _bowlGroup.Visibility = Visibility.Collapsed;
            _art.Children.Add(_bowlGroup);

            _effects = new Canvas();
            _effects.Width = 360; _effects.Height = 400;
            _art.Children.Add(_effects);

            BuildShadow();
            BuildTail();
            BuildHair();
            BuildBody();
            BuildHead();
            BuildBowl();
            Ui.SetPos(_bowlGroup, 0, 12);
            BuildEffects();
            BuildSpriteLayer();

            _blinkTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _blinkTimer.Interval = TimeSpan.FromSeconds(2.2);
            _blinkTimer.Tick += delegate { DoBlink(); };

            _speakTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _speakTimer.Interval = TimeSpan.FromMilliseconds(130);

            _eatTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _eatTimer.Interval = TimeSpan.FromMilliseconds(460);

            SetMood(PetMood.Idle);
            Loaded += delegate
            {
                if (_loaded) return;
                _loaded = true;
                StartIdleLoop();
                _blinkTimer.Start();
                if (_mood == PetMood.Sleepy) SetMood(PetMood.Sleepy);
            };
        }

        private Path Add(Canvas c, string data, Brush fill)
        {
            var p = Ui.P(data, fill);
            c.Children.Add(p);
            return p;
        }

        private Path AddStroke(Canvas c, string data, Brush stroke, double width)
        {
            var p = Ui.Stroke(data, stroke, width);
            c.Children.Add(p);
            return p;
        }

        private Ellipse AddEllipse(Canvas c, double x, double y, double w, double h, Brush fill)
        {
            var e = new Ellipse();
            e.Width = w; e.Height = h;
            e.Fill = fill;
            Ui.SetPos(e, x, y);
            c.Children.Add(e);
            return e;
        }

        private void BuildSpriteLayer()
        {
            _spriteLayer = new Canvas();
            _spriteLayer.Width = 360;
            _spriteLayer.Height = 400;
            _spriteLayer.IsHitTestVisible = false;

            _spriteImage = new Image();
            _spriteImage.Width = 177;
            _spriteImage.Height = 177;
            _spriteImage.Stretch = Stretch.Uniform;
            _spriteImage.SnapsToDevicePixels = false;
            RenderOptions.SetBitmapScalingMode(_spriteImage, BitmapScalingMode.HighQuality);
            // Transform centers are relative to the image itself. Keep them at the
            // image center so a mirrored side view stays centered instead of
            // flipping half of the body outside the window.
            _spriteTilt = new RotateTransform(0, 88.5, 88.5);
            _spriteFlip = new ScaleTransform(1, 1, 88.5, 88.5);
            var spriteTransforms = new TransformGroup();
            spriteTransforms.Children.Add(_spriteFlip);
            spriteTransforms.Children.Add(_spriteTilt);
            _spriteImage.RenderTransform = spriteTransforms;
            Ui.SetPos(_spriteImage, 91, 177);
            _spriteLayer.Children.Add(_spriteImage);

            _sprites = new Dictionary<string, BitmapImage>();
            _spriteAlpha = new Dictionary<string, byte[]>();
            foreach (string key in new string[] { "daily", "happy", "confused", "shy", "eating",
                "sad", "ok", "angry", "sleep", "cry", "lie", "shock", "side" })
            {
                var bmp = LoadSprite("BlueFishPet.Assets." + key + ".png");
                _sprites[key] = bmp;
                if (bmp != null) _spriteAlpha[key] = LoadAlpha(bmp);
            }
            BitmapImage sprite;
            _sprites.TryGetValue("daily", out sprite);
            _spriteAlpha.TryGetValue("daily", out _spriteAlphaCurrent);
            if (sprite != null)
            {
                _spriteImage.Source = sprite;
                _spriteImage.Effect = Ui.Glow(Ui.C("#5CB8FF"), 10, 0.28);
                _spriteAvailable = true;
                _art.Visibility = Visibility.Collapsed;
                _art.Children.Remove(_bowlGroup);
                _spriteLayer.Children.Add(_bowlGroup);
            }
            Children.Add(_spriteLayer);
            if (_spriteAvailable)
            {
                _art.Children.Remove(_effects);
                Children.Add(_effects);
            }
            _spriteLayer.Visibility = _spriteAvailable ? Visibility.Visible : Visibility.Collapsed;
        }

        private static BitmapImage LoadSprite(string resourceName)
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream == null) return null;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
                bmp.Freeze();
                stream.Dispose();
                return bmp;
            }
            catch (Exception ex)
            {
                AppPaths.Log("加载桌宠贴图失败: " + ex.Message);
                return null;
            }
        }

        private void ShowSprite(PetMood mood)
        {
            if (!_spriteAvailable || _sprites == null) return;
            string key = "daily";
            if (mood == PetMood.Thinking) key = "confused";
            else if (mood == PetMood.Happy) key = "happy";
            else if (mood == PetMood.Eating) key = "eating";
            else if (mood == PetMood.Shy) key = "shy";
            else if (mood == PetMood.Sleepy) key = "sleep";
            else if (mood == PetMood.Surprised) key = "shock";
            else if (mood == PetMood.Walking) key = "side";
            else if (mood == PetMood.Sad) key = "sad";
            else if (mood == PetMood.Angry) key = "angry";
            else if (mood == PetMood.Cry) key = "cry";
            else if (mood == PetMood.Lying) key = "lie";
            else if (mood == PetMood.Received) key = "ok";
            BitmapImage img;
            if (!_sprites.TryGetValue(key, out img) || img == null) return;
            byte[] alpha;
            if (_spriteAlpha.TryGetValue(key, out alpha)) _spriteAlphaCurrent = alpha;
            if (!ReferenceEquals(_spriteImage.Source, img))
            {
                _spriteImage.Source = img;
                _spriteImage.Opacity = 1;
                _spriteImage.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0.6, 1, TimeSpan.FromMilliseconds(140)));
            }
            if (mood != PetMood.Walking) _spriteFlip.ScaleX = 1;
        }

        public void SetFacing(int direction)
        {
            if (_spriteFlip == null) return;
            // The side-view drawing faces left; mirror it when walking right.
            _spriteFlip.ScaleX = direction < 0 ? 1 : -1;
        }

        public void BeginChew(DoubleAnimation animation)
        {
            if (!_spriteAvailable || _spriteTilt == null) return;
            _spriteTilt.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        private static byte[] LoadAlpha(BitmapImage bmp)
        {
            try
            {
                BitmapSource src = bmp;
                if (src.Format != PixelFormats.Bgra32)
                    src = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                int w = src.PixelWidth;
                int h = src.PixelHeight;
                byte[] pixels = new byte[w * h * 4];
                src.CopyPixels(pixels, w * 4, 0);
                byte[] alpha = new byte[w * h];
                for (int i = 0; i < alpha.Length; i++) alpha[i] = pixels[i * 4 + 3];
                return alpha;
            }
            catch (Exception ex)
            {
                AppPaths.Log("加载桌宠透明通道失败: " + ex.Message);
                return null;
            }
        }

        private void BuildShadow()
        {
            var g = new RadialGradientBrush();
            g.GradientStops.Add(new GradientStop(Color.FromArgb(120, 12, 33, 70), 0));
            g.GradientStops.Add(new GradientStop(Color.FromArgb(0, 12, 33, 70), 1));
            var s = new Ellipse();
            s.Width = 130; s.Height = 18;
            s.Fill = g;
            Ui.SetPos(s, 115, 354);
            _aura.Children.Add(s);

            _auraGlow = new Ellipse();
            _auraGlow.Width = 156; _auraGlow.Height = 78;
            var rg = new RadialGradientBrush();
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(45, 90, 205, 255), 0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 90, 205, 255), 1));
            _auraGlow.Fill = rg;
            Ui.SetPos(_auraGlow, 102, 272);
            _aura.Children.Add(_auraGlow);

            for (int i = 0; i < 2; i++)
            {
                var ring = new Ellipse();
                ring.Width = 210 + i * 70;
                ring.Height = 40 + i * 16;
                ring.Stroke = Ui.BA("#83E6FF", (byte)(30 - i * 10));
                ring.StrokeThickness = 1;
                ring.Fill = null;
                Ui.SetPos(ring, 180 - ring.Width / 2, 342 - i * 6);
                _aura.Children.Add(ring);
                var a = new DoubleAnimation(0.55, 1, TimeSpan.FromSeconds(3.0 + i));
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                a.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
                ring.BeginAnimation(OpacityProperty, a);
            }
        }

        private void BuildTail()
        {
            Add(_tailGroup,
                "M 224,300 C 250,296 276,304 294,314 C 310,324 328,326 346,316 C 336,304 334,294 340,280 C 350,292 354,306 352,320 C 348,338 330,348 308,344 C 282,340 264,326 224,332 Z",
                Ui.Linear("#2967DB", "#7BC7FF", 35));
            Add(_tailGroup,
                "M 238,313 C 264,310 286,318 306,328 C 318,334 332,333 342,324 C 336,337 323,342 309,339 C 284,333 262,324 238,326 Z",
                Ui.BA("#123A91", 95));
            AddStroke(_tailGroup, "M 296,317 C 315,323 330,320 344,309", Ui.BA("#B8F4FF", 150), 2.2);
            AddStroke(_tailGroup, "M 265,317 C 282,319 297,324 309,331", Ui.BA("#A7E7FF", 80), 1.4);
        }

        private void BuildHair()
        {
            Add(_hairGroup,
                "M 180,42 C 124,42 94,82 96,136 C 97,168 86,200 82,238 C 77,280 88,318 112,336 C 132,348 152,338 146,318 C 138,292 142,260 148,232 L 212,232 C 218,260 222,292 214,318 C 208,338 228,348 248,336 C 272,318 283,280 278,238 C 274,200 263,168 264,136 C 266,82 236,42 180,42 Z",
                Ui.Linear("#2456C4", "#58A2F4", 80));
            Add(_hairGroup,
                "M 180,55 C 132,55 104,90 105,140 C 106,170 94,200 89,235 C 84,274 94,310 112,326 C 122,334 132,332 134,323 C 122,294 127,258 134,228 L 226,228 C 233,258 238,294 226,323 C 228,332 238,334 248,326 C 266,310 276,274 271,235 C 266,200 254,170 255,140 C 256,90 228,55 180,55 Z",
                Ui.Linear("#356FDC", "#6FB6FF", 100));
            AddStroke(_hairGroup, "M 103,158 C 93,208 84,268 98,319", Ui.BA("#B4E7FF", 100), 3.2);
            AddStroke(_hairGroup, "M 257,158 C 267,208 276,268 262,319", Ui.BA("#B4E7FF", 90), 3.0);
            AddStroke(_hairGroup, "M 117,322 C 126,333 140,334 146,325", Ui.BA("#1C4AA8", 100), 2.0);
            AddStroke(_hairGroup, "M 243,322 C 234,333 220,334 214,325", Ui.BA("#1C4AA8", 100), 2.0);
        }

        private void BuildBody()
        {
            AddEllipse(_bodyGroup, 143, 352, 32, 14, Ui.B("#18243F"));
            AddEllipse(_bodyGroup, 185, 352, 32, 14, Ui.B("#18243F"));
            AddStroke(_bodyGroup, "M 154,347 C 154,342 165,340 173,343", Ui.BA("#FFFFFF", 75), 2.0);

            Add(_bodyGroup,
                "M 156,272 C 142,294 124,326 110,346 C 126,357 151,363 180,363 C 209,363 234,357 250,346 C 236,326 218,294 204,272 Z",
                Ui.Linear("#28427F", "#142550", 90));
            Add(_bodyGroup,
                "M 118,343 C 138,368 222,368 242,343 C 232,359 210,366 180,366 C 150,366 128,359 118,343 Z",
                Ui.BA("#D9EAFF", 245));
            for (int i = 0; i < 9; i++)
            {
                double x = 123 + i * 14.2;
                var frill = new Ellipse();
                frill.Width = 18; frill.Height = 13;
                frill.Fill = Ui.B("#EEF7FF");
                frill.Opacity = 0.94;
                Ui.SetPos(frill, x, 354 + Math.Abs(i - 4) * 0.6);
                _bodyGroup.Children.Add(frill);
            }

            Add(_bodyGroup,
                "M 148,228 C 160,218 200,218 212,228 L 208,282 C 198,290 162,290 152,282 Z",
                Ui.Linear("#2F4B8D", "#1B2C5B", 90));
            AddStroke(_bodyGroup, "M 156,232 C 163,225 197,225 204,232", Ui.BA("#A3CBFF", 100), 1.4);

            Add(_bodyGroup,
                "M 162,239 L 198,239 L 203,285 C 213,310 219,336 218,344 C 205,352 155,352 142,344 C 142,336 147,310 157,285 Z",
                Ui.Linear("#FFFFFF", "#DDEBFF", 90));
            AddStroke(_bodyGroup, "M 165,239 L 158,251 M 195,239 L 202,251", Ui.BA("#B9D3F4", 210), 3.0);
            AddStroke(_bodyGroup, "M 152,286 C 166,294 194,294 208,286", Ui.BA("#AFC9EE", 160), 2.0);
            AddStroke(_bodyGroup, "M 118,344 C 138,355 222,355 242,344", Ui.BA("#D9B66A", 155), 1.6);

            // apron whale
            Add(_bodyGroup,
                "M 169,315 C 169,307 176,302 185,303 C 195,304 197,312 193,318 C 189,324 179,325 173,320 C 170,318 169,317 169,315 Z",
                Ui.BA("#357BE8", 210));
            Add(_bodyGroup, "M 170,316 L 162,311 L 165,320 Z", Ui.BA("#357BE8", 210));
            AddEllipse(_bodyGroup, 173, 309, 3, 3, Ui.BA("#FFFFFF", 230));
            AddStroke(_bodyGroup, "M 176,319 C 180,323 186,323 190,319", Ui.BA("#FFFFFF", 200), 1.4);

            // waist ribbon
            Add(_bodyGroup, "M 180,282 L 158,272 L 160,290 Z", Ui.BA("#294B96", 235));
            Add(_bodyGroup, "M 180,282 L 202,272 L 200,290 Z", Ui.BA("#294B96", 235));
            AddEllipse(_bodyGroup, 174, 277, 12, 10, Ui.B("#3D6EC9"));

            // left sleeve + arm (own pivot so the pet can raise a hand)
            _leftArmGroup = new Canvas();
            _leftArmRotate = new RotateTransform(0, 130, 246);
            _leftArmGroup.RenderTransform = _leftArmRotate;
            _bodyGroup.Children.Add(_leftArmGroup);
            Add(_leftArmGroup,
                "M 148,228 C 126,224 112,240 116,262 C 120,280 136,285 148,271 C 153,255 153,238 148,228 Z",
                Ui.Linear("#35538F", "#22386D", 80));
            Add(_leftArmGroup,
                "M 118,266 C 110,282 106,298 110,312 C 118,321 131,318 135,306 C 138,290 136,276 132,266 Z",
                Ui.Linear("#FFE9DE", "#F4C8B9", 90));
            AddEllipse(_leftArmGroup, 111, 307, 25, 20, Ui.Linear("#FFE9DE", "#F3C6B7", 90));
            AddStroke(_leftArmGroup, "M 111,264 C 105,278 103,294 107,307", Ui.BA("#FFFFFF", 130), 5.0);

            // right sleeve + arm
            _rightArmGroup = new Canvas();
            _rightArmRotate = new RotateTransform(0, 230, 246);
            _rightArmGroup.RenderTransform = _rightArmRotate;
            _bodyGroup.Children.Add(_rightArmGroup);
            Add(_rightArmGroup,
                "M 212,228 C 234,224 248,240 244,262 C 240,280 224,285 212,271 C 207,255 207,238 212,228 Z",
                Ui.Linear("#35538F", "#22386D", 80));
            Add(_rightArmGroup,
                "M 242,266 C 250,282 254,298 250,312 C 242,321 229,318 225,306 C 222,290 224,276 228,266 Z",
                Ui.Linear("#FFE9DE", "#F4C8B9", 90));
            AddEllipse(_rightArmGroup, 224, 307, 25, 20, Ui.Linear("#FFE9DE", "#F3C6B7", 90));
            AddStroke(_rightArmGroup, "M 249,264 C 255,278 257,294 253,307", Ui.BA("#FFFFFF", 130), 5.0);

            AddStroke(_bodyGroup, "M 150,282 C 164,287 196,287 210,282", Ui.BA("#7EA5DE", 120), 1.0);
        }

        private void BuildHead()
        {
            // neck
            Add(_headGroup, "M 165,205 L 195,205 L 198,238 C 190,246 170,246 162,238 Z",
                Ui.Linear("#FFE5D8", "#F2C2B2", 90));

            // fins behind face
            Add(_headGroup,
                "M 108,148 C 82,136 55,140 42,158 C 50,181 76,197 112,193 C 118,178 116,160 108,148 Z",
                Ui.Linear("#2C69DC", "#79C8FF", 20));
            Add(_headGroup,
                "M 96,157 C 76,153 60,158 52,166 C 62,179 79,187 101,184 C 105,174 102,164 96,157 Z",
                Ui.BA("#CDEFFF", 210));
            AddStroke(_headGroup, "M 96,162 C 83,162 69,166 58,171 M 93,171 C 82,172 72,176 64,180",
                Ui.BA("#7BB6E8", 160), 1.5);

            Add(_headGroup,
                "M 252,148 C 278,136 305,140 318,158 C 310,181 284,197 248,193 C 242,178 244,160 252,148 Z",
                Ui.Linear("#79C8FF", "#2C69DC", 160));
            Add(_headGroup,
                "M 264,157 C 284,153 300,158 308,166 C 298,179 281,187 259,184 C 255,174 258,164 264,157 Z",
                Ui.BA("#CDEFFF", 210));
            AddStroke(_headGroup, "M 264,162 C 277,162 291,166 302,171 M 267,171 C 278,172 288,176 296,180",
                Ui.BA("#7BB6E8", 160), 1.5);

            // face
            Add(_headGroup,
                "M 100,125 C 100,80 130,52 180,52 C 230,52 260,80 260,125 C 260,172 234,212 208,220 C 195,224 165,224 152,220 C 126,212 100,172 100,125 Z",
                Ui.Linear("#FFEDE3", "#F8CDBC", 90));
            Add(_headGroup,
                "M 126,202 C 146,220 214,220 234,202 C 222,218 204,224 180,224 C 156,224 138,218 126,202 Z",
                Ui.BA("#EAAE9E", 60));

            // neck frill
            for (int i = 0; i < 5; i++)
            {
                var f = new Ellipse();
                f.Width = 17; f.Height = 14;
                f.Fill = Ui.B("#F6FBFF");
                Ui.SetPos(f, 156 + i * 10.5, 225);
                _headGroup.Children.Add(f);
            }
            Add(_headGroup, "M 162,231 L 180,237 L 198,231 L 192,244 L 168,244 Z", Ui.B("#F9FCFF"));

            // bangs (behind headband)
            _bangsGroup = new Canvas();
            _headGroup.Children.Add(_bangsGroup);
            Add(_bangsGroup,
                "M 101,132 C 96,82 128,51 180,51 C 232,51 264,82 259,132 C 245,114 232,99 218,91 C 206,108 190,118 180,100 C 168,118 150,120 138,100 C 122,108 111,116 101,132 Z",
                Ui.Linear("#2F67D8", "#7CC3FF", 100));
            Add(_bangsGroup,
                "M 104,113 C 94,146 94,192 106,228 C 119,219 125,194 121,171 C 131,150 127,124 104,113 Z",
                Ui.Linear("#2D63D2", "#6FB8FF", 90));
            Add(_bangsGroup,
                "M 256,113 C 266,146 266,192 254,228 C 241,219 235,194 239,171 C 229,150 233,124 256,113 Z",
                Ui.Linear("#6FB8FF", "#2D63D2", 90));
            Add(_bangsGroup,
                "M 164,61 C 176,80 177,104 171,131 C 183,116 191,98 189,78 C 201,67 214,64 225,70 C 213,55 186,51 164,61 Z",
                Ui.BA("#8FD2FF", 175));
            Add(_bangsGroup,
                "M 104,112 C 122,92 142,80 169,76 C 155,84 140,96 130,112 C 122,130 120,146 121,166 C 116,146 109,127 104,112 Z",
                Ui.BA("#9BD8FF", 95));
            AddStroke(_bangsGroup, "M 130,83 C 148,71 166,65 184,63", Ui.BA("#CDF1FF", 140), 2.2);
            AddStroke(_bangsGroup, "M 218,70 C 234,75 246,84 254,96", Ui.BA("#CDF1FF", 110), 2.0);

            // headband
            AddStroke(_headGroup, "M 108,91 C 126,65 234,65 252,91", Ui.BA("#EAF4FF", 235), 7.0);
            for (int i = 0; i < 10; i++)
            {
                double t = i / 9.0;
                double x = 108 + t * 144;
                double y = 91 - Math.Sin(t * Math.PI) * 22;
                var puff = new Ellipse();
                puff.Width = 15; puff.Height = 13;
                puff.Fill = Ui.B("#F7FBFF");
                Ui.SetPos(puff, x - 7.5, y - 10);
                _headGroup.Children.Add(puff);
            }
            AddStroke(_headGroup, "M 108,95 C 126,70 234,70 252,95", Ui.BA("#A7C9F0", 120), 1.0);

            // little whale hairpin
            Add(_headGroup, "M 119,104 C 119,97 126,93 134,95 C 142,97 143,104 138,108 C 133,112 122,111 119,107 Z", Ui.BA("#F4FCFF", 235));
            Add(_headGroup, "M 120,105 L 112,100 L 115,110 Z", Ui.BA("#F4FCFF", 235));
            AddEllipse(_headGroup, 124, 99, 3, 3, Ui.BA("#2C6FE8", 230));
            AddStroke(_headGroup, "M 127,108 C 131,111 136,111 139,108", Ui.BA("#8FD2FF", 210), 1.2);

            // bow
            Add(_headGroup, "M 246,91 C 258,78 272,81 270,94 C 268,105 256,108 246,99 Z", Ui.Linear("#69C8FF", "#2C6FE8", 90));
            Add(_headGroup, "M 246,99 C 259,103 268,113 261,119 C 253,125 243,113 242,104 Z", Ui.Linear("#4BA6F5", "#2453BE", 90));
            AddEllipse(_headGroup, 243, 93, 13, 13, Ui.B("#4B8FEA"));
            AddEllipse(_headGroup, 246, 96, 5, 5, Ui.BA("#C9F2FF", 200));

            // ahoge
            var ahoge = AddStroke(_headGroup, "M 184,53 C 192,20 230,18 231,39 C 232,54 214,58 211,45",
                Ui.Linear("#2E65D7", "#79C4FF", 90), 7.0);
            _ahogeRotate = new RotateTransform(0, 188, 50);
            ahoge.RenderTransform = _ahogeRotate;
            AddStroke(_headGroup, "M 187,47 C 195,27 218,24 225,34", Ui.BA("#B7EBFF", 120), 2.0);

            BuildEyes();
            BuildMouth();
        }

        private void BuildEyes()
        {
            _eyesNormal = new Canvas();
            _eyeScale = new ScaleTransform(1, 1, 180, 159);
            _eyeLook = new TranslateTransform(0, 0);
            var tg = new TransformGroup();
            tg.Children.Add(_eyeScale);
            tg.Children.Add(_eyeLook);
            _eyesNormal.RenderTransform = tg;
            _headGroup.Children.Add(_eyesNormal);

            BuildEye(_eyesNormal, 127, 132, false);
            BuildEye(_eyesNormal, 191, 132, true);

            // brows
            AddStroke(_eyesNormal, "M 130,120 C 139,113 157,113 167,119", Ui.BA("#7A4E58", 150), 2.4);
            AddStroke(_eyesNormal, "M 230,120 C 221,113 203,113 193,119", Ui.BA("#7A4E58", 150), 2.4);

            _eyesHappy = new Canvas();
            _headGroup.Children.Add(_eyesHappy);
            AddStroke(_eyesHappy, "M 129,158 C 139,143 158,143 168,158", Ui.B("#24355C"), 4.2);
            AddStroke(_eyesHappy, "M 192,158 C 202,143 221,143 231,158", Ui.B("#24355C"), 4.2);
            AddStroke(_eyesHappy, "M 129,158 C 139,143 158,143 168,158", Ui.BA("#8FE9FF", 80), 1.2);

            _eyesSleepy = new Canvas();
            _headGroup.Children.Add(_eyesSleepy);
            AddStroke(_eyesSleepy, "M 130,161 C 141,170 158,170 168,161", Ui.B("#24355C"), 3.6);
            AddStroke(_eyesSleepy, "M 192,161 C 202,170 219,170 230,161", Ui.B("#24355C"), 3.6);

            _eyesWide = new Canvas();
            _headGroup.Children.Add(_eyesWide);
            BuildEye(_eyesWide, 127, 132, false);
            BuildEye(_eyesWide, 191, 132, true);

            _eyesHappy.Visibility = Visibility.Collapsed;
            _eyesSleepy.Visibility = Visibility.Collapsed;
            _eyesWide.Visibility = Visibility.Collapsed;

            _blushL = AddEllipse(_headGroup, 115, 179, 27, 12, Ui.BA("#FF8FA6", 95));
            _blushR = AddEllipse(_headGroup, 218, 179, 27, 12, Ui.BA("#FF8FA6", 95));
            _blushL.Opacity = 0.2;
            _blushR.Opacity = 0.2;
        }

        private void BuildEye(Canvas c, double x, double y, bool right)
        {
            var white = Add(c,
                "M " + x + "," + (y + 26) + " C " + x + "," + (y + 8) + " " + (x + 8) + "," + y + " " + (x + 22) + "," + y + " C " + (x + 38) + "," + y + " " + (x + 44) + "," + (y + 12) + " " + (x + 44) + "," + (y + 27) + " C " + (x + 44) + "," + (y + 44) + " " + (x + 34) + "," + (y + 55) + " " + (x + 22) + "," + (y + 55) + " C " + (x + 9) + "," + (y + 55) + " " + x + "," + (y + 43) + " " + x + "," + (y + 26) + " Z",
                Ui.B("#FCFEFF"));

            var iris = new RadialGradientBrush();
            iris.GradientStops.Add(new GradientStop(Ui.C("#2E8BFF"), 0));
            iris.GradientStops.Add(new GradientStop(Ui.C("#1550B8"), 0.62));
            iris.GradientStops.Add(new GradientStop(Ui.C("#0B2B70"), 1));
            var ie = AddEllipse(c, x + 7, y + 9, 31, 39, iris);
            ie.Stroke = Ui.BA("#0D2A63", 150);
            ie.StrokeThickness = 1.2;

            AddEllipse(c, x + 16, y + 19, 14, 20, Ui.B("#0A1B3F"));
            AddEllipse(c, x + 12, y + 12, 10, 12, Ui.BA("#FFFFFF", 245));
            AddEllipse(c, x + 25, y + 37, 6, 6, Ui.BA("#AEEBFF", 230));
            AddEllipse(c, x + 29, y + 13, 4, 4, Ui.BA("#FFFFFF", 160));

            AddStroke(c, "M " + x + " " + (y + 19) + " C " + (x + 10) + " " + (y + 3) + " " + (x + 34) + " " + (y + 3) + " " + (x + 45) + " " + (y + 19),
                Ui.B("#26385F"), 3.4);
            AddStroke(c, "M " + x + " " + (y + 22) + " L " + (x - 4) + " " + (y + 17),
                Ui.B("#26385F"), 2.2);
            if (right)
            {
                AddStroke(c, "M " + (x + 41) + " " + (y + 21) + " L " + (x + 46) + " " + (y + 17),
                    Ui.B("#26385F"), 2.2);
            }
        }

        private void BuildMouth()
        {
            _mouthSmile = AddStroke(_headGroup, "M 169,194 C 175,201 185,201 191,194", Ui.B("#7A3A48"), 2.7);
            _mouthOpen = Add(_headGroup,
                "M 168,192 C 173,186 187,186 192,192 C 191,205 185,213 180,213 C 175,213 169,205 168,192 Z",
                Ui.B("#873C4D"));
            _tongue = AddEllipse(_headGroup, 174, 201, 12, 8, Ui.BA("#F38A97", 220));
            _tongue.Visibility = Visibility.Collapsed;
            _mouthWavy = AddStroke(_headGroup, "M 169,198 Q 174,192 180,198 T 191,198", Ui.B("#7A3A48"), 2.4);
            _mouthSleep = AddStroke(_headGroup, "M 173,198 Q 180,203 187,198", Ui.B("#7A3A48"), 2.2);
            _mouthScale = new ScaleTransform(1, 1, 180, 200);
            _mouthOpen.RenderTransform = _mouthScale;
            _mouthOpen.Visibility = Visibility.Collapsed;
            _mouthWavy.Visibility = Visibility.Collapsed;
            _mouthSleep.Visibility = Visibility.Collapsed;

            AddEllipse(_headGroup, 176, 184, 8, 4, Ui.BA("#E8A596", 110));
        }

        private void BuildBowl()
        {
            // steam
            for (int i = 0; i < 2; i++)
            {
                var steam = AddStroke(_bowlGroup, "M " + (163 + i * 30) + ",270 C " + (156 + i * 30) + ",258 " + (172 + i * 30) + ",252 " + (165 + i * 30) + ",240",
                    Ui.BA("#FFFFFF", 90), 2.2);
                var a = new DoubleAnimation(0.15, 0.65, TimeSpan.FromSeconds(2.2));
                a.AutoReverse = true; a.RepeatBehavior = RepeatBehavior.Forever;
                steam.BeginAnimation(OpacityProperty, a);
            }

            Add(_bowlGroup, "M 135,300 C 138,331 222,331 225,300 L 221,286 L 139,286 Z",
                Ui.Linear("#FFFFFF", "#D5E8FF", 90));
            AddEllipse(_bowlGroup, 137, 284, 86, 20, Ui.B("#F7FCFF"));
            AddEllipse(_bowlGroup, 143, 287, 74, 12, Ui.BA("#D7EBFF", 180));
            Add(_bowlGroup,
                "M 146,286 C 146,267 161,256 180,256 C 199,256 214,267 214,286 Z",
                Ui.Linear("#FFFFFF", "#F6F1E4", 90));
            for (int i = 0; i < 9; i++)
            {
                var rice = AddEllipse(_bowlGroup, 152 + (i % 5) * 11, 267 + (i / 5) * 8, 9, 7,
                    Ui.BA(i % 2 == 0 ? "#FFFFFF" : "#F4F8FF", 240));
                rice.RenderTransform = new RotateTransform((i * 23) % 50 - 25, 5, 3);
            }
            AddStroke(_bowlGroup, "M 181,297 C 189,294 195,297 197,303", Ui.BA("#4C93F2", 220), 2.0);
            AddStroke(_bowlGroup, "M 194,302 C 198,304 202,304 205,302", Ui.BA("#4C93F2", 200), 1.5);
            AddStroke(_bowlGroup, "M 199,246 L 225,219 M 206,249 L 232,224", Ui.BA("#B98A54", 235), 2.8);
            AddStroke(_bowlGroup, "M 199,246 L 225,219", Ui.BA("#E0B27A", 180), 0.8);
        }

        private void BuildEffects()
        {
            _thoughtDots = new Canvas();
            _thoughtDots.Visibility = Visibility.Collapsed;
            _effects.Children.Add(_thoughtDots);
            for (int i = 0; i < 3; i++)
            {
                double size = 8 + i * 4;
                var dot = new Ellipse();
                dot.Width = size; dot.Height = size;
                dot.Fill = Ui.BA("#E8FAFF", 225);
                dot.Stroke = Ui.BA("#6FD5FF", 180);
                dot.StrokeThickness = 1;
                Ui.SetPos(dot, 72 - i * 16, 115 - i * 18);
                _thoughtDots.Children.Add(dot);
                var a = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(520));
                a.BeginTime = TimeSpan.FromMilliseconds(i * 180);
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                dot.BeginAnimation(OpacityProperty, a);
            }

            _hearts = new Canvas();
            _hearts.Visibility = Visibility.Collapsed;
            _effects.Children.Add(_hearts);
            for (int i = 0; i < 3; i++)
            {
                var h = Add(_hearts,
                    "M 0,5 C 0,-2 9,-2 9,5 C 9,11 0,16 0,18 C 0,16 -9,11 -9,5 C -9,-2 0,-2 0,5 Z",
                    Ui.BA(i == 1 ? "#FFB6C9" : "#FF8FB0", 230));
                h.RenderTransform = new TranslateTransform(i == 0 ? 44 : (i == 1 ? 310 : 292), i == 0 ? 188 : (i == 1 ? 140 : 82));
                var a = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700));
                a.BeginTime = TimeSpan.FromMilliseconds(i * 160);
                a.AutoReverse = true;
                a.RepeatBehavior = RepeatBehavior.Forever;
                h.BeginAnimation(OpacityProperty, a);
            }

            _stars = new Canvas();
            _stars.Visibility = Visibility.Collapsed;
            _effects.Children.Add(_stars);
            for (int i = 0; i < 5; i++)
            {
                var st = Add(_stars, "M 0,-9 L 2.4,-2.6 L 9,-2.6 L 3.8,1.3 L 5.8,8 L 0,3.8 L -5.8,8 L -3.8,1.3 L -9,-2.6 L -2.4,-2.6 Z",
                    Ui.BA(i % 2 == 0 ? "#A9F0FF" : "#FFFFFF", 220));
                double[] sx = new double[] { 42, 318, 26, 336, 292 }; double[] sy = new double[] { 74, 108, 220, 252, 52 }; st.RenderTransform = new TranslateTransform(sx[i], sy[i]);
                var a = new DoubleAnimation(0.1, 0.95, TimeSpan.FromSeconds(1.2 + i * 0.2));
                a.BeginTime = TimeSpan.FromMilliseconds(i * 120);
                a.AutoReverse = true; a.RepeatBehavior = RepeatBehavior.Forever;
                st.BeginAnimation(OpacityProperty, a);
            }

            _zzz = new Canvas();
            _zzz.Visibility = Visibility.Collapsed;
            _effects.Children.Add(_zzz);
            for (int i = 0; i < 3; i++)
            {
                var z = Ui.T("Z", 12 + i * 4, "#A8E9FF", true);
                Ui.SetPos(z, 255 + i * 12, 120 - i * 18);
                _zzz.Children.Add(z);
                var a = new DoubleAnimation(0.15, 0.9, TimeSpan.FromSeconds(1.8));
                a.BeginTime = TimeSpan.FromMilliseconds(i * 350);
                a.AutoReverse = true; a.RepeatBehavior = RepeatBehavior.Forever;
                z.BeginAnimation(OpacityProperty, a);
            }

            _sweat = new Canvas();
            _sweat.Visibility = Visibility.Collapsed;
            _effects.Children.Add(_sweat);
            var drop = Add(_sweat, "M 0,0 C 8,13 9,20 0,24 C -9,20 -8,13 0,0 Z", Ui.BA("#9FE4FF", 225));
            Ui.SetPos(drop, 274, 146);
            var da = new DoubleAnimation(0.3, 0.95, TimeSpan.FromSeconds(1.4));
            da.AutoReverse = true; da.RepeatBehavior = RepeatBehavior.Forever;
            drop.BeginAnimation(OpacityProperty, da);

            _surprise = new Canvas();
            _surprise.Visibility = Visibility.Collapsed;
            _effects.Children.Add(_surprise);
            var ex = Ui.T("!", 42, "#FFD86B", true);
            Ui.SetPos(ex, 282, 65);
            _surprise.Children.Add(ex);
            var ea = new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(380));
            ea.AutoReverse = true; ea.RepeatBehavior = RepeatBehavior.Forever;
            ex.BeginAnimation(OpacityProperty, ea);

            var bubbleL = new Ellipse { Width = 8, Height = 8, Fill = Ui.BA("#CBEEFF", 80) };
            Ui.SetPos(bubbleL, 54, 240); _effects.Children.Add(bubbleL);
            var bubbleR = new Ellipse { Width = 6, Height = 6, Fill = Ui.BA("#CBEEFF", 70) };
            Ui.SetPos(bubbleR, 302, 205); _effects.Children.Add(bubbleR);
            var ba = new DoubleAnimation(0.1, 0.5, TimeSpan.FromSeconds(2.5));
            ba.AutoReverse = true; ba.RepeatBehavior = RepeatBehavior.Forever;
            bubbleL.BeginAnimation(OpacityProperty, ba);
            bubbleR.BeginAnimation(OpacityProperty, ba);
        }

        private void StartIdleLoop()
        {
            var tail = new DoubleAnimation(-4, 7, TimeSpan.FromSeconds(1.9));
            tail.AutoReverse = true; tail.RepeatBehavior = RepeatBehavior.Forever;
            tail.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _tailRotate.BeginAnimation(RotateTransform.AngleProperty, tail);

            var hair = new DoubleAnimation(-1.2, 1.2, TimeSpan.FromSeconds(3.6));
            hair.AutoReverse = true; hair.RepeatBehavior = RepeatBehavior.Forever;
            hair.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _hairRotate.BeginAnimation(RotateTransform.AngleProperty, hair);

            var ahoge = new DoubleAnimation(-8, 8, TimeSpan.FromSeconds(2.1));
            ahoge.AutoReverse = true; ahoge.RepeatBehavior = RepeatBehavior.Forever;
            ahoge.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _ahogeRotate.BeginAnimation(RotateTransform.AngleProperty, ahoge);

            var breathe = new DoubleAnimation(1, 1.012, TimeSpan.FromSeconds(2.8));
            breathe.AutoReverse = true; breathe.RepeatBehavior = RepeatBehavior.Forever;
            breathe.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _breathe.BeginAnimation(ScaleTransform.ScaleYProperty, breathe);

            var bob = new DoubleAnimation(0, -4.0, TimeSpan.FromSeconds(2.1));
            bob.AutoReverse = true; bob.RepeatBehavior = RepeatBehavior.Forever;
            bob.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _bob.BeginAnimation(TranslateTransform.YProperty, bob);
        }

        private void DoBlink()
        {
            if (_eyesNormal.Visibility != Visibility.Visible) return;
            var blink = new DoubleAnimationUsingKeyFrames();
            blink.Duration = TimeSpan.FromMilliseconds(180);
            blink.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(0)));
            blink.KeyFrames.Add(new EasingDoubleKeyFrame(0.06, KeyTime.FromPercent(0.42)));
            blink.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(1)));
            _eyeScale.BeginAnimation(ScaleTransform.ScaleYProperty, blink);
        }

        private void StopMoodAnimations()
        {
            _headRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            _headGroup.BeginAnimation(OpacityProperty, null);
            _leftArmRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            _rightArmRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            _bowlGroup.BeginAnimation(OpacityProperty, null);
            if (_spriteTilt != null) _spriteTilt.BeginAnimation(RotateTransform.AngleProperty, null);
            _speakTimer.Stop();
            _eatTimer.Stop();
        }

        public void SetMood(PetMood mood)
        {
            _mood = mood;
            _speaking = mood == PetMood.Speaking;
            StopMoodAnimations();
            ShowSprite(mood);

            _eyesNormal.Visibility = Visibility.Visible;
            _eyesHappy.Visibility = Visibility.Collapsed;
            _eyesSleepy.Visibility = Visibility.Collapsed;
            _eyesWide.Visibility = Visibility.Collapsed;
            _mouthSmile.Visibility = Visibility.Visible;
            _mouthOpen.Visibility = Visibility.Collapsed;
            _mouthWavy.Visibility = Visibility.Collapsed;
            _mouthSleep.Visibility = Visibility.Collapsed;
            _bowlGroup.Visibility = Visibility.Collapsed;
            _thoughtDots.Visibility = Visibility.Collapsed;
            _hearts.Visibility = Visibility.Collapsed;
            _stars.Visibility = Visibility.Collapsed;
            _zzz.Visibility = Visibility.Collapsed;
            _sweat.Visibility = Visibility.Collapsed;
            _surprise.Visibility = Visibility.Collapsed;
            _blushL.Opacity = 0.2;
            _blushR.Opacity = 0.2;
            _eyesNormal.Opacity = 1;
            _headGroup.Opacity = 1;

            switch (mood)
            {
                case PetMood.Listening:
                    TiltHead(3.0, 0.25);
                    TiltSprite(2.2, 0.30);
                    _eyeLook.X = 3; _eyeLook.Y = -2;
                    _leftArmRotate.Angle = -8;
                    _rightArmRotate.Angle = 8;
                    break;
                case PetMood.Thinking:
                    _eyesHappy.Visibility = Visibility.Collapsed;
                    _eyeLook.X = 4; _eyeLook.Y = -6;
                    TiltHead(-4.5, 0.5);
                    TiltSprite(-4.8, 0.55);
                    if (!_spriteAvailable) _thoughtDots.Visibility = Visibility.Visible;
                    _rightArmRotate.Angle = -32;
                    _leftArmRotate.Angle = 0;
                    _blushL.Opacity = 0.3; _blushR.Opacity = 0.3;
                    FastTail(0.9);
                    break;
                case PetMood.Speaking:
                    _mouthSmile.Visibility = Visibility.Collapsed;
                    _mouthOpen.Visibility = Visibility.Visible;
                    _eyeLook.X = 0; _eyeLook.Y = 0;
                    _speakStep = 0;
                    _speakTimer.Tick -= OnSpeakTick;
                    _speakTimer.Tick += OnSpeakTick;
                    _speakTimer.Start();
                    TiltHead(1.2, 0.2);
                    TiltSprite(-1.0, 0.25);
                    Bounce();
                    break;
                case PetMood.Happy:
                    _eyesNormal.Visibility = Visibility.Collapsed;
                    _eyesHappy.Visibility = Visibility.Visible;
                    _mouthSmile.Visibility = Visibility.Collapsed;
                    _mouthOpen.Visibility = Visibility.Visible;
                    _blushL.Opacity = 0.55; _blushR.Opacity = 0.55;
                    if (!_spriteAvailable) _hearts.Visibility = Visibility.Visible;
                    if (!_spriteAvailable) _stars.Visibility = Visibility.Visible;
                    _leftArmRotate.Angle = -10;
                    _rightArmRotate.Angle = 10;
                    TiltSprite(1.5, 0.3);
                    Bounce();
                    break;
                case PetMood.Eating:
                    _eyesNormal.Visibility = Visibility.Collapsed;
                    _eyesHappy.Visibility = Visibility.Visible;
                    _mouthSmile.Visibility = Visibility.Collapsed;
                    _mouthOpen.Visibility = Visibility.Visible;
                    if (!_spriteAvailable) _bowlGroup.Visibility = Visibility.Visible;
                    _leftArmRotate.Angle = 24;
                    _rightArmRotate.Angle = -24;
                    _eatStep = 0;
                    _eatTimer.Tick -= OnEatTick;
                    _eatTimer.Tick += OnEatTick;
                    _eatTimer.Start();
                    _blushL.Opacity = 0.45; _blushR.Opacity = 0.45;
                    TiltSprite(3.0, 0.35);
                    break;
                case PetMood.Shy:
                    _eyeLook.X = -5; _eyeLook.Y = 2;
                    _mouthSmile.Visibility = Visibility.Collapsed;
                    _mouthWavy.Visibility = Visibility.Visible;
                    _blushL.Opacity = 0.85; _blushR.Opacity = 0.85;
                    if (!_spriteAvailable) _sweat.Visibility = Visibility.Visible;
                    _leftArmRotate.Angle = 18;
                    _rightArmRotate.Angle = -18;
                    TiltHead(-2.0, 0.3);
                    TiltSprite(-2.4, 0.35);
                    break;
                case PetMood.Sleepy:
                    _eyesNormal.Visibility = Visibility.Collapsed;
                    _eyesSleepy.Visibility = Visibility.Visible;
                    _mouthSmile.Visibility = Visibility.Collapsed;
                    _mouthSleep.Visibility = Visibility.Visible;
                    if (!_spriteAvailable) _zzz.Visibility = Visibility.Visible;
                    _blushL.Opacity = 0.15; _blushR.Opacity = 0.15;
                    TiltHead(-3.0, 1.2);
                    TiltSprite(-3.4, 1.0);
                    SlowBreathe();
                    break;
                case PetMood.Surprised:
                    _eyesNormal.Visibility = Visibility.Collapsed;
                    _eyesWide.Visibility = Visibility.Visible;
                    _mouthSmile.Visibility = Visibility.Collapsed;
                    _mouthOpen.Visibility = Visibility.Visible;
                    if (!_spriteAvailable) _surprise.Visibility = Visibility.Visible;
                    _blushL.Opacity = 0.45; _blushR.Opacity = 0.45;
                    TiltSprite(0, 0.2);
                    Jump();
                    break;
                case PetMood.Sad:
                    TiltSprite(-2.6, 0.35);
                    IdleBob();
                    break;
                case PetMood.Angry:
                    TiltSprite(1.8, 0.3);
                    IdleBob();
                    break;
                case PetMood.Cry:
                    TiltSprite(-2.0, 0.4);
                    IdleBob();
                    break;
                case PetMood.Lying:
                    TiltSprite(0, 0.4);
                    SlowBreathe();
                    break;
                case PetMood.Received:
                    TiltSprite(0, 0.2);
                    Bounce();
                    break;
                case PetMood.Walking:
                    _eyeLook.X = 0; _eyeLook.Y = 0;
                    TiltHead(0, 0.2);
                    WalkBob();
                    break;
                default:
                    _eyeLook.X = 0; _eyeLook.Y = 0;
                    TiltHead(0, 0.4);
                    TiltSprite(0, 0.35);
                    IdleBob();
                    break;
            }
            if (_tongue != null) _tongue.Visibility = _mouthOpen.Visibility;
        }

        private void WalkBob()
        {
            var b = new DoubleAnimationUsingKeyFrames();
            b.Duration = TimeSpan.FromSeconds(0.40);
            b.RepeatBehavior = RepeatBehavior.Forever;
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromPercent(0.25)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.5)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromPercent(0.75)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            _bob.BeginAnimation(TranslateTransform.YProperty, b);

            var t = new DoubleAnimationUsingKeyFrames();
            t.Duration = TimeSpan.FromSeconds(0.40);
            t.RepeatBehavior = RepeatBehavior.Forever;
            t.KeyFrames.Add(new EasingDoubleKeyFrame(-2.0, KeyTime.FromPercent(0)));
            t.KeyFrames.Add(new EasingDoubleKeyFrame(1.5, KeyTime.FromPercent(0.5)));
            t.KeyFrames.Add(new EasingDoubleKeyFrame(-2.0, KeyTime.FromPercent(1)));
            _spriteTilt.BeginAnimation(RotateTransform.AngleProperty, t);
        }

        private void IdleBob()
        {
            var bob = new DoubleAnimation(0, -3.2, TimeSpan.FromSeconds(2.6));
            bob.AutoReverse = true;
            bob.RepeatBehavior = RepeatBehavior.Forever;
            bob.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _bob.BeginAnimation(TranslateTransform.YProperty, bob);
        }

        private void TiltSprite(double angle, double seconds)
        {
            if (_spriteTilt == null) return;
            var a = new DoubleAnimation(angle, TimeSpan.FromSeconds(seconds));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            _spriteTilt.BeginAnimation(RotateTransform.AngleProperty, a);
        }

        private void TiltHead(double angle, double seconds)
        {
            var a = new DoubleAnimation(angle, TimeSpan.FromSeconds(seconds));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            _headRotate.BeginAnimation(RotateTransform.AngleProperty, a);
        }

        private void FastTail(double seconds)
        {
            var tail = new DoubleAnimation(-6, 10, TimeSpan.FromSeconds(seconds));
            tail.AutoReverse = true; tail.RepeatBehavior = RepeatBehavior.Forever;
            tail.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _tailRotate.BeginAnimation(RotateTransform.AngleProperty, tail);
        }

        private void Bounce()
        {
            var b = new DoubleAnimationUsingKeyFrames();
            b.Duration = TimeSpan.FromSeconds(1.5);
            b.RepeatBehavior = RepeatBehavior.Forever;
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(-7, KeyTime.FromPercent(0.3)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.55)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            _bob.BeginAnimation(TranslateTransform.YProperty, b);
        }

        private void Jump()
        {
            var b = new DoubleAnimationUsingKeyFrames();
            b.Duration = TimeSpan.FromMilliseconds(650);
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(-18, KeyTime.FromPercent(0.35)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.7)));
            b.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            _bob.BeginAnimation(TranslateTransform.YProperty, b);
        }

        private void SlowBreathe()
        {
            var breathe = new DoubleAnimation(1, 1.02, TimeSpan.FromSeconds(5.2));
            breathe.AutoReverse = true; breathe.RepeatBehavior = RepeatBehavior.Forever;
            breathe.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            _breathe.BeginAnimation(ScaleTransform.ScaleYProperty, breathe);
            var bob = new DoubleAnimation(0, -1.6, TimeSpan.FromSeconds(5.0));
            bob.AutoReverse = true; bob.RepeatBehavior = RepeatBehavior.Forever;
            _bob.BeginAnimation(TranslateTransform.YProperty, bob);
        }

        private void OnSpeakTick(object sender, EventArgs e)
        {
            _speakStep++;
            _mouthScale.ScaleY = (_speakStep % 3 == 0) ? 0.55 : 1.0;
            _mouthScale.ScaleX = (_speakStep % 2 == 0) ? 0.94 : 1.05;
        }

        private void OnEatTick(object sender, EventArgs e)
        {
            _eatStep++;
            var head = new DoubleAnimation(8, TimeSpan.FromMilliseconds(220));
            _headRotate.BeginAnimation(RotateTransform.AngleProperty, head);
            var bowl = new DoubleAnimation(-12, TimeSpan.FromMilliseconds(220));
            _bowlMove.BeginAnimation(TranslateTransform.YProperty, bowl);
            var down = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(250) };
            down.Tick += delegate
            {
                down.Stop();
                if (_mood != PetMood.Eating) return;
                _headRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(260)));
                _bowlMove.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)));
            };
            down.Start();
        }

        public void PlayWave()
        {
            SetMood(PetMood.Happy);
            var hop = new DoubleAnimationUsingKeyFrames();
            hop.Duration = TimeSpan.FromSeconds(0.9);
            hop.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            hop.KeyFrames.Add(new EasingDoubleKeyFrame(-12, KeyTime.FromPercent(0.32)));
            hop.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.62)));
            hop.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            _bob.BeginAnimation(TranslateTransform.YProperty, hop);
        }

        public void SetSpeaking(bool speaking)
        {
            _speaking = speaking;
            if (speaking)
            {
                if (_mood != PetMood.Speaking) SetMood(PetMood.Speaking);
            }
            else if (_mood == PetMood.Speaking)
            {
                SetMood(PetMood.Idle);
            }
        }

        public bool HitTestPet(Point p)
        {
            if (_spriteAvailable)
            {
                // Cover the rendered sprite (plus a small margin), including hair and ears.
                return p.X >= 83 && p.X <= 276 && p.Y >= 169 && p.Y <= 362;
            }
            if (p.X < 20 || p.X > 340 || p.Y < 20 || p.Y > 390) return false;
            double dx = (p.X - 180) / 165.0;
            double dy = (p.Y - 200) / 205.0;
            return dx * dx + dy * dy <= 1.05;
        }
    }
}


