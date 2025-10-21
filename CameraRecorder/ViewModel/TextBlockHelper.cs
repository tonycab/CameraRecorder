using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraRecorder
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Text.RegularExpressions;
    using System.Diagnostics;


        public static class TextBlockHelper
        {
            // Regex pour détecter les liens http et chemins locaux (C:\... ou \\serveur\...)
            private static readonly Regex _urlRegex = new Regex(
                @"(http[s]?://[^\s]+|[a-zA-Z]:(\\[^\s]+)+|\\\\[^\s]+)",
                RegexOptions.Compiled);

            // Déclaration de la propriété attachée
            public static readonly DependencyProperty BindableTextProperty =
                DependencyProperty.RegisterAttached(
                    "BindableText",
                    typeof(string),
                    typeof(TextBlockHelper),
                    new PropertyMetadata(null, OnBindableTextChanged));

            public static void SetBindableText(TextBlock element, string value)
                => element.SetValue(BindableTextProperty, value);

            public static string GetBindableText(TextBlock element)
                => (string)element.GetValue(BindableTextProperty);

            private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is TextBlock tb)
                {
                    tb.Inlines.Clear();
                    string text = e.NewValue as string ?? "";

                    int lastIndex = 0;
                    foreach (Match match in _urlRegex.Matches(text))
                    {
                        if (match.Index > lastIndex)
                            tb.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));

                        try
                        {
                            var hyperlink = new Hyperlink(new Run(match.Value))
                            {
                                NavigateUri = new Uri(match.Value, UriKind.Absolute)
                            };
                            hyperlink.RequestNavigate += (s, ev) =>
                            {
                                Process.Start(new ProcessStartInfo(ev.Uri.AbsoluteUri) { UseShellExecute = true });
                            };
                            tb.Inlines.Add(hyperlink);
                        }
                        catch
                        {
                            tb.Inlines.Add(new Run(match.Value));
                        }

                        lastIndex = match.Index + match.Length;
                    }

                    if (lastIndex < text.Length)
                        tb.Inlines.Add(new Run(text.Substring(lastIndex)));
                }
            }
        }
    

}
