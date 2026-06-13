using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace GHubFreestyleInjector.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private Grid RootGrid = null!;
    private Grid AppTitleBar = null!;
    private Border StatusPanel = null!;
    private FontIcon StatusIcon = null!;
    private TextBlock StatusText = null!;
    private TextBox InputPathBox = null!;
    private TextBox DbPathBox = null!;
    private TextBlock InputPathText = null!;
    private TextBlock DbPathText = null!;
    private TextBox OutputBox = null!;
    private TextBlock InstallStateText = null!;
    private ToggleSwitch KillGHubBox = null!;
    private ToggleSwitch ForceBox = null!;
    private ToggleSwitch PruneBox = null!;
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        App.LogInfo("MainWindow manual content start");
        Closed += (_, _) =>
        {
            App.LogInfo("MainWindow closed");
            App.KeepAlive();
        };

        Title = "G HUB RGB Freestyle Injector";
        RootGrid = BuildRootGrid();
        Content = RootGrid;
        BuildUi();
        App.LogInfo("MainWindow BuildUi done");

        _hwnd = WindowNative.GetWindowHandle(this);
        App.LogInfo($"HWND acquired: {_hwnd}");
        InitializeWindowChrome();
        TryApplyBackdrop();
        App.LogInfo("Backdrop step done");
        InitializeDefaults();
        RefreshInstallState();
        App.LogInfo("Defaults initialized");
    }

    private static Grid BuildRootGrid()
    {
        var grid = new Grid
        {
            MinWidth = 940,
            MinHeight = 640,
            RowSpacing = 0,
            ColumnSpacing = 0,
            Background = ResolvePageBackground()
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(292) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grid;
    }

    private static Brush ResolvePageBackground()
    {
        var accent = WindowsAccentColor;
        return new SolidColorBrush(IsLightTheme
            ? Blend(accent, Colors.White, 0.90, 0xC0)
            : Blend(accent, Color.FromArgb(0xFF, 0x20, 0x20, 0x20), 0.86, 0xBC));
    }

    private void InitializeWindowChrome()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        try
        {
            AppWindow.Title = "G HUB RGB Freestyle Injector";
            AppWindow.Resize(new SizeInt32(1160, 820));
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = IsLightTheme
                    ? Color.FromArgb(0x20, 0x00, 0x00, 0x00)
                    : Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF);
                AppWindow.TitleBar.ButtonPressedBackgroundColor = IsLightTheme
                    ? Color.FromArgb(0x30, 0x00, 0x00, 0x00)
                    : Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF);
            }
            App.LogInfo($"Window chrome initialized. AppWindowId={AppWindow.Id.Value}");
        }
        catch (Exception ex)
        {
            App.LogInfo("Window chrome fallback: " + ex.Message);
        }
    }

    private void BuildUi()
    {
        AppTitleBar = BuildTitleBar();
        Grid.SetColumnSpan(AppTitleBar, 2);
        RootGrid.Children.Add(AppTitleBar);

        RootGrid.Children.Add(BuildSidebar());

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
            Content = BuildMainSurface()
        };
        Grid.SetRow(scroll, 1);
        Grid.SetColumn(scroll, 1);
        RootGrid.Children.Add(scroll);
    }

    private Grid BuildTitleBar()
    {
        var titleBar = new Grid
        {
            Height = 40,
            Padding = new Thickness(12, 0, 148, 0),
            Background = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00))
        };
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        titleBar.Children.Add(new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(WindowsAccentColor),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon
            {
                Glyph = "\uE7F4",
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.White)
            }
        });

        var title = new TextBlock
        {
            Text = "G HUB RGB Freestyle Injector",
            FontSize = 12,
            Opacity = 0.72,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        Grid.SetColumn(title, 1);
        titleBar.Children.Add(title);
        return titleBar;
    }

    private UIElement BuildSidebar()
    {
        var side = new Grid
        {
            Padding = new Thickness(16, 14, 14, 18),
            Background = new SolidColorBrush(IsLightTheme
                ? Blend(WindowsAccentColor, Colors.White, 0.86, 0x72)
                : Blend(WindowsAccentColor, Color.FromArgb(0xFF, 0x20, 0x20, 0x20), 0.76, 0x78))
        };
        Grid.SetRow(side, 1);
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new StackPanel { Spacing = 2, Margin = new Thickness(4, 0, 0, 18) };
        title.Children.Add(new TextBlock
        {
            Text = "Freestyle Injector",
            FontSize = 18,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        title.Children.Add(new TextBlock
        {
            Text = "Presets RGB para Logitech G HUB",
            FontSize = 12,
            Opacity = 0.68
        });
        side.Children.Add(title);

        var search = new TextBox
        {
            PlaceholderText = "Localizar ação",
            IsTabStop = false,
            CornerRadius = new CornerRadius(18),
            Height = 36,
            Margin = new Thickness(0, 0, 0, 14)
        };
        Grid.SetRow(search, 1);
        side.Children.Add(search);

        var nav = new StackPanel { Spacing = 4 };
        nav.Children.Add(BuildNavItem("\uE895", "Sincronização", selected: true));
        nav.Children.Add(BuildNavItem("\uE7B8", "Instalação"));
        nav.Children.Add(BuildNavItem("\uE8A7", "Logs"));
        nav.Children.Add(BuildNavItem("\uE713", "Configurações"));
        Grid.SetRow(nav, 2);
        side.Children.Add(nav);

        var footer = new StackPanel { Spacing = 4, Margin = new Thickness(4, 0, 0, 0) };
        footer.Children.Add(BuildSmallFooter("\uE946", "CLI local"));
        footer.Children.Add(BuildSmallFooter("\uE930", "G HUB settings.db"));
        Grid.SetRow(footer, 3);
        side.Children.Add(footer);

        return side;
    }

    private UIElement BuildMainSurface()
    {
        var main = new StackPanel
        {
            Spacing = 18,
            Padding = new Thickness(28, 18, 32, 32)
        };

        main.Children.Add(BuildHeader());
        main.Children.Add(BuildInputCard());
        main.Children.Add(BuildOperationCard());
        main.Children.Add(BuildInstallCard());
        main.Children.Add(BuildOutputCard());
        return main;
    }

    private UIElement BuildHeader()
    {
        var header = new Grid { ColumnSpacing = 18 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { Spacing = 5 };
        copy.Children.Add(new TextBlock
        {
            Text = "Sincronização RGB",
            FontSize = 32,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Crie, atualize e reaplique presets Freestyle do G HUB a partir dos seus arquivos Markdown.",
            FontSize = 13,
            Opacity = 0.76,
            TextWrapping = TextWrapping.Wrap
        });
        header.Children.Add(copy);

        StatusText = new TextBlock
        {
            Text = "Pronto para simular",
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        StatusIcon = new FontIcon
        {
            Glyph = "\uE73E",
            FontSize = 15,
            Foreground = new SolidColorBrush(WindowsAccentColor),
            VerticalAlignment = VerticalAlignment.Center
        };
        var statusStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        statusStack.Children.Add(StatusIcon);
        statusStack.Children.Add(StatusText);
        StatusPanel = new Border
        {
            Padding = new Thickness(10, 5, 10, 5),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
            Child = statusStack,
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(StatusPanel, 1);
        header.Children.Add(StatusPanel);
        return header;
    }

    private UIElement BuildInputCard()
    {
        var panel = BuildCardStack("Entradas", "Escolha a pasta das paletas e o banco de dados do G HUB.");

        InputPathBox = new TextBox
        {
            Visibility = Visibility.Collapsed
        };
        InputPathText = BuildPathText();
        panel.Children.Add(InputPathBox);
        panel.Children.Add(BuildSettingActionRow(
            "\uE8B7",
            "Pasta das paletas",
            "Diretório com os arquivos Markdown que serão lidos.",
            InputPathText,
            "Alterar",
            ChooseInput_Click));

        DbPathBox = new TextBox
        {
            Visibility = Visibility.Collapsed
        };
        DbPathText = BuildPathText();
        panel.Children.Add(DbPathBox);
        panel.Children.Add(BuildSettingActionRow(
            "\uE8A5",
            "settings.db do G HUB",
            "Banco SQLite onde o G HUB guarda a configuração local.",
            DbPathText,
            "Alterar",
            ChooseDb_Click));

        return BuildCard(panel);
    }

    private UIElement BuildOperationCard()
    {
        var panel = BuildCardStack("Operação", "Controle o que será feito quando a CLI rodar.");

        var options = new StackPanel { Spacing = 1 };
        KillGHubBox = BuildSettingToggle(
            options,
            "Encerrar G HUB",
            "Fecha o G HUB antes de gravar presets para evitar conflito com o banco de dados.",
            isOn: true);
        ForceBox = BuildSettingToggle(
            options,
            "Forçar regravação",
            "Substitui presets gerenciados já detectados, mesmo sem mudança nos arquivos Markdown.");
        PruneBox = BuildSettingToggle(
            options,
            "Limpar órfãos",
            "Remove presets gerenciados que não têm mais um arquivo Markdown correspondente.");
        panel.Children.Add(options);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 12, 0, 0)
        };
        actions.Children.Add(BuildActionButton("\uE721", "Listar", List_Click));
        actions.Children.Add(BuildActionButton("\uE9D9", "Simular", DryRun_Click));
        actions.Children.Add(BuildActionButton("\uE73E", "Aplicar", Apply_Click, primary: true));
        actions.Children.Add(BuildActionButton("\uE711", "Encerrar G HUB", KillGHub_Click));
        panel.Children.Add(actions);

        return BuildCard(panel);
    }

    private UIElement BuildInstallCard()
    {
        var panel = BuildCardStack("Instalação", "Gerencie a cópia instalada no perfil do usuário.");

        InstallStateText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.78,
            FontSize = 13
        };
        panel.Children.Add(BuildFlatStateRow("\uE946", InstallStateText));

        var wizardButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };
        wizardButtons.Children.Add(BuildActionButton("\uE896", "Instalar", Install_Click));
        wizardButtons.Children.Add(BuildActionButton("\uE895", "Atualizar", Update_Click));
        wizardButtons.Children.Add(BuildActionButton("\uE90F", "Reparar", Repair_Click));
        wizardButtons.Children.Add(BuildActionButton("\uE74D", "Desinstalar", Uninstall_Click));
        panel.Children.Add(wizardButtons);

        return BuildCard(panel);
    }

    private UIElement BuildOutputCard()
    {
        var panel = BuildCardStack("Saída", "Acompanhe exatamente o que a CLI retornou.");
        OutputBox = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            MinHeight = 210,
            PlaceholderText = "Os logs da execução aparecem aqui.",
            CornerRadius = new CornerRadius(8)
        };
        ScrollViewer.SetVerticalScrollBarVisibility(OutputBox, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(OutputBox, ScrollBarVisibility.Auto);
        panel.Children.Add(OutputBox);
        return BuildCard(panel);
    }

    private static StackPanel BuildCardStack(string title, string subtitle)
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        stack.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 12,
            Opacity = 0.66,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, -8, 0, 0)
        });
        return stack;
    }

    private static Border BuildCard(UIElement content)
    {
        return new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(18),
            Background = new SolidColorBrush(IsLightTheme
                ? Blend(WindowsAccentColor, Colors.White, 0.94, 0xB4)
                : Blend(WindowsAccentColor, Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C), 0.90, 0xAC)),
            BorderBrush = new SolidColorBrush(IsLightTheme
                ? Blend(WindowsAccentColor, Color.FromArgb(0xFF, 0xC8, 0xBE, 0xB4), 0.62, 0x4C)
                : Blend(WindowsAccentColor, Color.FromArgb(0xFF, 0x78, 0x78, 0x78), 0.68, 0x50)),
            Child = content
        };
    }

    private static TextBlock BuildPathText()
    {
        return new TextBlock
        {
            FontSize = 12,
            Opacity = 0.72,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        };
    }

    private static UIElement BuildSettingActionRow(
        string glyph,
        string title,
        string description,
        TextBlock value,
        string actionText,
        RoutedEventHandler clickHandler)
    {
        var row = new Grid
        {
            MinHeight = 72,
            Padding = new Thickness(0, 8, 0, 8),
            ColumnSpacing = 16
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        row.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 18,
            Width = 28,
            VerticalAlignment = VerticalAlignment.Center
        });

        var copy = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 500 }
        });
        copy.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Opacity = 0.68,
            TextWrapping = TextWrapping.Wrap
        });
        copy.Children.Add(value);
        Grid.SetColumn(copy, 1);
        row.Children.Add(copy);

        var button = new Button
        {
            Content = actionText,
            MinHeight = 32,
            Padding = new Thickness(14, 5, 14, 5),
            CornerRadius = new CornerRadius(6),
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTipService.SetToolTip(button, actionText);
        button.Click += clickHandler;
        Grid.SetColumn(button, 2);
        row.Children.Add(button);
        return row;
    }

    private static UIElement BuildFlatStateRow(string glyph, FrameworkElement content)
    {
        var row = new Grid { ColumnSpacing = 14, MinHeight = 42 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 17,
            Width = 24,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(content, 1);
        row.Children.Add(content);
        return row;
    }

    private static ToggleSwitch BuildSettingToggle(StackPanel host, string title, string description, bool isOn = false)
    {
        var row = new Grid
        {
            MinHeight = 58,
            Padding = new Thickness(0, 8, 0, 8),
            ColumnSpacing = 18
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 500 }
        });
        copy.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Opacity = 0.68,
            TextWrapping = TextWrapping.Wrap
        });
        row.Children.Add(copy);

        var toggle = new ToggleSwitch
        {
            IsOn = isOn,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(toggle, 1);
        row.Children.Add(toggle);
        host.Children.Add(row);
        return toggle;
    }

    private static Button BuildActionButton(string glyph, string text, RoutedEventHandler clickHandler, bool primary = false)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new FontIcon { Glyph = glyph, FontSize = 15 });
        content.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });

        var button = new Button
        {
            Content = content,
            MinHeight = 36,
            Padding = new Thickness(14, 7, 14, 7),
            CornerRadius = new CornerRadius(6)
        };
        AutomationProperties.SetName(button, text);
        if (primary)
        {
            button.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }

        button.Click += clickHandler;
        return button;
    }

    private static UIElement BuildNavItem(string glyph, string text, bool selected = false)
    {
        var row = new Grid
        {
            Height = 38,
            Padding = new Thickness(12, 0, 10, 0),
            ColumnSpacing = 12,
            Background = selected
                ? new SolidColorBrush(IsLightTheme
                    ? Blend(WindowsAccentColor, Colors.White, 0.78, 0x9C)
                    : Blend(WindowsAccentColor, Color.FromArgb(0xFF, 0x3B, 0x3B, 0x3B), 0.72, 0x86))
                : new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
            CornerRadius = new CornerRadius(6)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        row.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = selected
                ? new SolidColorBrush(WindowsAccentColor)
                : null
        });
        var label = new TextBlock
        {
            Text = text,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(label, 1);
        row.Children.Add(label);

        return row;
    }

    private static bool IsLightTheme
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return !Equals(key?.GetValue("AppsUseLightTheme"), 0);
            }
            catch
            {
                return true;
            }
        }
    }

    private static Color WindowsAccentColor
    {
        get
        {
            var fallback = Color.FromArgb(0xFF, 0x00, 0x78, 0xD4);
            try
            {
                return new UISettings().GetColorValue(UIColorType.Accent);
            }
            catch
            {
            }

            try
            {
                using var dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (dwm?.GetValue("ColorizationColor") is int colorization)
                {
                    return Color.FromArgb(
                        0xFF,
                        (byte)((colorization >> 16) & 0xFF),
                        (byte)((colorization >> 8) & 0xFF),
                        (byte)(colorization & 0xFF));
                }

                using var accent = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
                if (accent?.GetValue("AccentColorMenu") is int accentMenu)
                {
                    return Color.FromArgb(
                        0xFF,
                        (byte)(accentMenu & 0xFF),
                        (byte)((accentMenu >> 8) & 0xFF),
                        (byte)((accentMenu >> 16) & 0xFF));
                }
            }
            catch
            {
            }

            return fallback;
        }
    }

    private static Color Blend(Color foreground, Color background, double backgroundWeight, byte alpha)
    {
        var foregroundWeight = 1.0 - backgroundWeight;
        return Color.FromArgb(
            alpha,
            (byte)Math.Clamp((foreground.R * foregroundWeight) + (background.R * backgroundWeight), 0, 255),
            (byte)Math.Clamp((foreground.G * foregroundWeight) + (background.G * backgroundWeight), 0, 255),
            (byte)Math.Clamp((foreground.B * foregroundWeight) + (background.B * backgroundWeight), 0, 255));
    }

    private static UIElement BuildSmallFooter(string glyph, string text)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Padding = new Thickness(8, 8, 8, 8)
        };
        row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 14, Opacity = 0.76 });
        row.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            Opacity = 0.78,
            VerticalAlignment = VerticalAlignment.Center
        });
        return row;
    }

    private void TryApplyBackdrop()
    {
        var requestedBackdrop = Environment.GetEnvironmentVariable("GHUB_WINUI_BACKDROP");
        if (string.Equals(requestedBackdrop, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(requestedBackdrop, "none", StringComparison.OrdinalIgnoreCase))
        {
            App.LogInfo("Backdrop disabled by GHUB_WINUI_BACKDROP");
            return;
        }

        if (string.Equals(requestedBackdrop, "mica", StringComparison.OrdinalIgnoreCase))
        {
            TryEnableMica();
            return;
        }

        try
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            App.LogInfo("Acrylic backdrop enabled");
        }
        catch (Exception ex)
        {
            App.LogInfo("Acrylic backdrop fallback: " + ex.Message);
            TryEnableMica();
        }
    }

    private void TryEnableMica()
    {
        try
        {
            SystemBackdrop = new MicaBackdrop();
            App.LogInfo("Mica backdrop enabled");
        }
        catch (Exception ex)
        {
            App.LogInfo("Mica backdrop unavailable: " + ex.Message);
        }
    }

    private void InitializeDefaults()
    {
        InputPathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DbPathBox.Text = Path.Combine(localAppData, "LGHUB", "settings.db");
        RefreshPathLabels();
    }

    private async void ChooseInput_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, _hwnd);
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            InputPathBox.Text = folder.Path;
            RefreshPathLabels();
        }
    }

    private async void ChooseDb_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, _hwnd);
        picker.FileTypeFilter.Add(".db");
        picker.FileTypeFilter.Add("*");
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            DbPathBox.Text = file.Path;
            RefreshPathLabels();
        }
    }

    private void RefreshPathLabels()
    {
        if (InputPathText is not null)
        {
            InputPathText.Text = InputPathBox.Text;
        }

        if (DbPathText is not null)
        {
            DbPathText.Text = DbPathBox.Text;
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Instalar", () => InstallOrUpdateAsync(allowSameSource: false));
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Atualizar", () => InstallOrUpdateAsync(allowSameSource: false));
    }

    private async void Repair_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Reparar", async () =>
        {
            if (!Directory.Exists(InstallDir))
            {
                await InstallOrUpdateAsync(allowSameSource: false);
                return;
            }

            CreateLaunchers();
            RegisterUninstallEntry();
            await Task.CompletedTask;
        });
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Desinstalar", async () =>
        {
            if (!Directory.Exists(InstallDir))
            {
                RemoveLaunchers();
                RemoveUninstallEntry();
                return;
            }

            if (IsRunningFromInstallDir)
            {
                ScheduleSelfRemoval();
                Close();
                return;
            }

            Directory.Delete(InstallDir, recursive: true);
            RemoveLaunchers();
            RemoveUninstallEntry();
            await Task.CompletedTask;
        });
    }

    private async Task RunWizardActionAsync(string action, Func<Task> work)
    {
        SetBusy(true);
        try
        {
            AppendLog($"{action}: iniciando.");
            await Task.Run(work);
            RefreshInstallState();
            SetStatus("Instalação", $"{action} concluído.", StatusKind.Success);
            AppendLog($"{action}: concluído.");
        }
        catch (Exception ex)
        {
            SetStatus("Instalação", ex.Message, StatusKind.Error);
            AppendLog($"{action}: erro: {ex}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task InstallOrUpdateAsync(bool allowSameSource)
    {
        var sourceDir = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetDir = Path.GetFullPath(InstallDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!allowSameSource && string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Para atualizar, execute uma versão baixada fora da pasta instalada.");
        }

        var stagingDir = Path.Combine(Path.GetTempPath(), "GHubFreestyleInjector-install-" + Guid.NewGuid().ToString("N"));
        try
        {
            CopyDirectory(sourceDir, stagingDir);
            if (Directory.Exists(InstallDir))
            {
                Directory.Delete(InstallDir, recursive: true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(InstallDir)!);
            Directory.Move(stagingDir, InstallDir);
            CreateLaunchers();
            RegisterUninstallEntry();
        }
        finally
        {
            if (Directory.Exists(stagingDir))
            {
                Directory.Delete(stagingDir, recursive: true);
            }
        }

        await Task.CompletedTask;
    }

    private void RefreshInstallState()
    {
        if (InstallStateText is null) return;

        var installed = File.Exists(InstalledExe);
        var origin = IsRunningFromInstallDir ? "instalada" : "artefato baixado";
        InstallStateText.Text = installed
            ? $"Instalado em {InstallDir}. Esta execução veio de {origin}; artefatos baixados continuam funcionando em modo portátil."
            : $"Modo portátil ativo. Você pode usar este artefato sem instalar; a instalação por usuário só cria atalhos e registro em {InstallDir}.";
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var directory in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(targetDir, Path.GetRelativePath(sourceDir, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var targetFile = Path.Combine(targetDir, Path.GetRelativePath(sourceDir, file));
            File.Copy(file, targetFile, overwrite: true);
        }
    }

    private void CreateLaunchers()
    {
        Directory.CreateDirectory(StartMenuProgramsDir);
        CreateShortcut(
            StartMenuLauncher,
            InstalledExe,
            InstallDir,
            InstalledExe,
            "Sincronizador de presets RGB Freestyle para Logitech G HUB");
        CreateShortcut(
            DesktopLauncher,
            InstalledExe,
            InstallDir,
            InstalledExe,
            "G HUB RGB Freestyle Injector");
        File.WriteAllText(UninstallScript, BuildUninstallScript(), Encoding.UTF8);
        TryDelete(LegacyStartMenuLauncher);
    }

    private static void RemoveLaunchers()
    {
        TryDelete(StartMenuLauncher);
        TryDelete(DesktopLauncher);
        TryDelete(LegacyStartMenuLauncher);
    }

    private void RegisterUninstallEntry()
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallRegistryKey);
        key?.SetValue("DisplayName", "G HUB RGB Freestyle Injector");
        key?.SetValue("DisplayVersion", "0.1.0");
        key?.SetValue("Publisher", "wvxbs");
        key?.SetValue("InstallLocation", InstallDir);
        key?.SetValue("DisplayIcon", InstalledExe);
        key?.SetValue("UninstallString", $"\"{UninstallScript}\"");
        key?.SetValue("QuietUninstallString", $"\"{UninstallScript}\"");
        key?.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key?.SetValue("NoRepair", 0, RegistryValueKind.DWord);
        key?.SetValue("EstimatedSize", EstimateInstallSizeKb(), RegistryValueKind.DWord);
        key?.SetValue("URLInfoAbout", "https://github.com/wvxbs/ghub-rgb-freestyle-injector");

        using var appPath = Registry.CurrentUser.CreateSubKey(AppPathsRegistryKey);
        appPath?.SetValue(string.Empty, InstalledExe);
        appPath?.SetValue("Path", InstallDir);
    }

    private static void RemoveUninstallEntry()
    {
        Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(AppPathsRegistryKey, throwOnMissingSubKey: false);
    }

    private void ScheduleSelfRemoval()
    {
        var script = Path.Combine(Path.GetTempPath(), "GHubFreestyleInjector-uninstall-" + Guid.NewGuid().ToString("N") + ".cmd");
        File.WriteAllText(script, BuildUninstallScript(deleteSelf: true), Encoding.UTF8);
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            ArgumentList = { "/c", script },
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private static string BuildUninstallScript(bool deleteSelf = false)
    {
        var selfDelete = deleteSelf ? "\r\ndel \"%~f0\" >nul 2>nul" : string.Empty;
        return $"""
@echo off
taskkill /IM GHubFreestyleInjector.WinUI.exe /F >nul 2>nul
timeout /t 2 /nobreak >nul 2>nul
rmdir /s /q "{InstallDir}" >nul 2>nul
del "{StartMenuLauncher}" >nul 2>nul
del "{DesktopLauncher}" >nul 2>nul
del "{LegacyStartMenuLauncher}" >nul 2>nul
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\GHubFreestyleInjector /f >nul 2>nul
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\GHubFreestyleInjector.WinUI.exe /f >nul 2>nul
{selfDelete}
""";
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath, string description)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        Type? shellType = null;
        object? shell = null;
        object? shortcut = null;
        try
        {
            shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                throw new InvalidOperationException("WScript.Shell não está disponível para criar atalhos do Windows.");
            }

            shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                throw new InvalidOperationException("Não foi possível iniciar o criador de atalhos do Windows.");
            }

            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: [shortcutPath]);
            var shortcutType = shortcut!.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, [targetPath]);
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, [workingDirectory]);
            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, [$"{iconPath},0"]);
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, [description]);
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, []);
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
        }
    }

    private static int EstimateInstallSizeKb()
    {
        try
        {
            if (!Directory.Exists(InstallDir)) return 0;
            var bytes = Directory.EnumerateFiles(InstallDir, "*", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path).Length)
                .Sum();
            return Math.Max(1, (int)Math.Ceiling(bytes / 1024.0));
        }
        catch
        {
            return 0;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    private static string InstallDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "GHubFreestyleInjector");

    private static string InstalledExe => Path.Combine(InstallDir, "GHubFreestyleInjector.WinUI.exe");

    private static string StartMenuProgramsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        "Programs");

    private static string StartMenuLauncher => Path.Combine(StartMenuProgramsDir, "G HUB RGB Freestyle Injector.lnk");

    private static string LegacyStartMenuLauncher => Path.Combine(StartMenuProgramsDir, "G HUB RGB Freestyle Injector.cmd");

    private static string DesktopLauncher => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "G HUB RGB Freestyle Injector.lnk");

    private static string UninstallScript => Path.Combine(InstallDir, "uninstall.cmd");

    private static string UninstallRegistryKey => @"Software\Microsoft\Windows\CurrentVersion\Uninstall\GHubFreestyleInjector";

    private static string AppPathsRegistryKey => @"Software\Microsoft\Windows\CurrentVersion\App Paths\GHubFreestyleInjector.WinUI.exe";

    private static bool IsRunningFromInstallDir
    {
        get
        {
            var sourceDir = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var targetDir = Path.GetFullPath(InstallDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async void List_Click(object sender, RoutedEventArgs e)
    {
        await RunCliAsync("list", "--managed-only");
    }

    private async void DryRun_Click(object sender, RoutedEventArgs e)
    {
        await RunCliAsync("sync", "--dry-run");
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        var args = new List<string> { "sync" };
        if (KillGHubBox.IsOn) args.Add("--kill-ghub");
        if (ForceBox.IsOn) args.Add("--force");
        if (PruneBox.IsOn) args.Add("--prune");
        await RunCliAsync(args.ToArray());
    }

    private async void KillGHub_Click(object sender, RoutedEventArgs e)
    {
        await RunCliAsync(new[] { "kill-ghub" }, includePaths: false);
    }

    private async Task RunCliAsync(params string[] args)
    {
        await RunCliAsync(args, includePaths: true);
    }

    private async Task RunCliAsync(string[] args, bool includePaths)
    {
        SetBusy(true);
        ClearLog();

        try
        {
            var fullArgs = new List<string>(args);
            if (includePaths)
            {
                fullArgs.AddRange(["--input", InputPathBox.Text, "--db", DbPathBox.Text]);
            }

            var result = await Task.Run(() => ExecuteCli(fullArgs));
            AppendLog(result);
            SetStatus(
                result.ExitCode == 0 ? "Concluído" : "Falhou",
                $"ghub-freestyle terminou com código {result.ExitCode}.",
                result.ExitCode == 0 ? StatusKind.Success : StatusKind.Error);
        }
        catch (Exception ex)
        {
            AppendLog("ERRO: " + ex);
            SetStatus("Erro", ex.Message, StatusKind.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private CliResult ExecuteCli(IEnumerable<string> args)
    {
        var cli = ResolveCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = cli,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Não foi possível iniciar a CLI.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new CliResult(process.ExitCode, output, error);
    }

    private static string ResolveCliPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var localCli = Path.Combine(baseDir, "ghub-freestyle.exe");
        if (File.Exists(localCli))
        {
            return localCli;
        }

        return "ghub-freestyle";
    }

    private void AppendLog(CliResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output)) AppendLog(result.Output.TrimEnd());
        if (!string.IsNullOrWhiteSpace(result.Error)) AppendLog(result.Error.TrimEnd());
    }

    private void AppendLog(string text)
    {
        _log.AppendLine(text);
        OutputBox.Text = _log.ToString();
    }

    private void ClearLog()
    {
        _log.Clear();
        OutputBox.Text = string.Empty;
    }

    private void SetBusy(bool busy)
    {
        RootGrid.Opacity = busy ? 0.72 : 1.0;
        if (busy)
        {
            SetStatus("Rodando", "Executando ghub-freestyle...", StatusKind.Info);
        }
    }

    private void SetStatus(string title, string message, StatusKind kind)
    {
        StatusText.Text = $"{title}: {message}";

        var (background, border) = kind switch
        {
            StatusKind.Success => (
                Color.FromArgb(0x24, 0x10, 0x7C, 0x10),
                Color.FromArgb(0x72, 0x10, 0x7C, 0x10)),
            StatusKind.Error => (
                Color.FromArgb(0x24, 0xC4, 0x2B, 0x1C),
                Color.FromArgb(0x72, 0xC4, 0x2B, 0x1C)),
            _ => (
                Color.FromArgb(0x22, 0x00, 0x78, 0xD4),
                Color.FromArgb(0x66, 0x00, 0x78, 0xD4))
        };

        StatusIcon.Glyph = kind switch
        {
            StatusKind.Success => "\uE73E",
            StatusKind.Error => "\uE783",
            _ => "\uE895"
        };
        StatusIcon.Foreground = new SolidColorBrush(kind switch
        {
            StatusKind.Success => Color.FromArgb(0xFF, 0x0E, 0x7A, 0x0D),
            StatusKind.Error => Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C),
            _ => Color.FromArgb(0xFF, 0x00, 0x78, 0xD4)
        });
        StatusPanel.Background = new SolidColorBrush(background);
        StatusPanel.BorderBrush = new SolidColorBrush(border);
    }

    private enum StatusKind
    {
        Info,
        Success,
        Error
    }

    private sealed record CliResult(int ExitCode, string Output, string Error)
    {
        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] { Output, Error }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
