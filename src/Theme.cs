using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BlueFishPet
{
    public static class Ui
    {
        public static readonly FontFamily Font = new FontFamily("Microsoft YaHei UI, Segoe UI");
        public static readonly FontFamily Mono = new FontFamily("Cascadia Mono, Consolas, Microsoft YaHei UI");

        public static readonly Color Ink = C("#EAF4FF");
        public static readonly Color Muted = C("#93A9C9");
        public static readonly Color Cyan = C("#83E6FF");
        public static readonly Color Blue = C("#4C9BFF");
        public static readonly Color DeepBlue = C("#2C6FE8");
        public static readonly Color Navy = C("#111C34");
        public static readonly Color Pink = C("#FFA9BE");

        public static Color C(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        public static SolidColorBrush B(string hex)
        {
            var b = new SolidColorBrush(C(hex));
            b.Freeze();
            return b;
        }

        public static SolidColorBrush BA(string hex, byte alpha)
        {
            var c = C(hex);
            c.A = alpha;
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static LinearGradientBrush Linear(string a, string b, double angle)
        {
            var g = new LinearGradientBrush(C(a), C(b), angle);
            g.Freeze();
            return g;
        }

        public static RadialGradientBrush Radial(string center, string edge, double cx, double cy)
        {
            var g = new RadialGradientBrush();
            g.GradientStops.Add(new GradientStop(C(center), 0));
            g.GradientStops.Add(new GradientStop(C(edge), 1));
            g.Center = new Point(cx, cy);
            g.GradientOrigin = new Point(cx, cy);
            g.Freeze();
            return g;
        }

        public static DropShadowEffect Shadow(double blur, double opacity, double dy)
        {
            return new DropShadowEffect
            {
                BlurRadius = blur,
                ShadowDepth = dy,
                Direction = 270,
                Color = Colors.Black,
                Opacity = opacity,
                RenderingBias = RenderingBias.Performance
            };
        }

        public static DropShadowEffect Glow(Color color, double blur, double opacity)
        {
            return new DropShadowEffect
            {
                BlurRadius = blur,
                ShadowDepth = 0,
                Color = color,
                Opacity = opacity,
                RenderingBias = RenderingBias.Performance
            };
        }

        public static TextBlock T(string text, double size, Brush color, bool bold)
        {
            var t = new TextBlock();
            t.Text = text;
            t.FontFamily = Font;
            t.FontSize = size;
            t.Foreground = color;
            t.FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal;
            t.TextWrapping = TextWrapping.Wrap;
            TextOptions.SetTextFormattingMode(t, TextFormattingMode.Display);
            return t;
        }

        public static TextBlock T(string text, double size, string color, bool bold)
        {
            return T(text, size, B(color), bold);
        }

        public static Border Round(Brush bg, double radius)
        {
            var b = new Border();
            b.Background = bg;
            b.CornerRadius = new CornerRadius(radius);
            b.SnapsToDevicePixels = true;
            return b;
        }

        public static Border Round(Brush bg, double radius, Brush border, double thickness)
        {
            var b = Round(bg, radius);
            b.BorderBrush = border;
            b.BorderThickness = new Thickness(thickness);
            return b;
        }

        public static Grid Stack(params UIElement[] children)
        {
            var g = new Grid();
            foreach (var c in children) g.Children.Add(c);
            return g;
        }

        public static StackPanel VStack(double gap)
        {
            var p = new StackPanel();
            p.Orientation = Orientation.Vertical;
            return p;
        }

        public static StackPanel HStack(double gap)
        {
            var p = new StackPanel();
            p.Orientation = Orientation.Horizontal;
            return p;
        }

        public static void Margin(UIElement e, double l, double t, double r, double b)
        {
            e.SetValue(FrameworkElement.MarginProperty, new Thickness(l, t, r, b));
        }

        public static Border PrimaryButton(string text, double height)
        {
            var b = new PillButton(text, true);
            b.Height = height;
            return b;
        }

        public static Border GhostButton(string text, double height)
        {
            var b = new PillButton(text, false);
            b.Height = height;
            return b;
        }

        public static Ellipse Dot(string color, double size)
        {
            var e = new Ellipse();
            e.Width = size;
            e.Height = size;
            e.Fill = B(color);
            return e;
        }

        public static Geometry Geo(string data)
        {
            return Geometry.Parse(data);
        }

        public static Path P(string data, Brush fill)
        {
            var p = new Path();
            p.Data = Geo(data);
            p.Fill = fill;
            p.StrokeThickness = 0;
            p.StrokeStartLineCap = PenLineCap.Round;
            p.StrokeEndLineCap = PenLineCap.Round;
            p.StrokeLineJoin = PenLineJoin.Round;
            p.SnapsToDevicePixels = false;
            return p;
        }

        public static Path Stroke(string data, Brush stroke, double width)
        {
            var p = new Path();
            p.Data = Geo(data);
            p.Stroke = stroke;
            p.StrokeThickness = width;
            p.StrokeStartLineCap = PenLineCap.Round;
            p.StrokeEndLineCap = PenLineCap.Round;
            p.StrokeLineJoin = PenLineJoin.Round;
            p.Fill = null;
            p.SnapsToDevicePixels = false;
            return p;
        }

        public static void SetPos(UIElement e, double x, double y)
        {
            Canvas.SetLeft(e, x);
            Canvas.SetTop(e, y);
        }

        public static void InstallScrollBarStyle()
        {
            const string xaml = @"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='{x:Type ScrollBar}'>
  <Setter Property='Width' Value='7'/>
  <Setter Property='Background' Value='Transparent'/>
  <Setter Property='Template'>
    <Setter.Value>
      <ControlTemplate TargetType='{x:Type ScrollBar}'>
        <Grid Background='Transparent' Width='7'>
          <Track x:Name='PART_Track' IsDirectionReversed='True'
                 Orientation='{TemplateBinding Orientation}'>
            <Track.DecreaseRepeatButton>
              <RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='False'/>
            </Track.DecreaseRepeatButton>
            <Track.Thumb>
              <Thumb MinHeight='26'>
                <Thumb.Template>
                  <ControlTemplate TargetType='{x:Type Thumb}'>
                    <Border Background='#48698D' CornerRadius='3' Margin='1,2'/>
                  </ControlTemplate>
                </Thumb.Template>
              </Thumb>
            </Track.Thumb>
            <Track.IncreaseRepeatButton>
              <RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='False'/>
            </Track.IncreaseRepeatButton>
          </Track>
        </Grid>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>";
            var style = (Style)System.Windows.Markup.XamlReader.Parse(xaml);
            if (Application.Current != null)
                Application.Current.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = style;
        }
    }

    public class PillButton : Border
    {
        private TextBlock _label;
        private bool _primary;
        private bool _hover;
        private bool _pressed;
        private bool _enabled = true;

        public event EventHandler Clicked;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                Opacity = value ? 1 : 0.45;
                Cursor = value ? Cursors.Hand : Cursors.Arrow;
            }
        }

        public string Text
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        public PillButton(string text, bool primary)
        {
            _primary = primary;
            Cursor = Cursors.Hand;
            CornerRadius = new CornerRadius(13);
            Padding = new Thickness(16, 0, 16, 0);
            _label = Ui.T(text, 12.5, primary ? "#08152C" : "#D7E9FF", true);
            _label.TextAlignment = TextAlignment.Center;
            _label.HorizontalAlignment = HorizontalAlignment.Center;
            _label.VerticalAlignment = VerticalAlignment.Center;
            Child = _label;
            Apply();
            MouseEnter += delegate { _hover = true; Apply(); };
            MouseLeave += delegate { _hover = false; _pressed = false; Apply(); };
            MouseLeftButtonDown += delegate { _pressed = true; Apply(); };
            MouseLeftButtonUp += delegate
            {
                bool was = _pressed;
                _pressed = false;
                Apply();
                if (was && _enabled && Clicked != null) Clicked(this, EventArgs.Empty);
            };
        }

        private void Apply()
        {
            if (!_enabled)
            {
                Background = Ui.BA(_primary ? "#7FE5FF" : "#253A58", 90);
                return;
            }
            if (_primary)
            {
                Background = Ui.Linear(_pressed ? "#5CC9E9" : (_hover ? "#B9F4FF" : "#8FE9FF"),
                                       _pressed ? "#2C90D9" : (_hover ? "#70C9FF" : "#4FA6F5"), 90);
                Effect = _hover ? Ui.Glow(Ui.C("#67D9FF"), 18, 0.45) : null;
            }
            else
            {
                Background = Ui.BA(_pressed ? "#3A547A" : (_hover ? "#2C4160" : "#1C2C48"), 225);
                BorderBrush = Ui.BA("#77B8E8", _hover ? (byte)130 : (byte)70);
                BorderThickness = new Thickness(1);
            }
        }
    }

    public class ToggleSwitch : Grid
    {
        private Border _track;
        private Ellipse _knob;
        private bool _checked;

        public event EventHandler Changed;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Apply();
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }

        public ToggleSwitch()
        {
            Width = 42;
            Height = 24;
            Cursor = Cursors.Hand;
            _track = new Border();
            _track.CornerRadius = new CornerRadius(12);
            _track.BorderThickness = new Thickness(1);
            Children.Add(_track);
            _knob = new Ellipse();
            _knob.Width = 16;
            _knob.Height = 16;
            _knob.HorizontalAlignment = HorizontalAlignment.Left;
            _knob.VerticalAlignment = VerticalAlignment.Center;
            _knob.Margin = new Thickness(4, 0, 0, 0);
            _knob.Fill = Ui.B("#F4FBFF");
            _knob.Effect = Ui.Shadow(6, 0.35, 1);
            Children.Add(_knob);
            MouseLeftButtonUp += delegate { Checked = !Checked; };
            Apply();
        }

        private void Apply()
        {
            if (_checked)
            {
                _track.Background = Ui.Linear("#55C8E9", "#3678E9", 0);
                _track.BorderBrush = Ui.BA("#A5EEFF", 150);
                _knob.Margin = new Thickness(21, 0, 0, 0);
            }
            else
            {
                _track.Background = Ui.BA("#1B2C46", 230);
                _track.BorderBrush = Ui.BA("#6E8CB2", 90);
                _knob.Margin = new Thickness(4, 0, 0, 0);
            }
        }
    }

    public class InputField : Border
    {
        public TextBox Box;
        public PasswordBox Password;
        private TextBlock _hint;

        public string Text
        {
            get { return Box != null ? Box.Text : (Password != null ? Password.Password : ""); }
            set
            {
                if (Box != null) Box.Text = value;
                else if (Password != null) Password.Password = value;
                if (_hint != null) _hint.Visibility = string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public InputField(string hint, bool password, bool multiline)
        {
            Background = Ui.BA("#0B1528", 230);
            BorderBrush = Ui.BA("#4C6C96", 100);
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(11);
            Padding = new Thickness(11, 8, 11, 8);
            var grid = new Grid();
            if (password)
            {
                Password = new PasswordBox();
                Password.Background = Brushes.Transparent;
                Password.BorderThickness = new Thickness(0);
                Password.Foreground = Ui.B("#EAF4FF");
                Password.CaretBrush = Ui.B("#8FE9FF");
                Password.FontFamily = Ui.Font;
                Password.FontSize = 13;
                Password.PasswordChar = '\u25CF';
                Password.Padding = new Thickness(0);
                grid.Children.Add(Password);
            }
            else
            {
                Box = new TextBox();
                Box.Background = Brushes.Transparent;
                Box.BorderThickness = new Thickness(0);
                Box.Foreground = Ui.B("#EAF4FF");
                Box.CaretBrush = Ui.B("#8FE9FF");
                Box.FontFamily = Ui.Font;
                Box.FontSize = 13;
                Box.Padding = new Thickness(0);
                Box.AcceptsReturn = multiline;
                Box.TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap;
                Box.VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                grid.Children.Add(Box);
            }
            _hint = Ui.T(hint, 12.5, "#6C86A8", false);
            _hint.IsHitTestVisible = false;
            _hint.VerticalAlignment = VerticalAlignment.Center;
            _hint.Margin = new Thickness(1, 0, 0, 0);
            grid.Children.Add(_hint);
            Child = grid;
            if (Box != null)
            {
                Box.TextChanged += delegate { _hint.Visibility = string.IsNullOrEmpty(Box.Text) ? Visibility.Visible : Visibility.Collapsed; };
                Box.GotFocus += delegate { BorderBrush = Ui.BA("#6FD8FF", 200); };
                Box.LostFocus += delegate { BorderBrush = Ui.BA("#4C6C96", 100); };
            }
            if (Password != null)
            {
                Password.PasswordChanged += delegate { _hint.Visibility = string.IsNullOrEmpty(Password.Password) ? Visibility.Visible : Visibility.Collapsed; };
                Password.GotFocus += delegate { BorderBrush = Ui.BA("#6FD8FF", 200); };
                Password.LostFocus += delegate { BorderBrush = Ui.BA("#4C6C96", 100); };
            }
        }
    }
}


