using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace BlueFishPet
{
    public static class Markdown
    {
        private static readonly Regex Inline = new Regex(
            @"(\*\*[^*]+\*\*|__[^_]+__|`[^`]+`|~~[^~]+~~|\[[^\]]+\]\([^)]+\))",
            RegexOptions.Compiled);

        public static void Render(string text, StackPanel target)
        {
            target.Children.Clear();
            if (text == null) return;
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var paragraph = new List<string>();
            bool inCode = false;
            string codeLang = "";
            var code = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("```"))
                {
                    if (!inCode)
                    {
                        FlushParagraph(paragraph, target);
                        inCode = true;
                        codeLang = trimmed.Substring(3).Trim();
                        code.Clear();
                    }
                    else
                    {
                        AddCodeBlock(code, codeLang, target);
                        inCode = false;
                    }
                    continue;
                }
                if (inCode)
                {
                    code.Add(line);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph(paragraph, target);
                    continue;
                }
                if (trimmed.StartsWith("#"))
                {
                    FlushParagraph(paragraph, target);
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    string heading = trimmed.Substring(level).Trim();
                    if (heading.Length > 0)
                    {
                        var ht = Ui.T("", level <= 2 ? 16 : 14, "#F2FAFF", true);
                        ht.Margin = new Thickness(0, 5, 0, 3);
                        AddInlines(ht, heading);
                        target.Children.Add(ht);
                    }
                    continue;
                }
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
                {
                    FlushParagraph(paragraph, target);
                    var lt = Ui.T("", 13, "#E4F1FF", false);
                    lt.Margin = new Thickness(2, 1, 0, 1);
                    AddInlines(lt, "•  " + trimmed.Substring(2));
                    target.Children.Add(lt);
                    continue;
                }
                paragraph.Add(line);
            }
            if (inCode) AddCodeBlock(code, codeLang, target);
            FlushParagraph(paragraph, target);
        }

        private static void FlushParagraph(List<string> lines, StackPanel target)
        {
            if (lines.Count == 0) return;
            var tb = Ui.T("", 13, "#EAF4FF", false);
            tb.LineHeight = 20;
            tb.Margin = new Thickness(0, 1, 0, 3);
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) tb.Inlines.Add(new LineBreak());
                AddInlines(tb, lines[i]);
            }
            target.Children.Add(tb);
            lines.Clear();
        }

        private static void AddInlines(TextBlock block, string text)
        {
            int index = 0;
            foreach (Match m in Inline.Matches(text))
            {
                if (m.Index > index)
                    block.Inlines.Add(new Run(text.Substring(index, m.Index - index)));
                string token = m.Value;
                if (token.StartsWith("**") && token.EndsWith("**") && token.Length > 4)
                {
                    block.Inlines.Add(new Bold(new Run(token.Substring(2, token.Length - 4))));
                }
                else if (token.StartsWith("__") && token.EndsWith("__") && token.Length > 4)
                {
                    block.Inlines.Add(new Bold(new Run(token.Substring(2, token.Length - 4))));
                }
                else if (token.StartsWith("`") && token.EndsWith("`") && token.Length > 2)
                {
                    var r = new Run(token.Substring(1, token.Length - 2));
                    r.FontFamily = Ui.Mono;
                    r.Foreground = Ui.B("#9FE9FF");
                    r.Background = Ui.BA("#2A5C86", 70);
                    block.Inlines.Add(r);
                }
                else if (token.StartsWith("~~") && token.EndsWith("~~") && token.Length > 4)
                {
                    var r = new Run(token.Substring(2, token.Length - 4));
                    r.TextDecorations = TextDecorations.Strikethrough;
                    block.Inlines.Add(r);
                }
                else if (token.StartsWith("["))
                {
                    int split = token.IndexOf("](");
                    string label = token.Substring(1, split - 1);
                    string url = token.Substring(split + 2, token.Length - split - 3);
                    var link = new Hyperlink(new Run(label));
                    link.Foreground = Ui.B("#7FE7FF");
                    link.TextDecorations = TextDecorations.Underline;
                    try { link.NavigateUri = new Uri(url); } catch { }
                    link.ToolTip = url;
                    link.RequestNavigate += delegate(object s, System.Windows.Navigation.RequestNavigateEventArgs e)
                    {
                        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
                        catch { }
                        e.Handled = true;
                    };
                    block.Inlines.Add(link);
                }
                index = m.Index + m.Length;
            }
            if (index < text.Length) block.Inlines.Add(new Run(text.Substring(index)));
        }

        private static void AddCodeBlock(List<string> lines, string lang, StackPanel target)
        {
            if (lines.Count == 0 || string.IsNullOrEmpty(string.Join("", lines.ToArray()).Trim()))
                return;
            var border = new Border();
            border.Background = Ui.BA("#081222", 245);
            border.BorderBrush = Ui.BA("#34577D", 110);
            border.BorderThickness = new Thickness(1);
            border.CornerRadius = new CornerRadius(9);
            border.Padding = new Thickness(10, 8, 10, 9);
            border.Margin = new Thickness(0, 4, 0, 5);
            border.MaxWidth = 278;
            border.HorizontalAlignment = HorizontalAlignment.Left;
            var tb = Ui.T(string.Join("\n", lines.ToArray()), 11.2, "#D8ECFF", false);
            tb.FontFamily = Ui.Mono;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.LineHeight = 16;
            tb.MaxWidth = 256;
            border.Child = tb;
            target.Children.Add(border);
        }
    }
}

