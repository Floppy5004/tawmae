/*
Copyright (c) 2025 tawmae. All rights reserved.

This DLL is licensed for non-commercial use only.
Redistribution and use in binary forms, with or without modification, are permitted for non-commercial purposes only, provided that the following conditions are met:

1. Proper attribution must be given to "tawmae" with reference to the website "www.tawmae.xyz".
2. Commercial use, including integration into paid products or services, is strictly prohibited without explicit written permission.
3. This notice must be included in all copies or substantial portions of the Software.

For commercial licensing, please contact: tawmae@pm.me

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using SWC = System.Windows.Controls;

namespace TawmaeUI
{
    public class Tawmae
    {
        private static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version
            is Version v
            ? $"{v.Major}.{v.Minor}.{v.Build}"
            : "huh???";

        private readonly IInlineInvokeProxy _cph;
        private bool _withUi = true;
        public FluentWindow _window;
        private ListBox _sidebar;
        private ContentControl _contentArea;
        private Dictionary<string, StackPanel> _tabContentPanels;
        private SWC.TextBlock _statusText;
        private SWC.TextBlock _footerLink;
        private string _settingsKey = "tawmae_Settings_UI";
        private const string WinWidthKey = "WindowWidth";
        private const string WinHeightKey = "WindowHeight";
        private JObject _existingSettings;
        private readonly string _originalSettingsJson;
        private static bool _isOpen = false;
        public string _titleSuffix;
        private JObject _defaultValues;
        private Grid _mainGrid;
        private List<string> _passwordKeys = new List<string>();
        private Dictionary<string, ComboBox> _dropdowns = new Dictionary<string, ComboBox>();
        private Dictionary<string, StackPanel> _dynamicPanels =
            new Dictionary<string, StackPanel>();
        private readonly string _displayVersion;
        private readonly Dictionary<string, double> _tabScrollOffsets =
            new Dictionary<string, double>();
        private readonly Dictionary<string, Action> _updateDateTimeHidden =
            new Dictionary<string, Action>();

        public string GetDisplayVersion() => _displayVersion;

        private readonly Dictionary<string, FrameworkElement> _controlMap =
            new Dictionary<string, FrameworkElement>();

        public Tawmae(
            IInlineInvokeProxy cph,
            string settingsTitle = "default",
            string displayVersion = null,
            bool withUi = true
        )
        {
            _cph = cph;
            _withUi = withUi;
            _titleSuffix = settingsTitle;
            _displayVersion = string.IsNullOrWhiteSpace(displayVersion) ? Version : displayVersion;
            _settingsKey = $"tawmae_Settings_{settingsTitle}";
            string json = _cph.GetGlobalVar<string>(_settingsKey, true) ?? "{}";
            _originalSettingsJson = json;
            _existingSettings = JObject.Parse(json);
            _defaultValues = BuildDefaults();
            if (_withUi)
            {
                if (_isOpen)
                {
                    _cph.LogInfo(
                        $"Tawmae UI ({settingsTitle} (v{GetDisplayVersion()})) [{Version}]: Already opened, skipping..."
                    );
                    return;
                }
                _isOpen = true;
                InitializeUI();
            }
        }

        public void LogExistingSettings()
        {
            string sanitizedJson = REDACTED(_existingSettings);
            _cph.LogInfo(
                $"Tawmae UI ({_titleSuffix} (v{GetDisplayVersion()})) [{Version}]: Opening UI with existing settings:"
            );
            _cph.LogInfo(sanitizedJson);
        }

        private JObject BuildDefaults()
        {
            return new JObject();
        }

        private void InitializeUI()
        {
            Window splash = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4b6aa1")),
                Width = 300,
                Height = 100,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true,
                Content = new Grid
                {
                    Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4b6aa1")),
                    Children =
                    {
                        new SWC.TextBlock
                        {
                            Text = "Opening UI...",
                            Foreground = Brushes.White,
                            FontSize = 16,

                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontFamily = new FontFamily(
                                new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                                "#Poppins"
                            ),
                        },
                    },
                },
            };
            splash.Show();
            splash.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            _window = new FluentWindow();
            _window.Title = $"tawmae - {_titleSuffix} v{GetDisplayVersion()}";
            _window.Width = _existingSettings[WinWidthKey]?.Value<double?>() ?? 800;
            _window.Height = _existingSettings[WinHeightKey]?.Value<double?>() ?? 600;
            _window.ResizeMode = ResizeMode.CanMinimize;
            _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _window.Closed += (s, e) =>
            {
                var json = _cph.GetGlobalVar<string>(_settingsKey, true) ?? "{}";
                var settings = JObject.Parse(json);
                if (_window.WindowState == WindowState.Normal)
                {
                    settings[WinWidthKey] = _window.Width;
                    settings[WinHeightKey] = _window.Height;
                }
                _cph.SetGlobalVar(_settingsKey, settings.ToString(Formatting.None), true);
                _isOpen = false;
                _cph.LogInfo(
                    $"Tawmae UI ({_titleSuffix} (v{GetDisplayVersion()})) [{Version}] has been closed. Saved settings:"
                );
                string closedData = _cph.GetGlobalVar<string>(_settingsKey, true);
                if (!string.IsNullOrEmpty(closedData))
                {
                    try
                    {
                        var parsed = JObject.Parse(closedData);
                        string masked = REDACTED(parsed);
                        _cph.LogInfo(masked);
                    }
                    catch
                    {
                        _cph.LogInfo(closedData);
                    }
                }
            };

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
            ApplicationThemeManager.Apply(_window);
            _mainGrid = new Grid { Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)) };
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            );
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TawmaeExtensions.AddCustomTitleBar(this, _mainGrid);
            Grid.SetRow(_mainGrid.Children[_mainGrid.Children.Count - 1], 0);
            _contentArea = new ScrollViewer
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            var innerGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            };
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            innerGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );
            _sidebar = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
            };
            var itemStyle = new Style(typeof(ListBoxItem));
            itemStyle.Setters.Add(
                new Setter(
                    Control.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(32, 32, 32))
                )
            );
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(
                new Setter(
                    Control.FontFamilyProperty,
                    new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    )
                )
            );
            itemStyle.Setters.Add(new Setter(Control.FontSizeProperty, 16.0));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10)));
            var trigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            trigger.Setters.Add(
                new Setter(
                    Control.BackgroundProperty,
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#476393"))
                )
            );
            itemStyle.Triggers.Add(trigger);
            _sidebar.ItemContainerStyle = itemStyle;
            _sidebar.SelectionChanged += (s, e) =>
            {
                if (
                    e.RemovedItems.Count > 0
                    && e.RemovedItems[0] is ListBoxItem oldItem
                    && oldItem.Tag is string oldTab
                    && _contentArea is ScrollViewer svOld
                )
                {
                    _tabScrollOffsets[oldTab] = svOld.VerticalOffset;
                }
                if (_sidebar.SelectedItem is ListBoxItem item && item.Tag is string newTab)
                {
                    if (_tabContentPanels.TryGetValue(newTab, out var panel))
                    {
                        _contentArea.Content = panel;

                        if (_contentArea is ScrollViewer svNew)
                        {
                            double offset = _tabScrollOffsets.TryGetValue(newTab, out var saved)
                                ? saved
                                : 0;
                            svNew.ScrollToVerticalOffset(offset);
                        }
                    }
                }
            };
            Grid.SetColumn(_sidebar, 0);
            Grid.SetColumn(_contentArea, 1);
            innerGrid.Children.Add(_sidebar);
            innerGrid.Children.Add(_contentArea);
            Grid.SetRow(innerGrid, 2);
            _mainGrid.Children.Add(innerGrid);

            var buttonPanel = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10),
            };
            var footerBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(0, 0.7, 0, 0),
            };
            footerBorder.Child = buttonPanel;
            buttonPanel.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );
            buttonPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _footerLink = new SWC.TextBlock
            {
                Text = $"{GetVersion()} - Copyright © 2025 tawmae. All rights reserved.",
                Foreground = Brushes.Gray,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap,
            };
            var link = new Hyperlink
            {
                NavigateUri = new Uri("https://www.tawmae.xyz"),
                Foreground = Brushes.LightBlue,
            };
            link.RequestNavigate += (s, e) =>
            {
                try
                {
                    Process.Start(
                        new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }
                    );
                }
                catch { }
            };
            link.Inlines.Add("www.tawmae.xyz");
            var linkTextBlock = new SWC.TextBlock
            {
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 12,
                Foreground = Brushes.LightBlue,
                TextWrapping = TextWrapping.Wrap,
            };
            linkTextBlock.Inlines.Add(link);
            var footerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };
            footerStack.Children.Add(_footerLink);
            footerStack.Children.Add(linkTextBlock);
            Grid.SetColumn(footerStack, 0);
            Grid.SetRow(footerStack, 0);
            buttonPanel.Children.Add(footerStack);
            var statusStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            _statusText = new SWC.TextBlock
            {
                Text = "",
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap,
            };
            statusStack.Children.Add(_statusText);
            var saveButton = new Wpf.Ui.Controls.Button
            {
                Content = "Save",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#71a079")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
            };
            saveButton.Click += SaveButton_Click;
            statusStack.Children.Add(saveButton);
            var saveExitButton = new Wpf.Ui.Controls.Button
            {
                Content = "Save and Exit",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#71a079")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
            };
            saveExitButton.Click += SaveExitButton_Click;
            statusStack.Children.Add(saveExitButton);
            var resetButton = new Wpf.Ui.Controls.Button
            {
                Content = "Reset",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#ffb347")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
            };
            resetButton.Click += ResetButton_Click;
            statusStack.Children.Add(resetButton);
            var exitButton = new Wpf.Ui.Controls.Button
            {
                Content = "Exit",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#d9534f")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Padding = new Thickness(12, 6, 12, 6),
            };
            exitButton.Click += ExitButton_Click;
            statusStack.Children.Add(exitButton);
            Grid.SetColumn(statusStack, 4);
            Grid.SetRow(statusStack, 0);
            buttonPanel.Children.Add(statusStack);
            Grid.SetRow(footerBorder, 3);
            _mainGrid.Children.Add(footerBorder);
            _window.Content = _mainGrid;
            _tabContentPanels = new Dictionary<string, StackPanel>();
            AddHelpTab();
            splash.Close();
        }

        public void AddTitle(string text, string tabName)
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            var titleTextBlock = new SWC.TextBlock
            {
                Text = text,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(20, 10, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            parentPanel.Children.Add(titleTextBlock);
            AddSeparator(parentPanel);
        }

        public void AddDescription(string text, string tabName)
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var parentPanel = _tabContentPanels[tabName];

            var block = new SWC.TextBlock
            {
                FontSize = 16,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };

            foreach (Inline i in ParseRich(text))
                block.Inlines.Add(i);

            parentPanel.Children.Add(block);
            AddSeparator(parentPanel);

            IEnumerable<Inline> ParseRich(string raw)
            {
                var list = new List<Inline>();
                var rx = new Regex(
                    @"(\*\*(?<bold>.+?)\*\*)"
                        + @"|_(?<italic>.+?)_"
                        + @"|\{(?<col>#[0-9a-fA-F]{6,8})\|(?<colTxt>.+?)\}(?!\})"
                        + @"|\{size:(?<size>\d+)\|(?<sizeTxt>.+?)\}(?!\})"
                        + @"|\+\+(?<glow>.+?)\+\+"
                        + @"|\[link:(?<disp>[^\|\]]+)\|(?<url>[^\]]+)\]",
                    RegexOptions.Singleline
                );
                int idx = 0;

                foreach (Match m in rx.Matches(raw))
                {
                    if (m.Index > idx)
                        AddPlain(raw.Substring(idx, m.Index - idx));

                    if (m.Groups["bold"].Success)
                    {
                        var span = new Span();
                        foreach (var inner in ParseRich(m.Groups["bold"].Value))
                            span.Inlines.Add(inner);
                        span.FontWeight = FontWeights.Bold;
                        list.Add(span);
                    }
                    else if (m.Groups["italic"].Success)
                    {
                        var span = new Span();
                        foreach (var inner in ParseRich(m.Groups["italic"].Value))
                            span.Inlines.Add(inner);
                        span.FontStyle = FontStyles.Italic;
                        list.Add(span);
                    }
                    else if (m.Groups["col"].Success)
                    {
                        var span = new Span();
                        foreach (var inner in ParseRich(m.Groups["colTxt"].Value))
                            span.Inlines.Add(inner);
                        span.Foreground = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString(m.Groups["col"].Value)
                        );
                        list.Add(span);
                    }
                    else if (m.Groups["size"].Success)
                    {
                        var span = new Span();
                        foreach (var inner in ParseRich(m.Groups["sizeTxt"].Value))
                            span.Inlines.Add(inner);
                        span.FontSize = double.Parse(m.Groups["size"].Value);
                        list.Add(span);
                    }
                    else if (m.Groups["glow"].Success)
                    {
                        var glowTb = new System.Windows.Controls.TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new FontFamily(
                                new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                                "#Poppins"
                            ),
                            FontSize = 16,
                            Foreground = Brushes.White,
                        };
                        foreach (var inner in ParseRich(m.Groups["glow"].Value))
                            glowTb.Inlines.Add(inner);
                        Color glowColor = Colors.White;
                        Brush firstBrush = GetFirstForeground(glowTb.Inlines);
                        if (firstBrush is SolidColorBrush scb)
                            glowColor = scb.Color;
                        glowTb.Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = glowColor,
                            BlurRadius = 8,
                            ShadowDepth = 0,
                            Opacity = 0.8,
                        };
                        list.Add(
                            new InlineUIContainer(glowTb)
                            {
                                BaselineAlignment = BaselineAlignment.Center,
                            }
                        );
                    }
                    else if (m.Groups["disp"].Success)
                    {
                        var run = new Run(m.Groups["disp"].Value)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.LightBlue,
                        };
                        var link = new Hyperlink(run)
                        {
                            NavigateUri = new Uri(m.Groups["url"].Value),
                        };
                        link.RequestNavigate += (s, e) =>
                            Process.Start(
                                new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }
                            );
                        list.Add(link);
                    }

                    idx = m.Index + m.Length;
                }

                if (idx < raw.Length)
                    AddPlain(raw.Substring(idx));

                return list;

                void AddPlain(string segment)
                {
                    foreach (Inline i in ParseInlines(segment))
                        list.Add(i);
                }

                Brush GetFirstForeground(InlineCollection inl)
                {
                    foreach (Inline il in inl)
                    {
                        if (il is Run r && r.Foreground is SolidColorBrush sb)
                            return sb;
                        if (il is Span sp && sp.Foreground is SolidColorBrush sb2)
                            return sb2;
                        if (il is Span sp2)
                        {
                            var b = GetFirstForeground(sp2.Inlines);
                            if (b != null)
                                return b;
                        }
                    }
                    return null;
                }
            }
        }

        public void AddWebView(string url, string tabName)
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var container = new Grid
            {
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            container.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            );
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            var heightBinding = new System.Windows.Data.Binding("ActualHeight")
            {
                Source = _contentArea,
            };
            container.SetBinding(FrameworkElement.HeightProperty, heightBinding);

            var webView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                Source = new Uri(url),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetRow(webView, 0);

            container.Children.Add(webView);
            _tabContentPanels[tabName].Children.Add(container);
        }

        public void AddHelpTab()
        {
            EnsureTabExists("Help");

            AddTitle("Need help?", "Help");

            var helpDescription = new SWC.TextBlock
            {
                FontSize = 16,
                Foreground = Brushes.Gray,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            helpDescription.Inlines.Add(
                new Run(
                    "In case you run into issues, join my Discord server and create a support post: "
                )
            );
            var discordLink = new Hyperlink(new Run("www.tawmae.xyz/discord"))
            {
                NavigateUri = new Uri("https://www.tawmae.xyz/discord"),
                Foreground = Brushes.LightBlue,
            };
            discordLink.RequestNavigate += (s, e) =>
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            helpDescription.Inlines.Add(new LineBreak());
            helpDescription.Inlines.Add(discordLink);
            _tabContentPanels["Help"].Children.Add(helpDescription);
            AddSeparator(_tabContentPanels["Help"]);

            AddTitle("How to get your Streamer.bot logs:", "Help");

            var logsDescription = new SWC.TextBlock
            {
                FontSize = 16,
                Foreground = Brushes.Gray,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            logsDescription.Inlines.Add(
                new Run("You can find your logs in your Streamer.bot directory.")
            );
            logsDescription.Inlines.Add(new LineBreak());
            logsDescription.Inlines.Add(
                new Run("Just click on the path below and drag the latest logs into Discord:")
            );
            _tabContentPanels["Help"].Children.Add(logsDescription);
            AddSeparator(_tabContentPanels["Help"]);

            string logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            var logsLinkBlock = new SWC.TextBlock
            {
                FontSize = 16,
                Foreground = Brushes.LightBlue,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            var logsLink = new Hyperlink(new Run(logsPath)) { NavigateUri = new Uri(logsPath) };
            logsLink.RequestNavigate += (s, e) =>
                Process.Start(new ProcessStartInfo(logsPath) { UseShellExecute = true });
            logsLinkBlock.Inlines.Add(logsLink);
            _tabContentPanels["Help"].Children.Add(logsLinkBlock);
            AddSeparator(_tabContentPanels["Help"]);

            AddTitle("Like what I do?", "Help");

            var likeText = new SWC.TextBlock
            {
                FontSize = 16,
                Foreground = Brushes.Gray,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
                Text =
                    "All of my extensions are free for everyone to use and will forever be. If you like what I'm doing, an Iced Latte is always appreciated <3",
            };
            _tabContentPanels["Help"].Children.Add(likeText);
            AddSeparator(_tabContentPanels["Help"]);

            var coffeeImg = new SWC.Image
            {
                Width = 180,
                Height = 60,
                Stretch = Stretch.Uniform,
                Cursor = Cursors.Hand,
                Margin = new Thickness(20, 0, 20, 10),
            };
            try
            {
                using (var wc = new WebClient())
                {
                    byte[] data = wc.DownloadData("https://storage.ko-fi.com/cdn/kofi5.png");
                    using (var ms = new MemoryStream(data))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();
                        coffeeImg.Source = bmp;
                    }
                }
            }
            catch { }

            coffeeImg.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = new ScaleTransform(1.0, 1.0);
            coffeeImg.RenderTransform = scale;
            coffeeImg.MouseEnter += (s, e) =>
            {
                scale.ScaleX = 1.1;
                scale.ScaleY = 1.1;
            };
            coffeeImg.MouseLeave += (s, e) =>
            {
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
            };
            coffeeImg.MouseLeftButtonUp += (s, e) =>
                Process.Start(
                    new ProcessStartInfo("https://ko-fi.com/tawmae") { UseShellExecute = true }
                );

            _tabContentPanels["Help"].Children.Add(coffeeImg);
            AddSeparator(_tabContentPanels["Help"]);
        }

        public void CheckPresentViewersSettings(
            string title,
            string description,
            string buttonColor,
            string tabName,
            string saveKey
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var panel = _tabContentPanels[tabName];
            panel.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    FontSize = 20,
                    Margin = new Thickness(20, 10, 20, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            panel.Children.Add(
                new SWC.TextBlock
                {
                    Text = description,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(20, 0, 20, 10),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            var summary = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 20, 5),
            };
            var pvLabel = new SWC.TextBlock
            {
                Text = "Present Viewer: ",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Foreground = Brushes.White,
                FontSize = 14,
            };
            var pvStatus = new SWC.TextBlock
            {
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
            };
            var sep1 = new SWC.TextBlock
            {
                Text = "  -  ",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Foreground = Brushes.White,
                FontSize = 14,
            };
            var luLabel = new SWC.TextBlock
            {
                Text = "Live Update: ",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Foreground = Brushes.White,
                FontSize = 14,
            };
            var luStatus = new SWC.TextBlock
            {
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
            };
            var sep2 = new SWC.TextBlock
            {
                Text = "  -  ",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Foreground = Brushes.White,
                FontSize = 14,
            };
            var intervalBlock = new SWC.TextBlock
            {
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Foreground = Brushes.White,
                FontSize = 14,
            };
            summary.Children.Add(pvLabel);
            summary.Children.Add(pvStatus);
            summary.Children.Add(sep1);
            summary.Children.Add(luLabel);
            summary.Children.Add(luStatus);
            summary.Children.Add(sep2);
            summary.Children.Add(intervalBlock);
            panel.Children.Add(summary);
            var warnings = new StackPanel { Margin = new Thickness(20, 5, 20, 5) };
            panel.Children.Add(warnings);
            var refresh = new Wpf.Ui.Controls.Button
            {
                Content = "Refresh",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(buttonColor)
                ),
                Margin = new Thickness(20, 5, 20, 5),
            };
            panel.Children.Add(refresh);
            panel.Children.Add(
                new SWC.TextBlock
                {
                    Text =
                        "After changing your 'Present Viewers' settings, hit the 'Save' button in the top left corner of Streamer.bot before refreshing.",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 10,
                    Margin = new Thickness(20, 0, 20, 10),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            var hiddenInterval = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "0",
                Visibility = Visibility.Collapsed,
            };
            panel.Children.Add(hiddenInterval);
            AddSeparator(panel);
            Action update = () =>
            {
                string dir = Directory.GetCurrentDirectory();
                if (dir.IndexOf("system32", StringComparison.OrdinalIgnoreCase) >= 0)
                    dir = AppDomain.CurrentDomain.BaseDirectory;
                string fp = Path.Combine(dir, "data", "settings.json");
                if (!File.Exists(fp))
                    return;
                var root = JObject.Parse(File.ReadAllText(fp));
                var twitch = root["twitch"];
                bool en = twitch?.Value<bool>("presentViewersEnabled") == true;
                bool lu = twitch?.Value<bool>("presentViewersLiveUpdate") == true;
                int iv = twitch?.Value<int>("presentViewersInterval") ?? 0;
                pvStatus.Text = en ? "Enabled" : "Disabled";
                pvStatus.Foreground = en ? Brushes.LightGreen : Brushes.IndianRed;
                luStatus.Text = lu ? "Enabled" : "Disabled";
                luStatus.Foreground = lu ? Brushes.LightGreen : Brushes.IndianRed;
                intervalBlock.Inlines.Clear();
                intervalBlock.Inlines.Add(
                    new Run("Interval: ") { FontWeight = FontWeights.Normal }
                );
                var text = iv == 1 ? "1 minute" : $"{iv} minutes";
                intervalBlock.Inlines.Add(new Run(text) { FontWeight = FontWeights.Bold });
                warnings.Children.Clear();
                if (!en)
                    warnings.Children.Add(
                        new SWC.TextBlock
                        {
                            Text =
                                "'Present Viewers' is disabled. Make sure to enable it in Streamer.bot under 'Platforms -> Twitch -> Present Viewers'",
                            FontFamily = new FontFamily(
                                new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                                "#Poppins"
                            ),
                            Foreground = Brushes.IndianRed,
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap,
                        }
                    );
                if (!lu)
                    warnings.Children.Add(
                        new SWC.TextBlock
                        {
                            Text =
                                "'Live Update' is disabled. Make sure to enable it in Streamer.bot under 'Platforms -> Twitch -> Present Viewers -> Live Update'",
                            FontFamily = new FontFamily(
                                new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                                "#Poppins"
                            ),
                            Foreground = Brushes.IndianRed,
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap,
                        }
                    );
                hiddenInterval.Text = iv.ToString();
            };
            update();
            refresh.Click += (_, __) => update();
        }

        public void AddHeader(string url)
        {
            var headerImage = new SWC.Image
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                MaxHeight = 180,
            };
            try
            {
                using (var wc = new WebClient())
                {
                    byte[] imageData = wc.DownloadData(url);
                    using (var stream = new MemoryStream(imageData))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        headerImage.Source = bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                _cph.LogInfo($"[AddHeader] Failed to load image: {ex.Message}");
                return;
            }
            var container = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
            };
            container.Children.Add(headerImage);
            if (_mainGrid != null)
            {
                _mainGrid.Children.Add(container);
                Grid.SetRow(container, 1);
            }
        }

        public string GetVersion()
        {
            return Version;
        }

        public T GetValue<T>(string title, string key)
        {
            string varName = $"tawmae_Settings_{title}";
            string data = _cph.GetGlobalVar<string>(varName, true);

            if (string.IsNullOrWhiteSpace(data))
            {
                _cph.LogInfo(
                    $"{_titleSuffix} {GetDisplayVersion()} [{GetVersion()}]: GetValue<{typeof(T).Name}>: Global Variable doesn't exist. No data for '{varName}' (key='{key}')."
                );
                return default;
            }

            try
            {
                var obj = JObject.Parse(data);
                if (obj.ContainsKey(key))
                {
                    return obj[key].ToObject<T>();
                }
                else
                {
                    _cph.LogInfo(
                        $"{_titleSuffix} {GetDisplayVersion()} [{GetVersion()}]: GetValue<{typeof(T).Name}>: Key '{key}' not present in '{varName}'."
                    );
                }
            }
            catch (Exception ex)
            {
                _cph.LogInfo(
                    $"{_titleSuffix} {GetDisplayVersion()} [{GetVersion()}]: GetValue<{typeof(T).Name}>: Failed to parse JSON for '{varName}' (key='{key}'): {ex.Message}"
                );
            }

            return default;
        }

        public void SetValue<T>(string title, string key, T newValue)
        {
            string data = _cph.GetGlobalVar<string>($"tawmae_Settings_{title}", true);
            if (string.IsNullOrEmpty(data))
                data = "{}";

            JObject obj = JObject.Parse(data);
            obj[key] = JToken.FromObject(newValue);
            string newJson = obj.ToString(Formatting.None);
            _cph.SetGlobalVar($"tawmae_Settings_{title}", newJson, true);
            if (title == _titleSuffix)
            {
                _existingSettings[key] = JToken.FromObject(newValue);
            }
        }

        public T GetPendingValue<T>(string saveKey)
        {
            if (_controlMap.TryGetValue(saveKey, out var ctrl))
            {
                if (ctrl is System.Windows.Controls.PasswordBox pb && typeof(T) == typeof(string))
                    return (T)(object)pb.Password;

                if (ctrl is Wpf.Ui.Controls.TextBox tb)
                {
                    if (typeof(T) == typeof(double))
                        return (T)(object)Math.Round(double.Parse(tb.Text), 2);
                    if (typeof(T) == typeof(int))
                        return (T)(object)Convert.ToInt32(Math.Round(double.Parse(tb.Text), 2));
                    return (T)(object)tb.Text;
                }

                if (ctrl is ToggleSwitch tog && typeof(T) == typeof(bool))
                    return (T)(object)(tog.IsChecked == true);

                if (ctrl is ComboBox cb && typeof(T) == typeof(string))
                    return (T)(object)(cb.SelectedItem?.ToString() ?? "");

                if (ctrl is Slider sl)
                {
                    if (typeof(T) == typeof(double))
                        return (T)(object)Math.Round(sl.Value, 2);
                    if (typeof(T) == typeof(int))
                        return (T)(object)Convert.ToInt32(Math.Round(sl.Value, 2));
                    return (T)Convert.ChangeType(sl.Value, typeof(T));
                }
            }
            return GetValue<T>(_titleSuffix, saveKey);
        }

        private void EnsureTabExists(string tabName)
        {
            if (!_withUi || _window == null)
                return;
            if (!_tabContentPanels.ContainsKey(tabName))
            {
                var stackPanel = new StackPanel
                {
                    Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                _tabContentPanels[tabName] = stackPanel;
                AddSeparator(stackPanel);
                var listBoxItem = new ListBoxItem
                {
                    Content = tabName,
                    Tag = tabName,
                    Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 16,
                    Padding = new Thickness(10),
                };
                if (tabName == "Help")
                    _sidebar.Items.Add(listBoxItem);
                else
                    _sidebar.Items.Insert(Math.Max(0, _sidebar.Items.Count - 1), listBoxItem);
                if (_sidebar.SelectedItem == null && tabName != "Help")
                    _sidebar.SelectedItem = listBoxItem;
            }
        }

        private void AddSeparator(StackPanel panel)
        {
            var sep = new Border
            {
                Height = 1,
                Margin = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            };
            panel.Children.Add(sep);
        }

        public void AddImage(string url, string tabName)
        {
            if (string.IsNullOrWhiteSpace(tabName))
                return;
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            var image = new SWC.Image
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.Uniform,
            };
            BitmapImage bitmap;
            try
            {
                using (var wc = new WebClient())
                {
                    byte[] data = wc.DownloadData(url);
                    using (var stream = new MemoryStream(data))
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                }
                image.Source = bitmap;
            }
            catch (Exception ex)
            {
                _cph.LogInfo($"[AddImage] Failed to load image: {ex.Message}");
                return;
            }
            var container = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 10, 20, 10),
            };
            container.Children.Add(image);
            var binding = new System.Windows.Data.Binding("ActualWidth")
            {
                Source = container,
                Mode = System.Windows.Data.BindingMode.OneWay,
            };
            image.SetBinding(FrameworkElement.WidthProperty, binding);
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
        }

        public void AddToggleSwitch(
            string title,
            string description,
            string tabName,
            string saveKey,
            bool defaultValue = false
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            bool loadedValue = _existingSettings[saveKey]?.Value<bool>() ?? defaultValue;
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            var toggle = new ToggleSwitch
            {
                IsChecked = loadedValue,
                Tag = saveKey,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Margin = new Thickness(20, 5, 20, 5),
            };
            _controlMap[saveKey] = toggle;
            container.Children.Add(toggle);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(20, 0, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            if (!_defaultValues.ContainsKey(saveKey))
            {
                _defaultValues[saveKey] = defaultValue;
            }
        }

        public void AddSlider(
            string title,
            string description,
            string tabName,
            string saveKey,
            double minimum,
            double maximum,
            double defaultValue = 0
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var parentPanel = _tabContentPanels[tabName];
            long loadedVal = _existingSettings[saveKey]?.Value<long>() ?? (long)defaultValue;

            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };

            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);

            var row = new Grid { Margin = new Thickness(20, 0, 20, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );

            var valueBox = new Wpf.Ui.Controls.TextBox
            {
                Text = loadedVal.ToString(),
                Width = 100,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Foreground = Brushes.White,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
            };
            Grid.SetColumn(valueBox, 0);
            row.Children.Add(valueBox);

            var slider = new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = loadedVal,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Tag = saveKey,
                Height = 30,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
            };
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);

            _controlMap[saveKey] = slider;

            slider.ValueChanged += (s, e) =>
            {
                if (!valueBox.IsFocused)
                    valueBox.Text = ((long)slider.Value).ToString();
            };

            void SyncBoxToSlider()
            {
                if (long.TryParse(valueBox.Text, out long vLong))
                {
                    double v = Math.Max(minimum, Math.Min(maximum, vLong));
                    slider.Value = v;
                    valueBox.Text = ((long)v).ToString();
                }
                else
                    valueBox.Text = ((long)slider.Value).ToString();
            }

            valueBox.PreviewTextInput += (s, e) => e.Handled = !e.Text.All(char.IsDigit);
            DataObject.AddPastingHandler(
                valueBox,
                (s, e) =>
                {
                    if (
                        !e.DataObject.GetDataPresent(DataFormats.Text)
                        || !(e.DataObject.GetData(DataFormats.Text) is string txt)
                        || !txt.All(char.IsDigit)
                    )
                        e.CancelCommand();
                }
            );

            valueBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    SyncBoxToSlider();
                    slider.Focus();
                }
            };
            valueBox.LostFocus += (s, e) => SyncBoxToSlider();

            container.Children.Add(row);

            if (!string.IsNullOrEmpty(description))
            {
                var desc = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(20, 3, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(desc);
            }

            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);

            if (!_defaultValues.ContainsKey(saveKey))
                _defaultValues[saveKey] = defaultValue;
        }

        public void AddTextbox(
            string title,
            string description,
            string tabName,
            string saveKey,
            string defaultText = "",
            bool isPassword = false
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            string loadedValue = _existingSettings[saveKey]?.Value<string>() ?? defaultText;
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontSize = 16,
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            if (isPassword)
            {
                var pwd = new SWC.PasswordBox
                {
                    Password = loadedValue,
                    Tag = saveKey,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#455f8c")
                    ),
                    Foreground = Brushes.White,
                    Margin = new Thickness(20, 0, 20, 0),
                    PasswordChar = '•',
                };
                _passwordKeys.Add(saveKey);
                _controlMap[saveKey] = pwd;
                container.Children.Add(pwd);
            }
            else
            {
                var box = new Wpf.Ui.Controls.TextBox
                {
                    Text = loadedValue,
                    Tag = saveKey,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#455f8c")
                    ),
                    Foreground = Brushes.White,
                    Margin = new Thickness(20, 0, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                _controlMap[saveKey] = box;
                container.Children.Add(box);
            }
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    Margin = new Thickness(20, 5, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            if (!_defaultValues.ContainsKey(saveKey))
            {
                _defaultValues[saveKey] = defaultText;
            }
        }

        public void AddDropdown(
            string title,
            string description,
            string tabName,
            string saveKey,
            string[] options,
            int defaultIndex = 0
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            string loadedValue =
                _existingSettings[saveKey]?.Value<string>()
                ?? (
                    options != null && options.Length > 0
                        ? options[Math.Min(Math.Max(defaultIndex, 0), options.Length - 1)]
                        : ""
                );
            var container = new StackPanel { Margin = new Thickness(20, 0, 20, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            var combo = new SWC.ComboBox
            {
                ItemsSource = options,
                Tag = saveKey,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                SelectedItem = loadedValue,
            };
            _dropdowns[saveKey] = combo;
            _controlMap[saveKey] = combo;
            container.Children.Add(combo);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            if (!_defaultValues.ContainsKey(saveKey) && options != null && options.Length > 0)
            {
                _defaultValues[saveKey] = options[
                    Math.Min(Math.Max(defaultIndex, 0), options.Length - 1)
                ];
            }
        }

        public void AddDropdownWithPairValue(
            string title,
            string description,
            string tabName,
            string displaySaveKey,
            string valueSaveKey,
            List<Tuple<string, string>> options,
            int defaultIndex = 0
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            string loadedDisplayValue = _existingSettings[displaySaveKey]?.Value<string>();
            int selectedIndex = defaultIndex;
            if (options != null && options.Count > 0)
            {
                if (!string.IsNullOrEmpty(loadedDisplayValue))
                {
                    int index = options.FindIndex(t => t.Item2 == loadedDisplayValue);
                    if (index >= 0)
                    {
                        selectedIndex = index;
                    }
                }
                else
                {
                    loadedDisplayValue = options[
                        Math.Min(Math.Max(defaultIndex, 0), options.Count - 1)
                    ].Item2;
                }
            }
            else
            {
                loadedDisplayValue = "";
            }
            var container = new StackPanel { Margin = new Thickness(20, 0, 20, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            var combo = new SWC.ComboBox
            {
                ItemsSource = options,
                DisplayMemberPath = "Item2",
                SelectedValuePath = "Item1",
                Tag = $"{displaySaveKey}|{valueSaveKey}",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                SelectedIndex = selectedIndex,
            };
            _dropdowns[displaySaveKey] = combo;
            container.Children.Add(combo);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            if (!_defaultValues.ContainsKey(displaySaveKey) && options != null && options.Count > 0)
            {
                _defaultValues[displaySaveKey] = options[
                    Math.Min(Math.Max(defaultIndex, 0), options.Count - 1)
                ].Item2;
            }
            if (!_defaultValues.ContainsKey(valueSaveKey) && options != null && options.Count > 0)
            {
                _defaultValues[valueSaveKey] = options[
                    Math.Min(Math.Max(defaultIndex, 0), options.Count - 1)
                ].Item1;
            }
        }

        public void UpdateDropdown(string saveKey, string[] newItems, int newSelectedIndex = 0)
        {
            if (!_dropdowns.ContainsKey(saveKey))
                return;
            var combo = _dropdowns[saveKey];
            string currentSelection = combo.SelectedItem as string;
            List<string> updatedList = newItems != null ? newItems.ToList() : new List<string>();
            bool selectionValid =
                currentSelection != null && updatedList.Contains(currentSelection);
            if (!selectionValid && currentSelection != null)
            {
                if (!updatedList.Contains("< invalid selection >"))
                    updatedList.Insert(0, "< invalid selection >");
                combo.ItemsSource = updatedList;
                combo.SelectedItem = "< invalid selection >";
            }
            else
            {
                combo.ItemsSource = updatedList;
                if (currentSelection != null && updatedList.Contains(currentSelection))
                {
                    combo.SelectedItem = currentSelection;
                }
                else if (updatedList.Count > 0)
                {
                    combo.SelectedIndex = Math.Max(
                        0,
                        Math.Min(newSelectedIndex, updatedList.Count - 1)
                    );
                }
                else
                {
                    combo.SelectedIndex = -1;
                }
            }
        }

        public void UpdateDropdownWithPairValue(
            string displaySaveKey,
            string valueSaveKey,
            List<Tuple<string, string>> newOptions,
            int newSelectedIndex = 0
        )
        {
            if (!_dropdowns.ContainsKey(displaySaveKey))
                return;
            var combo = _dropdowns[displaySaveKey];
            combo.DisplayMemberPath = "Item2";
            combo.SelectedValuePath = "Item1";
            var currentTuple = combo.SelectedItem as Tuple<string, string>;
            List<Tuple<string, string>> updatedList =
                newOptions != null ? newOptions.ToList() : new List<Tuple<string, string>>();
            bool selectionValid =
                currentTuple != null
                && updatedList.Any(t =>
                    t.Item1 == currentTuple.Item1 && t.Item2 == currentTuple.Item2
                );
            if (!selectionValid && currentTuple != null)
            {
                if (!updatedList.Any(t => t.Item2 == "< invalid selection >"))
                    updatedList.Insert(0, new Tuple<string, string>("", "< invalid selection >"));
                combo.ItemsSource = updatedList;
                combo.SelectedItem = updatedList.First(t => t.Item2 == "< invalid selection >");
            }
            else
            {
                combo.ItemsSource = updatedList;
                if (
                    currentTuple != null
                    && updatedList.Any(t =>
                        t.Item1 == currentTuple.Item1 && t.Item2 == currentTuple.Item2
                    )
                )
                {
                    combo.SelectedItem = currentTuple;
                }
                else if (updatedList.Count > 0)
                {
                    combo.SelectedIndex = Math.Max(
                        0,
                        Math.Min(newSelectedIndex, updatedList.Count - 1)
                    );
                }
                else
                {
                    combo.SelectedIndex = -1;
                }
            }
        }

        public void AddDynamicDropdown(
    string title,
    string description,
    string tabName,
    string saveKey,
    string[] dropdownList
)
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 5, 0, 5),
                };
            var parentPanel = _tabContentPanels[tabName];
            var container = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            };
            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            if (!string.IsNullOrEmpty(description))
                container.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = description,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 0, 5),
                        TextWrapping = TextWrapping.Wrap,
                    }
                );
            var hidden = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "[]",
                Visibility = Visibility.Collapsed,
            };
            var panel = _dynamicPanels[saveKey];
            container.Children.Add(hidden);
            container.Children.Add(panel);
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            void UpdateHidden()
            {
                var arr = new JArray();
                foreach (Border b in panel.Children.OfType<Border>())
                {
                    var grid = (Grid)b.Child;
                    var combo = grid.Children.OfType<ComboBox>().FirstOrDefault();
                    arr.Add(combo?.SelectedItem?.ToString() ?? "");
                }
                hidden.Text = arr.ToString(Formatting.None);
            }
            Border CreateRow(bool canRemove, string preset)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(5),
                };
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var combo = new ComboBox
                {
                    ItemsSource = dropdownList,
                    SelectedItem = preset ?? dropdownList.FirstOrDefault(),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    MinHeight = 30,
                    Padding = new Thickness(5, 2, 5, 2),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                combo.SelectionChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(combo, 0);
                grid.Children.Add(combo);
                if (canRemove)
                {
                    var rem = new Wpf.Ui.Controls.Button
                    {
                        Content = "-",
                        FontSize = 14,
                        Width = 30,
                        Height = 30,
                        Padding = new Thickness(0),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#d9534f")
                        ),
                        Foreground = Brushes.White,
                    };
                    rem.Click += (s, e) =>
                    {
                        if (panel.Children.Count > 1)
                            panel.Children.Remove(border);
                        UpdateHidden();
                    };
                    Grid.SetColumn(rem, 1);
                    grid.Children.Add(rem);
                }
                var add = new Wpf.Ui.Controls.Button
                {
                    Content = "+",
                    FontSize = 14,
                    Width = 30,
                    Height = 30,
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Foreground = Brushes.White,
                };
                add.Click += (s, e) =>
                {
                    int idx = panel.Children.IndexOf(border);
                    panel.Children.Insert(idx + 1, CreateRow(true, null));
                    UpdateHidden();
                };
                Grid.SetColumn(add, 2);
                grid.Children.Add(add);
                border.Child = grid;
                return border;
            }
            var stored = _existingSettings.ContainsKey(saveKey)
                ? _existingSettings[saveKey].Value<string>()
                : "[]";
            var arrInit = JArray.Parse(stored);
            if (arrInit.Count > 0)
                foreach (var t in arrInit)
                    panel.Children.Add(CreateRow(true, t.Value<string>()));
            else
                panel.Children.Add(CreateRow(false, null));
            UpdateHidden();
        }


        public void UpdateDynamicDropdown(string saveKey, string[] newOptions)
        {
            if (!_dynamicPanels.ContainsKey(saveKey))
                return;
            var panel = _dynamicPanels[saveKey];
            foreach (Border b in panel.Children.OfType<Border>())
            {
                var grid = (Grid)b.Child;
                var combo = grid.Children.OfType<ComboBox>().FirstOrDefault();
                if (combo == null)
                    continue;
                var list = newOptions.ToList();
                var current = combo.SelectedItem as string;
                var valid = current != null && list.Contains(current);
                if (!valid && current != null)
                {
                    if (!list.Contains("< invalid selection >"))
                        list.Insert(0, "< invalid selection >");
                    combo.ItemsSource = list;
                    combo.SelectedItem = "< invalid selection >";
                }
                else
                {
                    combo.ItemsSource = list;
                    if (valid)
                        combo.SelectedItem = current;
                    else if (list.Count > 0)
                        combo.SelectedIndex = 0;
                    else
                        combo.SelectedIndex = -1;
                }
            }
        }

        public void AddClickableButton(
            string title,
            string description,
            string buttonTitle,
            string buttonColorHex,
            string tabName,
            Action onClick
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            var horizontalPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 20, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            var button = new Wpf.Ui.Controls.Button
            {
                Content = buttonTitle,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(buttonColorHex)
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var statusText = new SWC.TextBlock
            {
                Text = "",
                FontSize = 14,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            };
            button.Click += (s, e) =>
            {
                try
                {
                    onClick();
                    statusText.Foreground = Brushes.LightGreen;
                    statusText.Text = "Executed successfully.";
                }
                catch
                {
                    statusText.Foreground = Brushes.IndianRed;
                    statusText.Text = "Failed to execute.";
                }
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s2, e2) =>
                {
                    statusText.Text = "";
                    timer.Stop();
                };
                timer.Start();
            };
            horizontalPanel.Children.Add(button);
            horizontalPanel.Children.Add(statusText);
            container.Children.Add(horizontalPanel);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(20, 0, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
        }

        public void AddChecklist(
            string title,
            string description,
            string tabName,
            string saveKey,
            string[] options,
            int defaultChecks
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(20, 0, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            JArray savedStates = null;
            if (_existingSettings.ContainsKey(saveKey))
            {
                try
                {
                    savedStates = JArray.Parse(_existingSettings[saveKey].Value<string>());
                }
                catch { }
            }
            var checklistPanel = new StackPanel { Margin = new Thickness(20, 5, 20, 5) };
            var initialStates = new List<bool>();
            for (int i = 0; i < options.Length; i++)
            {
                bool state = false;
                if (savedStates != null && savedStates.Count > i)
                {
                    try
                    {
                        state = savedStates[i].Value<bool>();
                    }
                    catch
                    {
                        state = i < defaultChecks;
                    }
                }
                else
                {
                    state = i < defaultChecks;
                }
                initialStates.Add(state);
                var cb = new CheckBox
                {
                    Content = options[i],
                    IsChecked = state,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(0, 2, 0, 2),
                    FontSize = 14,
                };
                cb.Checked += (s, e) =>
                {
                    UpdateChecklistHidden(checklistPanel, saveKey);
                };
                cb.Unchecked += (s, e) =>
                {
                    UpdateChecklistHidden(checklistPanel, saveKey);
                };
                checklistPanel.Children.Add(cb);
            }
            var hiddenTextBox = new Wpf.Ui.Controls.TextBox
            {
                Text = Newtonsoft.Json.JsonConvert.SerializeObject(initialStates),
                Tag = saveKey,
                Visibility = Visibility.Collapsed,
            };
            container.Children.Add(checklistPanel);
            container.Children.Add(hiddenTextBox);
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            if (!_defaultValues.ContainsKey(saveKey))
            {
                _defaultValues[saveKey] = Newtonsoft.Json.JsonConvert.SerializeObject(
                    initialStates
                );
            }
        }

        private void UpdateChecklistHidden(StackPanel checklistPanel, string saveKey)
        {
            var parent = checklistPanel.Parent as Panel;
            if (parent == null)
                return;
            var array = new JArray();
            foreach (object child in checklistPanel.Children)
            {
                if (child is CheckBox cb)
                {
                    array.Add(cb.IsChecked ?? false);
                }
            }
            foreach (object child in parent.Children)
            {
                if (
                    child is Wpf.Ui.Controls.TextBox tb
                    && tb.Tag != null
                    && tb.Tag.ToString() == saveKey
                )
                {
                    tb.Text = array.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                }
            }
        }

        public void AddColorpicker(
            string title,
            string description,
            string tabName,
            string saveKey,
            string defaultColor = "#FFFFFF"
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            string loadedValue = _existingSettings[saveKey]?.Value<string>() ?? defaultColor;
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(20, 0, 20, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            var horizontalPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 20, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            var colorPreview = new Border
            {
                Width = 60,
                Height = 30,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(loadedValue)
                ),
                Margin = new Thickness(0, 0, 10, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                BorderThickness = new Thickness(1),
            };
            var colorTextBox = new Wpf.Ui.Controls.TextBox
            {
                Text = loadedValue,
                Tag = saveKey,
                FontSize = 14,
                Width = 120,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Foreground = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
            };
            var button = new Wpf.Ui.Controls.Button
            {
                Content = "Choose",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += (s, e) =>
            {
                var colorDialog = new System.Windows.Forms.ColorDialog();
                try
                {
                    var initialColor = System.Drawing.ColorTranslator.FromHtml(colorTextBox.Text);
                    colorDialog.Color = initialColor;
                }
                catch { }
                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string newColor =
                        $"#{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
                    colorTextBox.Text = newColor;
                    colorPreview.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(newColor)
                    );
                }
            };
            horizontalPanel.Children.Add(colorPreview);
            horizontalPanel.Children.Add(colorTextBox);
            horizontalPanel.Children.Add(button);
            container.Children.Add(horizontalPanel);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(20, 0, 20, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            if (!_defaultValues.ContainsKey(saveKey))
            {
                _defaultValues[saveKey] = defaultColor;
            }
        }

        public void AddDynamicTextboxes(
            string title,
            string description,
            string tabName,
            string saveKey
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel { Orientation = Orientation.Vertical };
            var parentPanel = _tabContentPanels[tabName];
            var container = new StackPanel { Margin = new Thickness(20, 0, 20, 5) };
            var titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            container.Children.Add(titleText);
            if (!string.IsNullOrEmpty(description))
            {
                var descText = new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(descText);
            }
            var hiddenBox = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "[]",
                Visibility = Visibility.Collapsed,
            };
            container.Children.Add(hiddenBox);
            var dynamicPanel = _dynamicPanels[saveKey];

            void UpdateHidden()
            {
                var array = new JArray();
                foreach (var child in dynamicPanel.Children)
                    if (
                        child is Grid row
                        && row.Children.OfType<Wpf.Ui.Controls.TextBox>().FirstOrDefault() is var tb
                        && tb != null
                    )
                        array.Add(tb.Text);

                hiddenBox.Text = array.ToString(Newtonsoft.Json.Formatting.None);

                _existingSettings[saveKey] = array;
                _cph.SetGlobalVar(_settingsKey, _existingSettings.ToString(Formatting.None), true);
            }

            UIElement CreateRow(bool canRemove, string preset)
            {
                var rowGrid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                rowGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var textBox = new Wpf.Ui.Controls.TextBox
                {
                    Text = preset,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#455f8c")
                    ),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 5, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                textBox.TextChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(textBox, 0);
                rowGrid.Children.Add(textBox);

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
                if (canRemove)
                {
                    var removeBtn = new Wpf.Ui.Controls.Button
                    {
                        Content = "─",
                        FontSize = 14,
                        Foreground = Brushes.White,
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#d9534f")
                        ),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 0),
                    };
                    removeBtn.Click += (s, e) =>
                    {
                        if (dynamicPanel.Children.Count > 1)
                            dynamicPanel.Children.Remove(rowGrid);
                        UpdateHidden();
                    };
                    btnPanel.Children.Add(removeBtn);
                }
                var addBtn = new Wpf.Ui.Controls.Button
                {
                    Content = "+",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Padding = new Thickness(8, 4, 8, 4),
                };
                addBtn.Click += (s, e) =>
                {
                    int idx = dynamicPanel.Children.IndexOf(rowGrid);
                    dynamicPanel.Children.Insert(idx + 1, CreateRow(true, ""));
                    UpdateHidden();
                };
                btnPanel.Children.Add(addBtn);

                Grid.SetColumn(btnPanel, 1);
                rowGrid.Children.Add(btnPanel);

                return rowGrid;
            }

            var stored = _existingSettings.ContainsKey(saveKey)
                ? _existingSettings[saveKey].ToString()
                : "[]";
            var arr = JArray.Parse(stored);
            if (arr.Count > 0)
                foreach (var token in arr)
                    dynamicPanel.Children.Add(CreateRow(true, token.ToString()));
            else
                dynamicPanel.Children.Add(CreateRow(false, ""));

            var groupBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Margin = new Thickness(0, 5, 0, 5),
                Child = dynamicPanel,
            };
            container.Children.Add(groupBorder);
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
        }

        public void AddDynamicTextboxesWithPreset(
    string title,
    string description,
    string tabName,
    string saveKey,
    string[] presets
)
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 5, 0, 5),
                };
            var parent = _tabContentPanels[tabName];
            var container = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            };
            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            if (!string.IsNullOrEmpty(description))
                container.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = description,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 0, 5),
                        TextWrapping = TextWrapping.Wrap,
                    }
                );
            var hidden = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "[]",
                Visibility = Visibility.Collapsed,
            };
            var panel = _dynamicPanels[saveKey];
            container.Children.Add(hidden);
            container.Children.Add(panel);
            parent.Children.Add(container);
            AddSeparator(parent);
            void UpdateHidden()
            {
                var arr = new JArray();
                foreach (UIElement child in panel.Children)
                {
                    if (child is Grid g)
                    {
                        var tb = g.Children.OfType<Wpf.Ui.Controls.TextBox>().FirstOrDefault();
                        arr.Add(tb?.Text ?? "");
                    }
                }
                hidden.Text = arr.ToString(Formatting.None);
            }
            UIElement CreateRow(bool canRemove, string preset)
            {
                var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var box = new Wpf.Ui.Controls.TextBox
                {
                    Text = preset ?? "",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    Margin = new Thickness(0, 0, 5, 0),
                };
                box.TextChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(box, 0);
                grid.Children.Add(box);
                if (canRemove)
                {
                    var rem = new Wpf.Ui.Controls.Button
                    {
                        Content = "-",
                        FontSize = 14,
                        Width = 30,
                        Height = 30,
                        Padding = new Thickness(0),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#d9534f")
                        ),
                        Foreground = Brushes.White,
                    };
                    rem.Click += (s, e) =>
                    {
                        if (panel.Children.Count > 1)
                            panel.Children.Remove(grid);
                        UpdateHidden();
                    };
                    Grid.SetColumn(rem, 1);
                    grid.Children.Add(rem);
                }
                var add = new Wpf.Ui.Controls.Button
                {
                    Content = "+",
                    FontSize = 14,
                    Width = 30,
                    Height = 30,
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Foreground = Brushes.White,
                };
                add.Click += (s, e) =>
                {
                    int idx = panel.Children.IndexOf(grid);
                    panel.Children.Insert(idx + 1, CreateRow(true, null));
                    UpdateHidden();
                };
                Grid.SetColumn(add, 2);
                grid.Children.Add(add);
                return grid;
            }
            JArray initial = null;
            if (_existingSettings.ContainsKey(saveKey))
                initial = JArray.Parse(_existingSettings[saveKey].ToString());
            else if (presets != null && presets.Length > 0)
                initial = new JArray(presets);
            if (initial != null && initial.Count > 0)
            {
                for (int i = 0; i < initial.Count; i++)
                    panel.Children.Add(CreateRow(i > 0, initial[i].Value<string>()));
            }
            else
            {
                panel.Children.Add(CreateRow(false, null));
            }
            UpdateHidden();
        }


        public void AddDynamicTextAndDropdown(
            string title,
            string description,
            string textBoxDescription,
            string dropDownDescription,
            string tabName,
            string saveKey,
            string[] dropdownList
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 5, 0, 5),
                };
            StackPanel parentPanel = _tabContentPanels[tabName];
            StackPanel mainContainer = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            };
            SWC.TextBlock titleText = new SWC.TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            mainContainer.Children.Add(titleText);
            SWC.TextBlock descText = new SWC.TextBlock
            {
                Text = description,
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic,
                FontSize = 13,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
            };
            mainContainer.Children.Add(descText);
            Wpf.Ui.Controls.TextBox hiddenTextBox = new Wpf.Ui.Controls.TextBox
            {
                Text = "[]",
                Tag = saveKey,
                Visibility = Visibility.Collapsed,
            };
            mainContainer.Children.Add(hiddenTextBox);
            StackPanel dynamicPanel = _dynamicPanels[saveKey];
            void UpdateHidden()
            {
                JArray array = new JArray();
                foreach (object child in dynamicPanel.Children)
                {
                    if (
                        child is Border rowBorder
                        && rowBorder.Child is StackPanel rowPanel
                        && rowPanel.Children.Count >= 3
                    )
                    {
                        Wpf.Ui.Controls.TextBox tb =
                            rowPanel.Children[0] as Wpf.Ui.Controls.TextBox;
                        Grid dropdownGrid = rowPanel.Children[2] as Grid;
                        ComboBox cb = null;
                        if (dropdownGrid != null)
                        {
                            foreach (UIElement element in dropdownGrid.Children)
                            {
                                if (Grid.GetColumn(element) == 0 && element is ComboBox combo)
                                {
                                    cb = combo;
                                    break;
                                }
                            }
                        }
                        if (tb != null && cb != null)
                        {
                            JObject pair = new JObject();
                            pair["text"] = tb.Text;
                            pair["option"] =
                                cb.SelectedItem != null ? cb.SelectedItem.ToString() : "";
                            array.Add(pair);
                        }
                    }
                }
                hiddenTextBox.Text = array.ToString(Formatting.None);
            }
            Border CreateDynamicRow(
                bool canRemove,
                string presetText = "",
                string presetOption = null
            )
            {
                Border rowBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(5),
                };
                StackPanel rowPanel = new StackPanel { Orientation = Orientation.Vertical };
                Wpf.Ui.Controls.TextBox inputBox = new Wpf.Ui.Controls.TextBox
                {
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#455f8c")
                    ),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 5),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Text = presetText,
                };
                inputBox.TextChanged += (s, e) =>
                {
                    UpdateHidden();
                };
                rowPanel.Children.Add(inputBox);
                SWC.TextBlock tbDesc = new SWC.TextBlock
                {
                    Text = textBoxDescription,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 12,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                };
                rowPanel.Children.Add(tbDesc);
                Grid dropdownGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                dropdownGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                dropdownGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Auto }
                );
                ComboBox combo = new ComboBox
                {
                    ItemsSource = dropdownList,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#455f8c")
                    ),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 5, 0),
                };
                if (presetOption != null)
                {
                    int index = Array.IndexOf(dropdownList, presetOption);
                    combo.SelectedIndex = index >= 0 ? index : 0;
                }
                else
                {
                    combo.SelectedIndex = 0;
                }
                combo.SelectionChanged += (s, e) =>
                {
                    UpdateHidden();
                };
                Grid.SetColumn(combo, 0);
                dropdownGrid.Children.Add(combo);
                StackPanel btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                if (canRemove)
                {
                    var removeBtn = new Wpf.Ui.Controls.Button
                    {
                        Content = "-",
                        FontSize = 14,
                        Foreground = Brushes.White,
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#d9534f")
                        ),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 0),
                    };
                    removeBtn.Click += (s, e) =>
                    {
                        if (dynamicPanel.Children.Count > 1)
                            dynamicPanel.Children.Remove(rowBorder);
                        UpdateHidden();
                    };
                    btnPanel.Children.Add(removeBtn);
                }
                var addBtn = new Wpf.Ui.Controls.Button
                {
                    Content = "+",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Padding = new Thickness(8, 4, 8, 4),
                };
                addBtn.Click += (s, e) =>
                {
                    int idx = dynamicPanel.Children.IndexOf(rowBorder);
                    Border newRow = CreateDynamicRow(true);
                    dynamicPanel.Children.Insert(idx + 1, newRow);
                    UpdateHidden();
                };
                btnPanel.Children.Add(addBtn);
                Grid.SetColumn(btnPanel, 1);
                dropdownGrid.Children.Add(btnPanel);
                rowPanel.Children.Add(dropdownGrid);
                SWC.TextBlock ddDesc = new SWC.TextBlock
                {
                    Text = dropDownDescription,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 12,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(0, 5, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                rowPanel.Children.Add(ddDesc);
                rowBorder.Child = rowPanel;
                return rowBorder;
            }
            string storedJson = _existingSettings.ContainsKey(saveKey)
                ? _existingSettings[saveKey].Value<string>() ?? "[]"
                : "[]";
            JArray storedArray = JArray.Parse(storedJson);
            if (storedArray.Count > 0)
            {
                foreach (JToken token in storedArray)
                {
                    string presetText = token["text"]?.Value<string>() ?? "";
                    string presetOption = token["option"]?.Value<string>();
                    Border row = CreateDynamicRow(true, presetText, presetOption);
                    dynamicPanel.Children.Add(row);
                }
            }
            else
            {
                Border firstRow = CreateDynamicRow(false);
                dynamicPanel.Children.Add(firstRow);
            }
            mainContainer.Children.Add(dynamicPanel);
            parentPanel.Children.Add(mainContainer);
        }

        public void UpdateDynamicTextAndDropdown(string saveKey, string[] newDropdownList)
        {
            if (_dynamicPanels == null || !_dynamicPanels.ContainsKey(saveKey))
                return;
            StackPanel dynamicPanel = _dynamicPanels[saveKey];
            foreach (object child in dynamicPanel.Children)
            {
                if (
                    child is Border rowBorder
                    && rowBorder.Child is StackPanel rowPanel
                    && rowPanel.Children.Count >= 3
                )
                {
                    Grid dropdownGrid = rowPanel.Children[2] as Grid;
                    if (dropdownGrid != null)
                    {
                        foreach (UIElement element in dropdownGrid.Children)
                        {
                            if (Grid.GetColumn(element) == 0 && element is ComboBox combo)
                            {
                                string currentSelection = combo.SelectedItem as string;
                                List<string> updatedList =
                                    newDropdownList != null
                                        ? newDropdownList.ToList()
                                        : new List<string>();
                                bool selectionValid =
                                    !string.IsNullOrEmpty(currentSelection)
                                    && updatedList.Contains(currentSelection);
                                if (!selectionValid && !string.IsNullOrEmpty(currentSelection))
                                {
                                    if (!updatedList.Contains("< invalid selection >"))
                                        updatedList.Insert(0, "< invalid selection >");
                                    combo.ItemsSource = updatedList;
                                    combo.SelectedItem = "< invalid selection >";
                                }
                                else
                                {
                                    combo.ItemsSource = updatedList;
                                    if (
                                        !string.IsNullOrEmpty(currentSelection)
                                        && updatedList.Contains(currentSelection)
                                    )
                                        combo.SelectedItem = currentSelection;
                                    else if (updatedList.Count > 0)
                                        combo.SelectedIndex = 0;
                                    else
                                        combo.SelectedIndex = -1;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddDynamicDropdownWithTextboxAndNumber(
            string title,
            string description,
            string textBoxDescription,
            string valueBoxDescription,
            string dropDownDescription,
            string tabName,
            string saveKey,
            string[] dropdownList
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 5, 0, 5),
                };
            var parentPanel = _tabContentPanels[tabName];
            var mainContainer = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            };
            mainContainer.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/"),
                        "./Font/#Poppins"
                    ),
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            mainContainer.Children.Add(
                new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/"),
                        "./Font/#Poppins"
                    ),
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap,
                }
            );
            var dynamicPanel = _dynamicPanels[saveKey];
            var hidden = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "[]",
                Visibility = Visibility.Collapsed,
            };
            mainContainer.Children.Add(hidden);

            void UpdateHidden()
            {
                var _arr = new JArray();
                foreach (Border b in dynamicPanel.Children.OfType<Border>())
                {
                    var row = (StackPanel)b.Child;
                    var grid = (Grid)row.Children[0];
                    var textBox = (Wpf.Ui.Controls.TextBox)
                        ((StackPanel)grid.Children[0]).Children[0];
                    var valBox = (Wpf.Ui.Controls.TextBox)
                        ((StackPanel)grid.Children[1]).Children[0];
                    var dropGrid = (Grid)row.Children[1];
                    var combo = dropGrid.Children.OfType<ComboBox>().First();
                    _arr.Add(
                        new JObject
                        {
                            ["text"] = textBox.Text,
                            ["value"] = int.TryParse(valBox.Text, out var n) ? n : 0,
                            ["option"] = combo.SelectedItem?.ToString() ?? "",
                        }
                    );
                }
                hidden.Text = _arr.ToString(Formatting.None);
            }

            _updateDateTimeHidden[saveKey] = UpdateHidden;

            Border CreateRow(bool canRemove, JObject preset)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(5),
                };
                var row = new StackPanel { Orientation = Orientation.Vertical };
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var textStack = new StackPanel { Orientation = Orientation.Vertical };
                var tb = new Wpf.Ui.Controls.TextBox
                {
                    Text = preset?["text"]?.Value<string>() ?? "",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/"),
                        "./Font/#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    Margin = new Thickness(0, 0, 0, 2),
                };
                tb.TextChanged += (_, __) => UpdateHidden();
                textStack.Children.Add(tb);
                textStack.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = textBoxDescription,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 12,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/"),
                            "./Font/#Poppins"
                        ),
                    }
                );
                Grid.SetColumn(textStack, 0);
                grid.Children.Add(textStack);
                var valStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                var vb = new Wpf.Ui.Controls.TextBox
                {
                    Text = preset?["value"]?.Value<int>().ToString() ?? "0",
                    Width = 100,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/"),
                        "./Font/#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    Margin = new Thickness(0, 0, 0, 2),
                };
                vb.PreviewTextInput += (_, e) => e.Handled = !e.Text.All(char.IsDigit);
                vb.TextChanged += (_, __) => UpdateHidden();
                valStack.Children.Add(vb);
                valStack.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = valueBoxDescription,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 12,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/"),
                            "./Font/#Poppins"
                        ),
                        Margin = new Thickness(0, 3, 0, 0),
                        TextAlignment = TextAlignment.Center,
                    }
                );
                Grid.SetColumn(valStack, 1);
                grid.Children.Add(valStack);
                row.Children.Add(grid);
                var dropGrid = new Grid();
                dropGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                dropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                dropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                var combo = new ComboBox
                {
                    ItemsSource = dropdownList,
                    SelectedItem = preset?["option"]?.Value<string>(),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/"),
                        "./Font/#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    Margin = new Thickness(0, 0, 5, 0),
                    MaxDropDownHeight = 200,
                };
                combo.SetValue(
                    ScrollViewer.VerticalScrollBarVisibilityProperty,
                    ScrollBarVisibility.Auto
                );
                combo.SelectionChanged += (_, __) => UpdateHidden();
                Grid.SetColumn(combo, 0);
                dropGrid.Children.Add(combo);
                var rem = new Wpf.Ui.Controls.Button
                {
                    Content = "-",
                    Width = 30,
                    Height = 30,
                    FontSize = 16,
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#d9534f")
                    ),
                    Foreground = Brushes.White,
                    Visibility = canRemove ? Visibility.Visible : Visibility.Hidden,
                };
                rem.Click += (_, __) =>
                {
                    if (dynamicPanel.Children.Count > 1)
                        dynamicPanel.Children.Remove(border);
                    UpdateHidden();
                };
                Grid.SetColumn(rem, 1);
                dropGrid.Children.Add(rem);
                var add = new Wpf.Ui.Controls.Button
                {
                    Content = "+",
                    Width = 30,
                    Height = 30,
                    FontSize = 16,
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Foreground = Brushes.White,
                };
                add.Click += (_, __) =>
                {
                    int idx = dynamicPanel.Children.IndexOf(border);
                    dynamicPanel.Children.Insert(idx + 1, CreateRow(true, null));
                    UpdateHidden();
                };
                Grid.SetColumn(add, 2);
                dropGrid.Children.Add(add);
                row.Children.Add(dropGrid);
                row.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = dropDownDescription,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 12,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/"),
                            "./Font/#Poppins"
                        ),
                        Margin = new Thickness(0, 5, 0, 0),
                    }
                );
                border.Child = row;
                return border;
            }

            var stored = _existingSettings.ContainsKey(saveKey)
                ? _existingSettings[saveKey].Value<string>()
                : "[]";
            var arr = JArray.Parse(stored);
            if (arr.Count > 0)
                foreach (JObject o in arr)
                    dynamicPanel.Children.Add(CreateRow(true, o));
            else
                dynamicPanel.Children.Add(CreateRow(false, null));

            mainContainer.Children.Add(dynamicPanel);
            parentPanel.Children.Add(mainContainer);
        }

        public void UpdateDynamicDropdownWithTextboxAndNumber(
            string saveKey,
            string[] newDropdownList
        )
        {
            if (_dynamicPanels == null || !_dynamicPanels.ContainsKey(saveKey))
                return;

            var dynamicPanel = _dynamicPanels[saveKey];
            foreach (var border in dynamicPanel.Children.Cast<UIElement>().OfType<Border>())
            {
                var row = (StackPanel)border.Child;
                var combo = row
                    .Children.OfType<Grid>()
                    .Skip(1)
                    .First()
                    .Children.Cast<UIElement>()
                    .OfType<ComboBox>()
                    .FirstOrDefault();
                if (combo == null)
                    continue;

                var list = newDropdownList?.ToList() ?? new List<string>();
                var current = combo.SelectedItem as string;
                var valid = !string.IsNullOrEmpty(current) && list.Contains(current);

                if (!valid && !string.IsNullOrEmpty(current))
                {
                    if (!list.Contains("< invalid selection >"))
                        list.Insert(0, "< invalid selection >");
                    combo.ItemsSource = list;
                    combo.SelectedItem = "< invalid selection >";
                }
                else
                {
                    combo.ItemsSource = list;
                    if (valid)
                        combo.SelectedItem = current;
                    else if (list.Count > 0)
                        combo.SelectedIndex = 0;
                    else
                        combo.SelectedIndex = -1;
                }
            }
        }

        public void AddDateTimePicker(
            string title,
            string description,
            string tabName,
            string saveKey,
            string[] dropdownList
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 5, 0, 5),
                };

            var parentPanel = _tabContentPanels[tabName];
            var wrapper = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            };
            wrapper.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 5),
                }
            );
            wrapper.Children.Add(
                new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(0, 0, 0, 10),
                }
            );

            var hiddenBox = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "[]",
                Visibility = Visibility.Collapsed,
            };
            wrapper.Children.Add(hiddenBox);

            var listPanel = _dynamicPanels[saveKey];

            void UpdateHidden()
            {
                var resultArr = new JArray();
                foreach (Border b in listPanel.Children.OfType<Border>())
                {
                    var rootPanel = (StackPanel)b.Child;
                    var timeRow = (StackPanel)rootPanel.Children[0];
                    var weekdayRow = (Grid)rootPanel.Children[1];
                    var dateRow = (Grid)rootPanel.Children[2];
                    var monthRow = (Grid)rootPanel.Children[3];
                    var dropRow = (Grid)rootPanel.Children[4];

                    var h = (ComboBox)((StackPanel)timeRow.Children[0]).Children[0];
                    var m = (ComboBox)((StackPanel)timeRow.Children[1]).Children[0];
                    var s = (ComboBox)((StackPanel)timeRow.Children[2]).Children[0];
                    var wdBox = (ComboBox)weekdayRow.Children[0];
                    var wdTog = (ToggleSwitch)weekdayRow.Children[1];
                    var dp = (DatePicker)dateRow.Children[0];
                    var dTog = (ToggleSwitch)dateRow.Children[1];
                    var dayBox = (ComboBox)monthRow.Children[0];
                    var mTog = (ToggleSwitch)monthRow.Children[1];
                    var cb = (ComboBox)dropRow.Children[0];

                    var obj = new JObject
                    {
                        ["time"] = $"{h.SelectedItem}:{m.SelectedItem}:{s.SelectedItem}",
                        ["weekday"] = wdTog.IsChecked == true ? wdBox.SelectedItem?.ToString() : "",
                        ["specificDate"] = dTog.IsChecked == true,
                        ["date"] = dp.SelectedDate.HasValue
                            ? dp.SelectedDate.Value.ToString("yyyy-MM-dd")
                            : "",
                        ["monthly"] = mTog.IsChecked == true ? dayBox.SelectedItem?.ToString() : "",
                        ["option"] = cb.SelectedItem?.ToString() ?? "",
                    };
                    resultArr.Add(obj);
                }
                hiddenBox.Text = resultArr.ToString(Formatting.None);
            }

            Func<bool, JObject, Border> createRow = null;
            createRow = (canRemove, preset) =>
            {
                var bd = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(5),
                };
                var rootPanel = new StackPanel { Orientation = Orientation.Vertical };

                ToggleSwitch dateTog = null;
                ToggleSwitch monthTog = null;

                var timeRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 10),
                };
                var hours = Enumerable.Range(0, 24).Select(i => i.ToString("D2")).ToList();
                var secs = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();
                var timeParts =
                    preset?["time"]?.Value<string>()?.Split(':')
                    ?? new[] { hours[0], secs[0], secs[0] };
                foreach (
                    var (list, label, sel) in new (List<string>, string, string)[]
                    {
                        (hours, "Hour", timeParts[0]),
                        (secs, "Minute", timeParts[1]),
                        (secs, "Second", timeParts[2]),
                    }
                )
                {
                    var stack = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(0, 0, 10, 0),
                    };
                    var box = new ComboBox
                    {
                        ItemsSource = list,
                        MinWidth = 70,
                        SelectedItem = list.Contains(sel) ? sel : list[0],
                    };
                    box.SelectionChanged += (s, e) => UpdateHidden();
                    stack.Children.Add(box);
                    stack.Children.Add(
                        new SWC.TextBlock
                        {
                            Text = label,
                            Foreground = Brushes.Gray,
                            FontSize = 12,
                            FontFamily = new FontFamily(
                                new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                                "#Poppins"
                            ),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        }
                    );
                    timeRow.Children.Add(stack);
                }
                rootPanel.Children.Add(timeRow);

                var weekdayRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                weekdayRow.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                weekdayRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var days = new[]
                {
                    "Monday",
                    "Tuesday",
                    "Wednesday",
                    "Thursday",
                    "Friday",
                    "Saturday",
                    "Sunday",
                };
                var presetWd = preset?["weekday"]?.Value<string>() ?? "";
                var wdBox = new ComboBox
                {
                    ItemsSource = days,
                    SelectedItem = days.Contains(presetWd) ? presetWd : days[0],
                    IsEnabled = presetWd != "",
                };
                var wdTog = new ToggleSwitch
                {
                    Content = "On Every Weekday",
                    IsChecked = presetWd != "",
                    Margin = new Thickness(10, 0, 0, 0),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#476393")
                    ),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                };
                wdTog.Checked += (s, e) =>
                {
                    wdBox.IsEnabled = true;
                    dateTog.IsChecked = false;
                    monthTog.IsChecked = false;
                    UpdateHidden();
                };
                wdTog.Unchecked += (s, e) =>
                {
                    wdBox.IsEnabled = false;
                    UpdateHidden();
                };
                wdBox.SelectionChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(wdBox, 0);
                Grid.SetColumn(wdTog, 1);
                weekdayRow.Children.Add(wdBox);
                weekdayRow.Children.Add(wdTog);
                rootPanel.Children.Add(weekdayRow);

                var dateRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                dateRow.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                dateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var datePicker = new DatePicker();
                if (
                    preset?["date"] != null
                    && DateTime.TryParse(preset["date"].Value<string>(), out var dt)
                )
                    datePicker.SelectedDate = dt;
                dateTog = new ToggleSwitch
                {
                    Content = "On Specific Date",
                    IsChecked = preset?["specificDate"]?.Value<bool>() ?? false,
                    Margin = new Thickness(10, 0, 0, 0),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#476393")
                    ),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                };
                datePicker.IsEnabled = dateTog.IsChecked == true;
                dateTog.Checked += (s, e) =>
                {
                    datePicker.IsEnabled = true;
                    wdTog.IsChecked = false;
                    monthTog.IsChecked = false;
                    UpdateHidden();
                };
                dateTog.Unchecked += (s, e) =>
                {
                    datePicker.IsEnabled = false;
                    UpdateHidden();
                };
                datePicker.SelectedDateChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(datePicker, 0);
                Grid.SetColumn(dateTog, 1);
                dateRow.Children.Add(datePicker);
                dateRow.Children.Add(dateTog);
                rootPanel.Children.Add(dateRow);

                var monthRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                monthRow.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var daysOfMonth = Enumerable.Range(1, 31).Select(i => i.ToString()).ToList();
                var presetM = preset?["monthly"]?.Value<string>() ?? "";
                var dayBox = new ComboBox
                {
                    ItemsSource = daysOfMonth,
                    SelectedItem = daysOfMonth.Contains(presetM) ? presetM : daysOfMonth[0],
                    IsEnabled = presetM != "",
                };
                monthTog = new ToggleSwitch
                {
                    Content = "Every xth Of The Month",
                    IsChecked = presetM != "",
                    Margin = new Thickness(10, 0, 0, 0),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#476393")
                    ),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                };
                monthTog.Checked += (s, e) =>
                {
                    dayBox.IsEnabled = true;
                    wdTog.IsChecked = false;
                    dateTog.IsChecked = false;
                    UpdateHidden();
                };
                monthTog.Unchecked += (s, e) =>
                {
                    dayBox.IsEnabled = false;
                    UpdateHidden();
                };
                dayBox.SelectionChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(dayBox, 0);
                Grid.SetColumn(monthTog, 1);
                monthRow.Children.Add(dayBox);
                monthRow.Children.Add(monthTog);
                rootPanel.Children.Add(monthRow);

                var dropRow = new Grid();
                dropRow.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                dropRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var optionBox = new ComboBox
                {
                    ItemsSource = dropdownList,
                    SelectedItem = preset?["option"]?.ToString() ?? dropdownList.FirstOrDefault(),
                };
                optionBox.SelectionChanged += (s, e) => UpdateHidden();
                Grid.SetColumn(optionBox, 0);
                dropRow.Children.Add(optionBox);
                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 0, 0, 0),
                };
                if (canRemove)
                {
                    var minus = new Wpf.Ui.Controls.Button
                    {
                        Content = "-",
                        Foreground = Brushes.White,
                        Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#d9534f")
                        ),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 5, 0),
                    };
                    minus.Click += (s, e) =>
                    {
                        if (listPanel.Children.Count > 1)
                            listPanel.Children.Remove(bd);
                        UpdateHidden();
                    };
                    btnPanel.Children.Add(minus);
                }
                var plus = new Wpf.Ui.Controls.Button
                {
                    Content = "+",
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Padding = new Thickness(8, 4, 8, 4),
                };
                plus.Click += (s, e) =>
                {
                    int idx = listPanel.Children.IndexOf(bd);
                    listPanel.Children.Insert(idx + 1, createRow(true, null));
                    UpdateHidden();
                };
                btnPanel.Children.Add(plus);
                Grid.SetColumn(btnPanel, 1);
                dropRow.Children.Add(btnPanel);
                rootPanel.Children.Add(dropRow);

                rootPanel.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = "Pick an action to run.",
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 13,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Margin = new Thickness(5, 5, 0, 0),
                    }
                );

                bd.Child = rootPanel;
                return bd;
            };

            var storedJson = _existingSettings.ContainsKey(saveKey)
                ? _existingSettings[saveKey].Value<string>()
                : "[]";
            var storedArray = JArray.Parse(storedJson);
            if (storedArray.Count == 0)
                listPanel.Children.Add(createRow(false, null));
            else
                foreach (JObject obj in storedArray.OfType<JObject>())
                    listPanel.Children.Add(createRow(true, obj));
            _updateDateTimeHidden[saveKey] = UpdateHidden;
            wrapper.Children.Add(listPanel);
            parentPanel.Children.Add(wrapper);
        }

        public void UpdateDateTimePickerOptions(string saveKey, string[] newOptions)
        {
            if (!_dynamicPanels.ContainsKey(saveKey))
                return;

            var panel = _dynamicPanels[saveKey];
            foreach (var child in panel.Children)
            {
                if (child is Border bd && bd.Child is StackPanel root)
                {
                    var dropRow = root.Children.OfType<Grid>().Skip(3).FirstOrDefault();
                    if (dropRow == null)
                        continue;

                    var combo = dropRow.Children.OfType<ComboBox>().FirstOrDefault();
                    if (combo == null)
                        continue;

                    var current = combo.SelectedItem as string;
                    var list = newOptions?.ToList() ?? new List<string>();
                    bool valid = current != null && list.Contains(current);

                    if (!valid && current != null)
                    {
                        if (!list.Contains("< invalid selection >"))
                            list.Insert(0, "< invalid selection >");
                        combo.ItemsSource = list;
                        combo.SelectedItem = "< invalid selection >";
                    }
                    else
                    {
                        combo.ItemsSource = list;
                        if (valid)
                            combo.SelectedItem = current;
                        else if (list.Count > 0)
                            combo.SelectedIndex = 0;
                        else
                            combo.SelectedIndex = -1;
                    }
                }
            }
        }

        public void AddSliderWithToggleSwitch(
            string title,
            string description,
            string tabName,
            string saveKey,
            double minimum,
            double maximum,
            double defaultValue,
            bool toggleDefault = true
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var parent = _tabContentPanels[tabName];
            var storedJson = _existingSettings[saveKey]?.Value<string>();
            JObject stored = !string.IsNullOrEmpty(storedJson)
                ? JObject.Parse(storedJson)
                : new JObject { ["enabled"] = toggleDefault, ["value"] = defaultValue };

            bool loadedToggle = stored["enabled"].Value<bool>();
            double loadedValue = stored["value"].Value<double>();

            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };

            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    Margin = new Thickness(20, 0, 20, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );

            var row = new Grid { Margin = new Thickness(20, 0, 20, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var valueBox = new Wpf.Ui.Controls.TextBox
            {
                Text = ((int)loadedValue).ToString(),
                Width = 100,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Foreground = Brushes.White,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                IsEnabled = loadedToggle,
                Opacity = loadedToggle ? 1.0 : 0.5,
            };
            Grid.SetColumn(valueBox, 0);
            row.Children.Add(valueBox);

            var slider = new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = loadedValue,
                TickFrequency = (maximum - minimum) / 10.0,
                Height = 30,
                IsEnabled = loadedToggle,
                Opacity = loadedToggle ? 1.0 : 0.5,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
            };
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);

            var togglePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var toggle = new ToggleSwitch
            {
                IsChecked = loadedToggle,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Margin = new Thickness(0, 0, 0, 2),
            };
            togglePanel.Children.Add(toggle);
            togglePanel.Children.Add(
                new SWC.TextBlock
                {
                    Text = "Enabled",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            );
            Grid.SetColumn(togglePanel, 2);
            row.Children.Add(togglePanel);

            container.Children.Add(row);

            if (!string.IsNullOrEmpty(description))
                container.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = description,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 13,
                        Margin = new Thickness(20, 3, 20, 0),
                        TextWrapping = TextWrapping.Wrap,
                    }
                );

            var hidden = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Visibility = Visibility.Collapsed,
            };

            void UpdateHidden()
            {
                var obj = new JObject
                {
                    ["enabled"] = toggle.IsChecked == true,
                    ["value"] = slider.Value,
                };
                hidden.Text = obj.ToString(Formatting.None);
            }

            toggle.Checked += (s, e) =>
            {
                slider.IsEnabled = true;
                slider.Opacity = 1.0;
                valueBox.IsEnabled = true;
                valueBox.Opacity = 1.0;
                UpdateHidden();
            };
            toggle.Unchecked += (s, e) =>
            {
                slider.IsEnabled = false;
                slider.Opacity = 0.5;
                valueBox.IsEnabled = false;
                valueBox.Opacity = 0.5;
                UpdateHidden();
            };

            slider.ValueChanged += (s, e) =>
            {
                if (!valueBox.IsFocused)
                    valueBox.Text = ((int)slider.Value).ToString();
                UpdateHidden();
            };

            valueBox.PreviewTextInput += (s, e) => e.Handled = !e.Text.All(char.IsDigit);
            DataObject.AddPastingHandler(
                valueBox,
                (s, e) =>
                {
                    if (
                        !e.DataObject.GetDataPresent(DataFormats.Text)
                        || !(e.DataObject.GetData(DataFormats.Text) is string txt)
                        || !txt.All(char.IsDigit)
                    )
                        e.CancelCommand();
                }
            );
            valueBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (double.TryParse(valueBox.Text, out double v))
                    {
                        v = Math.Max(minimum, Math.Min(maximum, v));
                        slider.Value = v;
                    }
                    valueBox.Text = ((int)slider.Value).ToString();
                    slider.Focus();
                }
            };
            valueBox.LostFocus += (s, e) =>
            {
                if (double.TryParse(valueBox.Text, out double v))
                {
                    v = Math.Max(minimum, Math.Min(maximum, v));
                    slider.Value = v;
                }
                valueBox.Text = ((int)slider.Value).ToString();
            };

            container.Children.Add(hidden);
            parent.Children.Add(container);
            AddSeparator(parent);

            _defaultValues[saveKey] = new JObject
            {
                ["enabled"] = toggleDefault,
                ["value"] = defaultValue,
            };

            UpdateHidden();
        }

        public void AddDecimalStepper(
            string title,
            string description,
            string tabName,
            string saveKey,
            double minimum,
            double maximum,
            double step = 0.01,
            double defaultValue = 0
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var parent = _tabContentPanels[tabName];
            double loaded = _existingSettings[saveKey]?.Value<double>() ?? defaultValue;
            loaded = Math.Round(Math.Max(minimum, Math.Min(maximum, loaded)), 2);

            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    Margin = new Thickness(20, 0, 20, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 20, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            var dec = new Wpf.Ui.Controls.Button
            {
                Content = "−",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#d9534f")
                ),
                Foreground = Brushes.White,
            };
            var box = new Wpf.Ui.Controls.TextBox
            {
                Text = loaded.ToString("0.00", CultureInfo.InvariantCulture),
                Width = 80,
                Tag = saveKey,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Foreground = Brushes.White,
                Margin = new Thickness(5, 0, 5, 0),
            };
            var inc = new Wpf.Ui.Controls.Button
            {
                Content = "+",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#71a079")
                ),
                Foreground = Brushes.White,
            };

            void ClampAndFormat()
            {
                if (
                    !double.TryParse(
                        box.Text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double v
                    )
                )
                    v = loaded;
                v = Math.Round(Math.Max(minimum, Math.Min(maximum, v)), 2);
                box.Text = v.ToString("0.00", CultureInfo.InvariantCulture);
            }

            dec.Click += (_, __) =>
            {
                if (
                    double.TryParse(
                        box.Text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double v
                    )
                )
                {
                    v = Math.Round(Math.Max(minimum, v - step), 2);
                    box.Text = v.ToString("0.00", CultureInfo.InvariantCulture);
                }
            };
            inc.Click += (_, __) =>
            {
                if (
                    double.TryParse(
                        box.Text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double v
                    )
                )
                {
                    v = Math.Round(Math.Min(maximum, v + step), 2);
                    box.Text = v.ToString("0.00", CultureInfo.InvariantCulture);
                }
            };
            box.LostFocus += (_, __) => ClampAndFormat();

            row.Children.Add(dec);
            row.Children.Add(box);
            row.Children.Add(inc);
            container.Children.Add(row);

            if (!string.IsNullOrEmpty(description))
                container.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = description,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 13,
                        Margin = new Thickness(20, 3, 20, 0),
                        TextWrapping = TextWrapping.Wrap,
                    }
                );

            parent.Children.Add(container);
            AddSeparator(parent);

            _controlMap[saveKey] = box;
            if (!_defaultValues.ContainsKey(saveKey))
                _defaultValues[saveKey] = defaultValue;
        }

        public void AddFilepath(
            string title,
            string description,
            string tabName,
            string saveKey,
            string defaultPath = ""
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var parent = _tabContentPanels[tabName];
            string loaded = _existingSettings[saveKey]?.Value<string>() ?? defaultPath;

            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };

            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    Margin = new Thickness(20, 0, 20, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );

            var row = new Grid { Margin = new Thickness(20, 0, 20, 0) };
            row.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new Wpf.Ui.Controls.TextBox
            {
                Text = loaded,
                Tag = saveKey,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#455f8c")
                ),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 10, 0),
            };
            _controlMap[saveKey] = textBox;
            Grid.SetColumn(textBox, 0);
            row.Children.Add(textBox);

            var btn = new Wpf.Ui.Controls.Button
            {
                Content = "...",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 14,
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center,
            };
            btn.Click += (s, e) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                if (dlg.ShowDialog() == true)
                    textBox.Text = dlg.FileName;
            };
            Grid.SetColumn(btn, 1);
            row.Children.Add(btn);

            container.Children.Add(row);

            if (!string.IsNullOrEmpty(description))
                container.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = description,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        FontSize = 13,
                        Margin = new Thickness(20, 3, 20, 0),
                        TextWrapping = TextWrapping.Wrap,
                    }
                );

            parent.Children.Add(container);
            AddSeparator(parent);

            if (!_defaultValues.ContainsKey(saveKey))
                _defaultValues[saveKey] = defaultPath;
        }

        public void AddProbabilityTable(
            string title,
            string description,
            string dropdownDescription,
            string valueDescription,
            string tabName,
            string saveKey,
            string[] dropdownList
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;

            var parent = _tabContentPanels[tabName];

            var container = new StackPanel
            {
                Margin = new Thickness(20, 0, 20, 5),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            };

            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                }
            );

            container.Children.Add(
                new SWC.TextBlock
                {
                    Text = description,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap,
                }
            );

            var hidden = new Wpf.Ui.Controls.TextBox
            {
                Tag = saveKey,
                Text = "[]",
                Visibility = Visibility.Collapsed,
            };
            container.Children.Add(hidden);

            if (!_dynamicPanels.ContainsKey(saveKey))
                _dynamicPanels[saveKey] = new StackPanel { Orientation = Orientation.Vertical };
            var lootTablesPanel = _dynamicPanels[saveKey];
            container.Children.Add(lootTablesPanel);

            void UpdateHidden()
            {
                var arr = new JArray();
                foreach (Border tbl in lootTablesPanel.Children.OfType<Border>())
                {
                    var tup = (Tuple<Wpf.Ui.Controls.TextBox, StackPanel, SWC.TextBlock>)tbl.Tag;
                    var nameBox = tup.Item1;
                    var rowsPanel = tup.Item2;
                    var statusLine = tup.Item3;

                    double sum = 0;
                    var items = new JArray();
                    foreach (Border row in rowsPanel.Children.OfType<Border>())
                    {
                        var grid = (Grid)((StackPanel)row.Child).Children[0];
                        var combo = grid.Children.OfType<ComboBox>().FirstOrDefault();
                        var valBox = grid
                            .Children.OfType<Wpf.Ui.Controls.TextBox>()
                            .FirstOrDefault();

                        double v = double.TryParse(
                            valBox.Text,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var n
                        )
                            ? n
                            : 0;
                        sum += v;

                        items.Add(
                            new JObject
                            {
                                ["option"] = combo?.SelectedItem?.ToString() ?? "",
                                ["value"] = v,
                            }
                        );
                    }

                    if (Math.Round(sum, 2) == 100.0)
                        statusLine.Visibility = Visibility.Collapsed;
                    else
                    {
                        statusLine.Visibility = Visibility.Visible;
                        statusLine.Text =
                            $"Probabilities must add up to exactly 100 %. Currently at: {sum:0.##} %";
                    }

                    arr.Add(new JObject { ["name"] = nameBox.Text, ["items"] = items });
                }

                hidden.Text = arr.ToString(Formatting.None);
            }

            Border CreateRow(bool canRemove, JObject preset, StackPanel rowsPanel)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(5),
                };

                var row = new StackPanel { Orientation = Orientation.Vertical };

                var grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                var combo = new ComboBox
                {
                    ItemsSource = dropdownList,
                    SelectedItem = preset?["option"]?.Value<string>(),
                    Margin = new Thickness(0, 0, 5, 0),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                combo.SelectionChanged += (_, __) => UpdateHidden();
                Grid.SetRow(combo, 0);
                Grid.SetColumn(combo, 0);
                grid.Children.Add(combo);

                var valBox = new Wpf.Ui.Controls.TextBox
                {
                    Text = preset?["value"]?.Value<double>().ToString("0.##") ?? "0",
                    Width = 100,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                };
                valBox.PreviewTextInput += (s, e) =>
                    e.Handled = !(
                        char.IsDigit(e.Text, 0) || (e.Text == "." && !valBox.Text.Contains("."))
                    );
                DataObject.AddPastingHandler(
                    valBox,
                    (s, e) =>
                    {
                        if (
                            !e.DataObject.GetDataPresent(DataFormats.Text)
                            || !(e.DataObject.GetData(DataFormats.Text) is string txt)
                            || !double.TryParse(
                                txt,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out _
                            )
                        )
                            e.CancelCommand();
                    }
                );
                valBox.TextChanged += (_, __) => UpdateHidden();
                Grid.SetRow(valBox, 0);
                Grid.SetColumn(valBox, 1);
                grid.Children.Add(valBox);

                var percentText = new SWC.TextBlock
                {
                    Text = "%",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 10, 0),
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                };
                Grid.SetRow(percentText, 0);
                Grid.SetColumn(percentText, 2);
                grid.Children.Add(percentText);

                var rem = new Wpf.Ui.Controls.Button
                {
                    Width = 80,
                    Height = 30,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#d9534f")
                    ),
                    Foreground = Brushes.White,
                    Visibility = canRemove ? Visibility.Visible : Visibility.Hidden,
                };
                rem.Content = new SWC.TextBlock
                {
                    Text = "- Item",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 12,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                rem.Click += (_, __) =>
                {
                    if (rowsPanel.Children.Count > 1)
                        rowsPanel.Children.Remove(border);
                    UpdateHidden();
                };
                Grid.SetRow(rem, 0);
                Grid.SetColumn(rem, 3);
                grid.Children.Add(rem);

                var dd = new SWC.TextBlock
                {
                    Text = dropdownDescription,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                };
                Grid.SetRow(dd, 1);
                Grid.SetColumn(dd, 0);
                grid.Children.Add(dd);

                var vd = new SWC.TextBlock
                {
                    Text = valueDescription,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                Grid.SetRow(vd, 1);
                Grid.SetColumn(vd, 1);
                grid.Children.Add(vd);

                row.Children.Add(grid);
                border.Child = row;
                return border;
            }

            Border CreateTable(JObject presetTable)
            {
                var tableBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 10, 0, 10),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                };

                var tableStack = new StackPanel { Orientation = Orientation.Vertical };

                var nameGrid = new Grid();
                nameGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );
                nameGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(120) }
                );

                var nameBox = new Wpf.Ui.Controls.TextBox
                {
                    Text = presetTable?["name"]?.Value<string>() ?? "",
                    FontSize = 14,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    Margin = new Thickness(0, 0, 5, 0),
                };
                nameBox.TextChanged += (_, __) => UpdateHidden();
                Grid.SetColumn(nameBox, 0);
                nameGrid.Children.Add(nameBox);

                var removeTableBtn = new Wpf.Ui.Controls.Button
                {
                    Width = 120,
                    Height = 30,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#d9534f")
                    ),
                    Foreground = Brushes.White,
                };
                removeTableBtn.Content = new SWC.TextBlock
                {
                    Text = "- Loot Table",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 12,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                removeTableBtn.Click += (_, __) =>
                {
                    var result = System.Windows.MessageBox.Show(
                        "Remove this loot table?",
                        "Confirm",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning
                    );
                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;
                    if (lootTablesPanel.Children.Count > 1)
                        lootTablesPanel.Children.Remove(tableBorder);
                    UpdateHidden();
                };
                Grid.SetColumn(removeTableBtn, 1);
                nameGrid.Children.Add(removeTableBtn);

                tableStack.Children.Add(nameGrid);

                tableStack.Children.Add(
                    new SWC.TextBlock
                    {
                        Text = "Loot Table Name",
                        FontSize = 12,
                        Foreground = Brushes.Gray,
                        FontFamily = new FontFamily(
                            new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                            "#Poppins"
                        ),
                        Margin = new Thickness(0, 2, 0, 5),
                    }
                );

                var statusLine = new SWC.TextBlock
                {
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 14,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 5),
                    Visibility = Visibility.Collapsed,
                };
                tableStack.Children.Add(statusLine);

                var rowsPanel = new StackPanel { Orientation = Orientation.Vertical };
                var items = presetTable?["items"] as JArray;
                if (items != null && items.Count > 0)
                {
                    foreach (var it in items.OfType<JObject>())
                        rowsPanel.Children.Add(CreateRow(true, it, rowsPanel));
                }
                else
                {
                    rowsPanel.Children.Add(CreateRow(false, null, rowsPanel));
                }
                tableStack.Children.Add(rowsPanel);

                var addItemBtn = new Wpf.Ui.Controls.Button
                {
                    Width = 80,
                    Height = 30,
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#71a079")
                    ),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 5, 0, 0),
                };
                addItemBtn.Content = new SWC.TextBlock
                {
                    Text = "+ Item",
                    FontFamily = new FontFamily(
                        new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                        "#Poppins"
                    ),
                    FontSize = 12,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                addItemBtn.Click += (_, __) =>
                {
                    rowsPanel.Children.Add(CreateRow(true, null, rowsPanel));
                    UpdateHidden();
                };
                tableStack.Children.Add(addItemBtn);

                tableBorder.Child = tableStack;
                tableBorder.Tag = new Tuple<Wpf.Ui.Controls.TextBox, StackPanel, SWC.TextBlock>(
                    nameBox,
                    rowsPanel,
                    statusLine
                );
                return tableBorder;
            }

            var stored = _existingSettings.ContainsKey(saveKey)
                ? _existingSettings[saveKey].Value<string>()
                : "[]";
            var storedArr = JArray.Parse(stored);
            if (
                storedArr.Count > 0
                && storedArr.First is JObject
                && storedArr.First["items"] != null
            )
            {
                foreach (var tbl in storedArr.OfType<JObject>())
                    lootTablesPanel.Children.Add(CreateTable(tbl));
            }
            else
            {
                if (storedArr.Count > 0)
                {
                    var legacy = new JObject { ["name"] = "Default", ["items"] = storedArr };
                    lootTablesPanel.Children.Add(CreateTable(legacy));
                }
                else
                {
                    lootTablesPanel.Children.Add(CreateTable(null));
                }
            }

            var addTableButton = new Wpf.Ui.Controls.Button
            {
                Width = 100,
                Height = 30,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 13,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#71a079")
                ),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            addTableButton.Content = new SWC.TextBlock
            {
                Text = "+ Loot Table",
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 13,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            addTableButton.Click += (_, __) =>
            {
                lootTablesPanel.Children.Add(CreateTable(null));
                UpdateHidden();
            };
            container.Children.Add(addTableButton);

            parent.Children.Add(container);
            UpdateHidden();
        }

        public void UpdateProbabilityTableOptions(string saveKey, string[] newDropdownList)
        {
            if (_dynamicPanels == null || !_dynamicPanels.ContainsKey(saveKey))
                return;
            var rootPanel = _dynamicPanels[saveKey];
            var list = newDropdownList?.ToList() ?? new List<string>();
            void UpdateCombo(ComboBox combo)
            {
                if (combo == null)
                    return;
                var current = combo.SelectedItem as string;
                bool ok = !string.IsNullOrEmpty(current) && list.Contains(current);
                if (!ok && !string.IsNullOrEmpty(current) && !list.Contains("<invalid>"))
                {
                    var tmp = new List<string>(list);
                    tmp.Insert(0, "<invalid>");
                    combo.ItemsSource = tmp;
                    combo.SelectedItem = "<invalid>";
                }
                else
                {
                    combo.ItemsSource = list;
                    if (ok)
                        combo.SelectedItem = current;
                    else if (list.Count > 0)
                        combo.SelectedIndex = 0;
                    else
                        combo.SelectedIndex = -1;
                }
            }
            void UpdateRowBorder(Border rowBorder)
            {
                var grid = (Grid)((StackPanel)rowBorder.Child).Children[0];
                var combo = grid.Children.OfType<ComboBox>().FirstOrDefault();
                UpdateCombo(combo);
            }
            foreach (var tableBorder in rootPanel.Children.OfType<Border>())
            {
                var tup =
                    tableBorder.Tag as Tuple<Wpf.Ui.Controls.TextBox, StackPanel, SWC.TextBlock>;
                if (tup != null)
                {
                    var rowsPanel = tup.Item2;
                    foreach (var rowBorder in rowsPanel.Children.OfType<Border>())
                        UpdateRowBorder(rowBorder);
                }
                else
                {
                    UpdateRowBorder(tableBorder);
                }
            }
        }

        private class ConnectionIndicatorItem
        {
            public SWC.TextBlock TitleTextBlock;
            public Wpf.Ui.Controls.Button ConnectionButton;
            public Border Indicator;
            public string Status = "Red";
            public DispatcherTimer Timer;
            public Action OnClick;
            public Action OnStartUp;
        }

        private ConnectionIndicatorItem _connectionIndicatorItem;

        public void AddConnectionStatusIndicator(
            string title,
            string tabName,
            Action onClick,
            Action onStartUp
        )
        {
            EnsureTabExists(tabName);
            if (!_tabContentPanels.ContainsKey(tabName))
                return;
            var parentPanel = _tabContentPanels[tabName];
            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 5, 20, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            var titleTextBlock = new SWC.TextBlock
            {
                Text = title,
                FontSize = 16,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap,
            };
            var connectionButton = new Wpf.Ui.Controls.Button
            {
                Content = "Connect",
                FontSize = 14,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = true,
            };
            var indicator = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Colors.Red),
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Red,
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 1.0,
                },
            };
            container.Children.Add(titleTextBlock);
            container.Children.Add(connectionButton);
            container.Children.Add(indicator);
            parentPanel.Children.Add(container);
            AddSeparator(parentPanel);
            _connectionIndicatorItem = new ConnectionIndicatorItem
            {
                TitleTextBlock = titleTextBlock,
                ConnectionButton = connectionButton,
                Indicator = indicator,
                OnClick = onClick,
                OnStartUp = onStartUp,
                Status = "Red",
            };
            connectionButton.Click += (s, e) =>
            {
                if (GetCurrentStatus() == "Red")
                {
                    SetConnectionStatusIndicator("Orange");
                    onClick();
                    StartConnectionTimer();
                }
            };
            if (onStartUp != null)
            {
                SetConnectionStatusIndicator("Orange");
                onStartUp();
                StartConnectionTimer();
            }
        }

        public void SetConnectionStatusIndicator(string newStatus)
        {
            if (_connectionIndicatorItem == null)
                return;

            var indicator = _connectionIndicatorItem.Indicator;
            var effect = indicator.Effect as DropShadowEffect;
            Color color =
                newStatus == "Green" ? Colors.Green
                : newStatus == "Orange" ? Colors.Orange
                : Colors.Red;

            indicator.Background = new SolidColorBrush(color);
            if (effect != null)
                effect.Color = color;

            if (newStatus == "Green")
            {
                _connectionIndicatorItem.ConnectionButton.Content = "Connected";
                _connectionIndicatorItem.ConnectionButton.IsEnabled = false;
                if (_connectionIndicatorItem.Timer != null)
                {
                    _connectionIndicatorItem.Timer.Stop();
                    _connectionIndicatorItem.Timer = null;
                }
            }
            else if (newStatus == "Orange")
            {
                _connectionIndicatorItem.ConnectionButton.Content = "Connecting...";
                _connectionIndicatorItem.ConnectionButton.IsEnabled = false;
            }
            else
            {
                _connectionIndicatorItem.ConnectionButton.Content = "Connect";
                _connectionIndicatorItem.ConnectionButton.IsEnabled = true;
            }

            _connectionIndicatorItem.Status = newStatus;
        }

        private void StartConnectionTimer()
        {
            if (_connectionIndicatorItem == null)
                return;
            if (_connectionIndicatorItem.Timer != null)
            {
                _connectionIndicatorItem.Timer.Stop();
            }
            _connectionIndicatorItem.Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5),
            };
            _connectionIndicatorItem.Timer.Tick += (s, e) =>
            {
                if (_connectionIndicatorItem.Status != "Green")
                {
                    SetConnectionStatusIndicator("Red");
                }
                _connectionIndicatorItem.Timer.Stop();
                _connectionIndicatorItem.Timer = null;
            };
            _connectionIndicatorItem.Timer.Start();
        }

        public void AddPopupWindow(string title, string text)
        {
            var popup = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                WindowStyle = WindowStyle.SingleBorderWindow,
            };

            var outerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            var textBlock = new SWC.TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = popup.FontFamily,
            };

            foreach (var inline in ParseInlines(text))
                textBlock.Inlines.Add(inline);

            stack.Children.Add(textBlock);

            var okButton = new SWC.Button
            {
                Content = "OK",
                Width = 100,
                Height = 30,
                FontSize = 14,
                FontFamily = popup.FontFamily,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(77, 110, 167)),
                Foreground = Brushes.White,
                Padding = new Thickness(10, 5, 10, 5),
            };
            okButton.Click += (_, __) => popup.Close();

            stack.Children.Add(okButton);
            outerBorder.Child = stack;
            popup.Content = outerBorder;
            popup.ShowDialog();
        }

        public class ProgressHelper
        {
            private readonly Window _window;
            private readonly System.Windows.Controls.ProgressBar _bar;
            private readonly SWC.TextBlock _loadingDescBlock;
            private readonly string _loadingText;
            private readonly int _max;

            public ProgressHelper(
                Window window,
                System.Windows.Controls.ProgressBar bar,
                SWC.TextBlock loadingDescBlock,
                string loadingText,
                int max
            )
            {
                _window = window;
                _bar = bar;
                _loadingDescBlock = loadingDescBlock;
                _loadingText = loadingText;
                _max = max;
            }

            public void Report(int value)
            {
                _bar.Dispatcher.Invoke(() =>
                {
                    _bar.Value = value;
                    _loadingDescBlock.Text = $"{_loadingText} ({value}/{_max})";
                });
            }

            public void Close()
            {
                _window.Dispatcher.Invoke(() => _window.Close());
            }
        }

        public ProgressWindow ShowProgressWindow(
            string title,
            string description,
            string loadingBarDescription,
            int maximum
        )
        {
            var window = new Window
            {
                Title = title,
                Width = 450,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                WindowStyle = WindowStyle.SingleBorderWindow,
            };

            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 5),
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            var descBlock = new SWC.TextBlock
            {
                Text = description,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15),
                FontFamily = window.FontFamily,
            };
            stack.Children.Add(descBlock);

            var countBlock = new SWC.TextBlock
            {
                Text = $"0/{maximum}",
                Foreground = Brushes.White,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                FontFamily = window.FontFamily,
            };
            stack.Children.Add(countBlock);

            var barGrid = new Grid { Height = 25, Margin = new Thickness(0, 0, 0, 12) };

            var progressBar = new System.Windows.Controls.ProgressBar
            {
                Minimum = 0,
                Maximum = maximum,
                Value = 0,
                Height = 25,
                Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                Foreground = new SolidColorBrush(Color.FromRgb(113, 160, 121)),
            };
            barGrid.Children.Add(progressBar);

            var percentBlock = new SWC.TextBlock
            {
                Text = "0%",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = window.FontFamily,
            };
            barGrid.Children.Add(percentBlock);

            stack.Children.Add(barGrid);

            var warning = new SWC.TextBlock
            {
                Text = "Do not close until it has finished.",
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = window.FontFamily,
            };
            stack.Children.Add(warning);

            container.Child = stack;
            window.Content = container;
            window.Show();

            return new ProgressWindow(window, progressBar, percentBlock, countBlock);
        }

        public class ProgressWindow
        {
            private readonly Window _wnd;
            private readonly System.Windows.Controls.ProgressBar _pb;
            private readonly SWC.TextBlock _pct;
            private readonly SWC.TextBlock _cnt;

            public ProgressWindow(
                Window wnd,
                System.Windows.Controls.ProgressBar pb,
                SWC.TextBlock pct,
                SWC.TextBlock cnt
            )
            {
                _wnd = wnd;
                _pb = pb;
                _pct = pct;
                _cnt = cnt;
            }

            public void Report(int value)
            {
                _pb.Dispatcher.Invoke(() =>
                {
                    _pb.Value = value;
                    int max = (int)_pb.Maximum;
                    _pct.Text = $"{(int)(value * 100.0 / max)}%";
                    _cnt.Text = $"{value}/{max}";
                });
            }

            public void Close()
            {
                _wnd.Dispatcher.Invoke(() => _wnd.Close());
            }
        }

        private IEnumerable<Inline> ParseInlines(string text)
        {
            List<Inline> inlines = new List<Inline>();
            Regex regex = new Regex(@"\[link:(?<display>[^|\]]+)\|(?<url>[^\]]+)\]");
            int current = 0;
            foreach (Match match in regex.Matches(text))
            {
                if (match.Index > current)
                {
                    string segment = text.Substring(current, match.Index - current);
                    AddTextSegment(inlines, segment);
                }
                string display = match.Groups["display"].Value;
                string url = match.Groups["url"].Value;
                Hyperlink hyperlink = new Hyperlink(new Run(display));
                hyperlink.NavigateUri = new Uri(url);
                hyperlink.RequestNavigate += (s, e) =>
                {
                    Process.Start(
                        new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }
                    );
                };
                inlines.Add(hyperlink);
                current = match.Index + match.Length;
            }
            if (current < text.Length)
            {
                string segment = text.Substring(current);
                AddTextSegment(inlines, segment);
            }
            return inlines;
        }

        private void AddTextSegment(List<Inline> inlines, string segment)
        {
            string[] lines = segment.Split(new[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                inlines.Add(new Run(lines[i]));
                if (i < lines.Length - 1)
                    inlines.Add(new LineBreak());
            }
        }

        private string GetCurrentStatus()
        {
            return _connectionIndicatorItem?.Status ?? "Red";
        }

        public void ShowUI()
        {
            if (_withUi && _window != null)
            {
                _window.Show();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveValuesInternal();
        }

        private void SaveExitButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var kv in _updateDateTimeHidden)
                kv.Value();
            SaveValuesInternal();
            _window.Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to reset all values to default? The UI will close and you'll have to start it again.",
                "Confirm Reset",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question
            );
            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            _existingSettings = new JObject();
            foreach (var def in _defaultValues)
            {
                if (def.Value is JContainer)
                    _existingSettings[def.Key] = JToken.FromObject(def.Value.ToString());
                else
                    _existingSettings[def.Key] = JToken.FromObject(def.Value);
            }

            foreach (var kv in _dropdowns)
            {
                var combo = kv.Value;
                if (combo.Tag is string tag && tag.Contains("|"))
                {
                    var parts = tag.Split('|');
                    var displayKey = parts[0];
                    var valueKey = parts[1];
                    _existingSettings[displayKey] = "< invalid selection >";
                    _existingSettings[valueKey] = "";
                }
            }

            string json = JsonConvert.SerializeObject(_existingSettings, Formatting.None);
            _cph.SetGlobalVar(_settingsKey, json, true);

            OverwriteUiWithSettings();
            _window.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _window.Close();
        }

        private void SaveValuesInternal()
        {
            string existingJson = _cph.GetGlobalVar<string>(_settingsKey, true);
            if (string.IsNullOrEmpty(existingJson))
                existingJson = "{}";
            JObject oldSettings = JObject.Parse(existingJson);
            JObject newSettings = BuildSettings();
            oldSettings.Merge(
                newSettings,
                new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    MergeNullValueHandling = MergeNullValueHandling.Ignore,
                }
            );
            string mergedJson = oldSettings.ToString(Formatting.None);
            string sanitizedJson = REDACTED(oldSettings);
            _cph.SetGlobalVar(_settingsKey, mergedJson, true);
            _cph.LogInfo($"Tawmae UI ({_titleSuffix} (v{GetDisplayVersion()})) [{Version}]: Saved");
            _cph.LogInfo(sanitizedJson);
            _statusText.Text = "Successfully saved.";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s2, args2) =>
            {
                _statusText.Text = "";
                timer.Stop();
            };
            timer.Start();
        }

        private JObject BuildSettings()
        {
            var settings = new JObject();
            foreach (var tab in _tabContentPanels)
            {
                ExtractSettingsFromPanel(tab.Value, settings);
            }
            return settings;
        }

        private void ExtractSettingsFromPanel(Panel panel, JObject settings)
        {
            foreach (var child in panel.Children)
            {
                if (child is ToggleSwitch tog && tog.Tag != null)
                    settings[tog.Tag.ToString()] = tog.IsChecked == true;
                else if (child is Slider sld && sld.Tag != null)
                    settings[sld.Tag.ToString()] = sld.Value;
                else if (child is Wpf.Ui.Controls.TextBox txt && txt.Tag != null)
                    settings[txt.Tag.ToString()] = txt.Text;
                else if (child is System.Windows.Controls.PasswordBox pwd && pwd.Tag != null)
                    settings[pwd.Tag.ToString()] = pwd.Password;
                else if (child is ComboBox cmb && cmb.Tag != null && cmb.SelectedItem != null)
                {
                    string tag = cmb.Tag.ToString();
                    if (tag.Contains("|") && cmb.SelectedItem is Tuple<string, string> pair)
                    {
                        var keys = tag.Split('|');
                        settings[keys[0]] = pair.Item2;
                        settings[keys[1]] = pair.Item1;
                    }
                    else
                    {
                        settings[tag] = !string.IsNullOrEmpty(cmb.SelectedValuePath)
                            ? cmb.SelectedValue?.ToString()
                            : cmb.SelectedItem.ToString();
                    }
                }
                else if (child is Panel inner)
                    ExtractSettingsFromPanel(inner, settings);
            }
        }

        private void OverwriteUiWithSettings()
        {
            foreach (var tab in _tabContentPanels)
            {
                foreach (var element in tab.Value.Children)
                {
                    if (element is StackPanel container)
                    {
                        foreach (var child in container.Children)
                        {
                            if (child is ToggleSwitch tog)
                            {
                                string key = tog.Tag.ToString();
                                if (_existingSettings.ContainsKey(key))
                                    tog.IsChecked = _existingSettings[key].Value<bool>();
                                else
                                    tog.IsChecked = false;
                            }
                            else if (child is Slider sld)
                            {
                                string key = sld.Tag.ToString();
                                if (_existingSettings.ContainsKey(key))
                                    sld.Value = _existingSettings[key].Value<double>();
                                else
                                    sld.Value = sld.Minimum;
                            }
                            else if (child is Wpf.Ui.Controls.TextBox txt)
                            {
                                string key = txt.Tag.ToString();
                                if (_existingSettings.ContainsKey(key))
                                    txt.Text = _existingSettings[key].Value<string>();
                                else
                                    txt.Text = "";
                            }
                            else if (child is System.Windows.Controls.PasswordBox pwd)
                            {
                                string key = pwd.Tag.ToString();
                                if (_existingSettings.ContainsKey(key))
                                    pwd.Password = _existingSettings[key].Value<string>();
                                else
                                    pwd.Password = "";
                            }
                            else if (child is ComboBox cmb && cmb.Tag != null)
                            {
                                string tag = cmb.Tag.ToString();
                                if (tag.Contains("|"))
                                {
                                    var keys = tag.Split('|');
                                    string displayKey = keys[0];
                                    string valueKey = keys[1];
                                    string savedDisplay =
                                        _existingSettings[displayKey]?.Value<string>() ?? "";
                                    string savedValue =
                                        _existingSettings[valueKey]?.Value<string>() ?? "";
                                    if (cmb.ItemsSource is IEnumerable<Tuple<string, string>> items)
                                    {
                                        var match = items.FirstOrDefault(t =>
                                            t.Item1 == savedValue && t.Item2 == savedDisplay
                                        );
                                        if (match != null)
                                            cmb.SelectedItem = match;
                                        else
                                            cmb.SelectedIndex = -1;
                                    }
                                }
                                else
                                {
                                    string key = tag;
                                    if (_existingSettings.ContainsKey(key))
                                        cmb.SelectedValue = _existingSettings[key].Value<string>();
                                    else
                                        cmb.SelectedIndex = -1;
                                }
                            }
                        }
                    }
                }
            }
        }

        private string REDACTED(JObject settings)
        {
            JObject clone = (JObject)settings.DeepClone();
            Mask(clone);
            return JsonConvert.SerializeObject(clone, Formatting.Indented);

            void Mask(JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if (prop.Value.Type == JTokenType.Object)
                        Mask((JObject)prop.Value);

                    string name = prop.Name;
                    string nameLower = name.ToLowerInvariant();

                    bool sensitive =
                        _passwordKeys.Contains(name)
                        || nameLower.Contains("password")
                        || nameLower.Contains("secret")
                        || nameLower.Contains("key")
                        || nameLower.Contains("token")
                        || nameLower.Contains("username")
                        || nameLower.Contains("handle")
                        || nameLower.Contains("login")
                        || name.EndsWith("ID", StringComparison.Ordinal)
                        || name == "ID";

                    if (sensitive)
                        prop.Value = "[REDACTED]";
                }
            }
        }
    }

    public static class TawmaeExtensions
    {
        public static void AddCustomTitleBar(Tawmae obj, Grid mainGrid)
        {
            var titleGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                Height = 40,
                VerticalAlignment = VerticalAlignment.Top,
            };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            );

            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(titleGrid, 0);
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tawmae.ico");
            if (!File.Exists(iconPath))
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.DownloadFile(
                            "https://raw.githubusercontent.com/tawmae/tawmae/refs/heads/main/assets/page-design/ui_favicon.ico",
                            iconPath
                        );
                    }
                }
                catch { }
            }
            try
            {
                if (File.Exists(iconPath))
                {
                    var icon = new SWC.Image
                    {
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(10, 0, 10, 5),
                        VerticalAlignment = VerticalAlignment.Center,
                        Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute)),
                    };
                    Grid.SetColumn(icon, 0);
                    titleGrid.Children.Add(icon);
                }
            }
            catch { }
            var titleText = new SWC.TextBlock
            {
                Text = $"tawmae - {obj._titleSuffix} v{obj.GetDisplayVersion()}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontFamily = new FontFamily(
                    new Uri("pack://application:,,,/TawmaeUI;component/Font/"),
                    "#Poppins"
                ),
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetColumn(titleText, 1);
            titleGrid.Children.Add(titleText);
            var minBtn = new System.Windows.Controls.Button
            {
                Content = "—",
                FontSize = 14,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(10, 0, 10, 0),
                Cursor = Cursors.Hand,
            };
            minBtn.Click += (s, e) => obj._window.WindowState = WindowState.Minimized;
            Grid.SetColumn(minBtn, 2);
            titleGrid.Children.Add(minBtn);
            var closeBtn = new SWC.Button
            {
                Content = "X",
                FontSize = 14,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            closeBtn.Click += (s, e) => obj._window.Close();
            Grid.SetColumn(closeBtn, 3);
            titleGrid.Children.Add(closeBtn);
            titleGrid.MouseLeftButtonDown += (s, e) => obj._window.DragMove();
            mainGrid.Children.Add(titleGrid);
        }
    }
}

/*
Copyright (c) 2025 tawmae. All rights reserved.

This DLL is licensed for non-commercial use only.
Redistribution and use in binary forms, with or without modification, are permitted for non-commercial purposes only, provided that the following conditions are met:

1. Proper attribution must be given to "tawmae" with reference to the website "www.tawmae.xyz".
2. Commercial use, including integration into paid products or services, is strictly prohibited without explicit written permission.
3. This notice must be included in all copies or substantial portions of the Software.

For commercial licensing, please contact: tawmae@pm.me

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
*/
