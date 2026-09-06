using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Linq;
using RibbonKit.Controls;
using RibbonKit.Localization;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Phase 6 localization/RTL contracts for RibbonKit-owned menus and customization UI.</summary>
public class RibbonLocalizationTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace RibbonKitNamespace = "urn:ribbonkit";

    [Fact]
    public void Classic_orb_proxy_tooltip_and_automation_name_follow_live_localization() => Sta.Run(() =>
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new PrefixProvider("first");
            var ribbon = new Ribbon();
            var template = new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(Grid), "OrbGlyph"),
            };

            MethodInfo createProxy = typeof(Ribbon).GetMethod(
                "GetOrCreateClassicBackstageOrbProxy",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Classic orb proxy factory was not found.");
            var proxy = Assert.IsType<Button>(createProxy.Invoke(ribbon, [template]));

            Assert.Equal("first:Back", proxy.ToolTip);
            Assert.Equal("first:Back", AutomationProperties.GetName(proxy));
            Assert.IsType<BindingExpression>(
                BindingOperations.GetBindingExpression(proxy, ToolTipService.ToolTipProperty));
            Assert.IsType<BindingExpression>(
                BindingOperations.GetBindingExpression(proxy, AutomationProperties.NameProperty));

            RibbonLocalization.Provider = new PrefixProvider("second");
            Sta.Drain();

            Assert.Equal("second:Back", proxy.ToolTip);
            Assert.Equal("second:Back", AutomationProperties.GetName(proxy));
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    });
    [Fact]
    public void Embedded_resources_cover_every_declared_ribbon_string()
    {
        var resources = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Resources",
            "Strings.resx"));
        string[] declaredNames = resources
            .Root!
            .Elements("data")
            .Select(element => (string?)element.Attribute("name"))
            .OfType<string>()
            .ToArray();
        Assert.Equal(
            Enum.GetNames<RibbonString>().OrderBy(name => name),
            declaredNames.OrderBy(name => name));

        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            foreach (RibbonString key in Enum.GetValues<RibbonString>())
            {
                string value = RibbonLocalization.GetString(key);
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.Contains(key.ToString(), declaredNames);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Chrome_tooltips_and_qat_overflow_keytip_are_resource_backed()
    {
        string root = RepositoryRoot();
        string[] themeFiles =
        [
            "Controls.Window.xaml",
            "Controls.Backstage.xaml",
            "Controls.Shared.xaml",
            "Controls.RibbonChrome.xaml",
            "Controls.Groups.xaml",
        ];
        string[] expectedKeys =
        [
            "Back",
            "Minimize",
            "Maximize",
            "RestoreDown",
            "Close",
            "MoreQuickAccessCommands",
            "MinimizeRibbon",
            "MinimizeWindow",
            "RestoreWindow",
            "CloseWindow",
            "MoreOptions",
        ];

        string[] tooltips = themeFiles
            .Select(fileName => XDocument.Load(Path.Combine(
                root,
                "src",
                "RibbonKit",
                "Themes",
                fileName)))
            .SelectMany(document => document.Descendants().Attributes("ToolTip"))
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.DoesNotContain(tooltips, tooltip => !tooltip.StartsWith('{'));
        foreach (string key in expectedKeys)
        {
            Assert.Contains($"{{localization:RibbonString Key={key}}}", tooltips);
        }

        string keyTipService = File.ReadAllText(Path.Combine(
            root,
            "src",
            "RibbonKit",
            "Controls",
            "KeyTipService.cs"));
        Assert.Contains(
            "RibbonLocalization.GetString(RibbonString.MoreQuickAccessCommands)",
            keyTipService);
        Assert.DoesNotContain("\"More quick access commands\"", keyTipService);
    }

    [Fact]
    public void Provider_can_override_one_string_and_fall_back_for_the_rest()
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new SingleStringProvider(
                RibbonString.CustomizeRibbon,
                "Personalize Ribbon…");

            Assert.Equal(
                "Personalize Ribbon…",
                RibbonLocalization.GetString(RibbonString.CustomizeRibbon));
            Assert.Equal(
                "Collapse the Ribbon",
                RibbonLocalization.GetString(RibbonString.CollapseRibbon));
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    }

    [Fact]
    public void Xaml_localization_binding_refreshes_when_provider_changes() => Sta.Run(() =>
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new PrefixProvider("first");
            var text = Assert.IsType<TextBlock>(XamlReader.Parse("""
                <TextBlock
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:localization="clr-namespace:RibbonKit.Localization;assembly=RibbonKit"
                    Text="{localization:RibbonString Key=Reset}" />
                """));

            Assert.Equal("first:Reset", text.Text);

            RibbonLocalization.Provider = new PrefixProvider("second");
            Sta.Drain(DispatcherPriority.DataBind);

            Assert.Equal("second:Reset", text.Text);
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    });

    [Fact]
    public void Default_application_button_header_refreshes_without_replacing_app_values_or_bindings() =>
        Sta.Run(() =>
        {
            IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
            try
            {
                RibbonLocalization.Provider = new PrefixProvider("first");
                var ribbon = new Ribbon();

                Assert.Equal("first:File", ribbon.ApplicationButtonHeader);
                Assert.Equal("first:File", ribbon.EffectiveApplicationButtonHeader);

                RibbonLocalization.Provider = new PrefixProvider("second");
                Sta.Drain(DispatcherPriority.DataBind);
                Assert.Equal("second:File", ribbon.ApplicationButtonHeader);
                Assert.Equal("second:File", ribbon.EffectiveApplicationButtonHeader);

                ribbon.ApplicationButtonHeader = "Document";
                RibbonLocalization.Provider = new PrefixProvider("third");
                Sta.Drain(DispatcherPriority.DataBind);
                Assert.Equal("Document", ribbon.ApplicationButtonHeader);
                Assert.Equal("Document", ribbon.EffectiveApplicationButtonHeader);

                BindingOperations.SetBinding(
                    ribbon,
                    Ribbon.ApplicationButtonHeaderProperty,
                    new Binding { Source = "Bound header" });
                Assert.NotNull(BindingOperations.GetBindingExpression(
                    ribbon,
                    Ribbon.ApplicationButtonHeaderProperty));
                RibbonLocalization.Provider = new PrefixProvider("fourth");
                Sta.Drain(DispatcherPriority.DataBind);
                Assert.Equal("Bound header", ribbon.ApplicationButtonHeader);
                Assert.Equal("Bound header", ribbon.EffectiveApplicationButtonHeader);

                BindingOperations.ClearBinding(ribbon, Ribbon.ApplicationButtonHeaderProperty);
                Assert.Equal("fourth:File", ribbon.ApplicationButtonHeader);
                Assert.Equal("fourth:File", ribbon.EffectiveApplicationButtonHeader);
            }
            finally
            {
                RibbonLocalization.Provider = previous;
            }
        });

    [Fact]
    public void Application_button_template_uses_the_effective_localized_header()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Controls.RibbonChrome.xaml"));
        XElement applicationButton = Assert.Single(
            document.Descendants(Presentation + "ToggleButton"),
            element => (string?)element.Attribute(Xaml + "Name") == "PART_ApplicationButton");
        string effectiveBinding =
            "{Binding EffectiveApplicationButtonHeader, RelativeSource={RelativeSource AncestorType={x:Type controls:Ribbon}}}";

        Assert.Equal(effectiveBinding, (string?)applicationButton.Attribute("Content"));
        Assert.Equal(effectiveBinding, (string?)applicationButton.Attribute("ToolTip"));
        Assert.Equal(
            effectiveBinding,
            (string?)applicationButton.Attribute("AutomationProperties.Name"));
        Assert.DoesNotContain(
            applicationButton.Attributes().Select(attribute => attribute.Value),
            value => value.Contains("Binding ApplicationButtonHeader,"));
    }

    [Fact]
    public void Custom_tree_marker_uses_the_localized_format() => Sta.Run(() =>
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new SingleStringProvider(
                RibbonString.CustomItemFormat,
                "Custom: {0}");

            var node = new RibbonCustomizeNode(
                new Ribbon(),
                RibbonCustomizeNodeKind.Tab,
                new object(),
                parent: null,
                header: "Home",
                icon: null,
                isCustom: true);

            Assert.Equal("Custom: Home", node.Header);
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    });

    [Fact]
    public void Cached_qat_menu_refreshes_localized_headers_each_time_it_opens() => Sta.Run(() =>
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new PrefixProvider("first");
            var ribbon = new Ribbon();
            ContextMenu menu = Invoke<ContextMenu>(ribbon, "EnsureQatContextMenu");

            Assert.Equal("first:RemoveFromQuickAccessToolbar", Header(menu, 0));
            Assert.Equal("first:ShowQuickAccessToolbarInTitleBar", Header(menu, 2));
            Assert.Equal("first:CustomizeQuickAccessToolbar", Header(menu, 6));

            RibbonLocalization.Provider = new PrefixProvider("second");
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent, menu));

            Assert.Equal("second:RemoveFromQuickAccessToolbar", Header(menu, 0));
            Assert.Equal("second:ShowQuickAccessToolbarAboveRibbon", Header(menu, 3));
            Assert.Equal("second:ShowQuickAccessToolbarBelowRibbon", Header(menu, 4));
            Assert.Equal("second:CustomizeQuickAccessToolbar", Header(menu, 6));
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    });

    [Fact]
    public void Qat_menu_copies_rtl_flow_from_its_disconnected_popup_host() => Sta.Run(() =>
    {
        var host = new Border { FlowDirection = FlowDirection.RightToLeft };
        var menu = new ContextMenu { FlowDirection = FlowDirection.LeftToRight };

        MethodInfo prepare = Assert.IsAssignableFrom<MethodInfo>(typeof(Ribbon).GetMethod(
            "PrepareQatContextMenu",
            BindingFlags.Static | BindingFlags.NonPublic));
        prepare.Invoke(null, [host, menu]);

        Assert.Equal(FlowDirection.RightToLeft, menu.FlowDirection);
    });

    [Fact]
    public void Rtl_submenus_open_to_the_left()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Menus.xaml"));

        XElement trigger = Assert.Single(
            document.Descendants(Presentation + "Trigger"),
            element =>
                (string?)element.Attribute("Property") == "FlowDirection"
                && (string?)element.Attribute("Value") == "RightToLeft");

        XElement[] setters = trigger.Elements(Presentation + "Setter").ToArray();
        Assert.Contains(setters, setter =>
            (string?)setter.Attribute("TargetName") == "PART_Popup"
            && (string?)setter.Attribute("Property") == "Placement"
            && (string?)setter.Attribute("Value") == "Left");
    }

    [Fact]
    public void Ribbon_context_menus_no_longer_embed_their_english_headers()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Controls",
            "Ribbon.cs"));

        Assert.DoesNotContain("Header = \"Add to Quick Access Toolbar\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Header = \"Customize Quick Access Toolbar…\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Header = \"Show Quick Access Toolbar Above the Ribbon\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Customize_and_options_templates_use_localized_built_in_strings()
    {
        string root = RepositoryRoot();
        string customizeSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "RibbonKit",
            "Themes",
            "Controls.Customize.xaml"));
        string optionsSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "RibbonKit",
            "Themes",
            "Controls.OptionsDialog.xaml"));
        string customizeCode = File.ReadAllText(Path.Combine(
            root,
            "src",
            "RibbonKit",
            "Controls",
            "RibbonCustomizePage.cs"));
        string serializerCode = File.ReadAllText(Path.Combine(
            root,
            "src",
            "RibbonKit",
            "Controls",
            "RibbonCustomizationSerializer.cs"));

        string[] embeddedValues =
        [
            "Text=\"Choose commands from the ribbon:\"",
            "Content=\"Add »\"",
            "Content=\"« Remove\"",
            "Content=\"New Tab\"",
            "Content=\"New Group\"",
            "Content=\"Edit…\"",
            "ToolTip=\"Close\"",
            "Content=\"OK\"",
            "Content=\"Cancel\"",
        ];

        foreach (string value in embeddedValues)
        {
            Assert.DoesNotContain(value, customizeSource, StringComparison.Ordinal);
            Assert.DoesNotContain(value, optionsSource, StringComparison.Ordinal);
        }

        Assert.Contains("localization:RibbonString Key=ChooseCommandsFromRibbon", customizeSource);
        Assert.Contains("localization:RibbonString Key=Ok", optionsSource);
        Assert.DoesNotContain("Header = \"New Tab\"", customizeCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = \"Export Ribbon Customization\"", customizeCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Header = tabDto.Header ?? \"New Tab\"", serializerCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Customize_pages_use_directional_rtl_action_labels()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Controls.Customize.xaml"));

        XElement[] triggers = document
            .Descendants(Presentation + "Trigger")
            .Where(element =>
                (string?)element.Attribute("Property") == "FlowDirection"
                && (string?)element.Attribute("Value") == "RightToLeft")
            .ToArray();

        Assert.Equal(2, triggers.Length);
        foreach (XElement trigger in triggers)
        {
            XElement[] setters = trigger.Elements(Presentation + "Setter").ToArray();
            Assert.Contains(setters, setter =>
                (string?)setter.Attribute("TargetName") == "AddActionArrow"
                && (string?)setter.Attribute("Property") == "Text"
                && (string?)setter.Attribute("Value") == "«");
            Assert.Contains(setters, setter =>
                (string?)setter.Attribute("TargetName") == "RemoveActionArrow"
                && (string?)setter.Attribute("Property") == "Text"
                && (string?)setter.Attribute("Value") == "»");
        }
    }

    [Fact]
    public void Customize_template_resolves_live_ltr_and_rtl_labels() => Sta.Run(() =>
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new PrefixProvider("localized");
            var resources = new ResourceDictionary
            {
                Source = new Uri(
                    "/RibbonKit;component/Themes/Controls.Customize.xaml",
                    UriKind.Relative),
            };
            var page = new RibbonQuickAccessPage
            {
                Style = Assert.IsType<Style>(resources[typeof(RibbonQuickAccessPage)]),
                FlowDirection = FlowDirection.LeftToRight,
            };

            page.ApplyTemplate();
            var addLabel = Assert.IsType<TextBlock>(page.Template.FindName("AddActionLabel", page));
            var addArrow = Assert.IsType<TextBlock>(page.Template.FindName("AddActionArrow", page));
            var removeLabel = Assert.IsType<TextBlock>(page.Template.FindName("RemoveActionLabel", page));
            var removeArrow = Assert.IsType<TextBlock>(page.Template.FindName("RemoveActionArrow", page));
            Assert.Equal("localized:Add", addLabel.Text);
            Assert.Equal("»", addArrow.Text);
            Assert.Equal("localized:Remove", removeLabel.Text);
            Assert.Equal("«", removeArrow.Text);

            page.FlowDirection = FlowDirection.RightToLeft;
            Sta.Drain(DispatcherPriority.DataBind);
            Assert.Equal("«", addArrow.Text);
            Assert.Equal(0, Grid.GetColumn(addArrow));
            Assert.Equal(1, Grid.GetColumn(addLabel));
            Assert.Equal("»", removeArrow.Text);
            Assert.Equal(1, Grid.GetColumn(removeArrow));
            Assert.Equal(0, Grid.GetColumn(removeLabel));
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    });

    [Fact]
    public void Localization_lab_localizes_page_headers_and_keeps_direct_qat_items_small()
    {
        string root = RepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "LocalizationRtlDemo.xaml"));
        XElement quickAccessItems = Assert.Single(
            document.Descendants(RibbonKitNamespace + "Ribbon.QuickAccessItems"));
        XElement[] buttons = quickAccessItems
            .Elements(RibbonKitNamespace + "RibbonButton")
            .ToArray();

        Assert.NotEmpty(buttons);
        Assert.All(buttons, button => Assert.Equal("Small", (string?)button.Attribute("Size")));

        string demoCode = File.ReadAllText(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "LocalizationRtlDemo.xaml.cs"));
        string mainCode = File.ReadAllText(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml.cs"));

        Assert.Contains("RibbonString.CustomizeRibbonPage", demoCode);
        Assert.Contains("RibbonString.QuickAccessToolbarPage", demoCode);
        Assert.Contains("RibbonString.CustomizeRibbonPage", mainCode);
        Assert.Contains("RibbonString.QuickAccessToolbarPage", mainCode);
        Assert.DoesNotContain("Header = \"Customize Ribbon\"", demoCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Header = \"Quick Access Toolbar\"", mainCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_lab_exercises_bidirectional_backstage_content()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase",
            "LocalizationRtlDemo.xaml"));
        XElement backstageProperty = Assert.Single(
            document.Descendants(RibbonKitNamespace + "Ribbon.Backstage"));
        XElement backstage = Assert.Single(
            backstageProperty.Elements(RibbonKitNamespace + "Backstage"));
        XElement[] items = backstage
            .Elements(RibbonKitNamespace + "BackstageTabItem")
            .ToArray();

        Assert.Equal(3, items.Length);
        Assert.Contains(items, item => (string?)item.Attribute("Header") == "معلومات — Info");
        Assert.Contains(items, item => (string?)item.Attribute("Header") == "Recent — الأخيرة");
        Assert.Contains(items, item => (string?)item.Attribute("Header") == "خيارات — Options");

        XElement mixedText = Assert.Single(
            backstage.Descendants(Presentation + "TextBlock"),
            element =>
                (string?)element.Attribute("Text")
                == "تقرير ربع سنوي — Quarterly report — ٢٠٢٦");
        Assert.Null(mixedText.Attribute("FlowDirection"));

        XElement documentName = Assert.Single(
            backstage.Descendants(Presentation + "TextBlock"),
            element => (string?)element.Attribute("Text") == "Report-2026-Q3.docx");
        Assert.Equal("LeftToRight", (string?)documentName.Attribute("FlowDirection"));
        Assert.Equal("Left", (string?)documentName.Attribute("TextAlignment"));
    }

    [Fact]
    public void Localization_lab_exercises_inherited_rtl_and_ltr_text_inside_mirrored_inputs()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase",
            "LocalizationRtlDemo.xaml"));

        XElement mixedInput = Assert.Single(
            document.Descendants(RibbonKitNamespace + "RibbonTextBox"),
            element => (string?)element.Attribute(Xaml + "Name") == "RtlMixedTextBox");
        Assert.Null(mixedInput.Attribute("FlowDirection"));
        Assert.Null(mixedInput.Attribute("TextAlignment"));

        XElement documentInput = Assert.Single(
            document.Descendants(RibbonKitNamespace + "RibbonTextBox"),
            element => (string?)element.Attribute(Xaml + "Name") == "RtlDocumentTextBox");
        Assert.Null(documentInput.Attribute("FlowDirection"));
        Assert.Equal("Left", (string?)documentInput.Attribute("TextAlignment"));
        Assert.Equal("en-US", (string?)documentInput.Attribute("Language"));

        XElement popupGroup = Assert.Single(
            document.Descendants(RibbonKitNamespace + "RibbonGroup"),
            element => (string?)element.Attribute(RibbonKitNamespace + "Ribbon.CommandId")
                == "loc.group.popups");
        XElement[] popupItems = popupGroup.Elements().ToArray();
        XElement separator = Assert.Single(
            popupItems,
            element => element.Name == RibbonKitNamespace + "RibbonGroupSeparator"
                && (string?)element.Attribute(Xaml + "Name") == "RtlPopupGroupSeparator");
        int separatorIndex = Array.IndexOf(popupItems, separator);
        Assert.Equal(
            "loc.command.paste",
            (string?)popupItems[separatorIndex - 1].Attribute(RibbonKitNamespace + "Ribbon.CommandId"));
        Assert.Equal(
            "loc.command.select",
            (string?)popupItems[separatorIndex + 1].Attribute(RibbonKitNamespace + "Ribbon.CommandId"));

        XElement scrollingGroup = Assert.Single(
            document.Descendants(RibbonKitNamespace + "RibbonGroup"),
            element => (string?)element.Attribute(RibbonKitNamespace + "Ribbon.CommandId")
                == "loc.group.scrolling");
        XElement[] scrollBars = scrollingGroup
            .Descendants(RibbonKitNamespace + "RibbonScrollBar")
            .ToArray();
        Assert.Equal(2, scrollBars.Length);
        Assert.Contains(
            scrollBars,
            element => (string?)element.Attribute(Xaml + "Name") == "RtlVerticalScrollBar"
                && element.Attribute("Orientation") is null
                && element.Attribute("FlowDirection") is null);
        Assert.Contains(
            scrollBars,
            element => (string?)element.Attribute(Xaml + "Name") == "RtlHorizontalScrollBar"
                && (string?)element.Attribute("Orientation") == "Horizontal"
                && element.Attribute("FlowDirection") is null);
    }

    [Fact]
    public void Localization_lab_follows_showcase_file_surface_and_exercises_bidirectional_application_menu()
    {
        string root = RepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "LocalizationRtlDemo.xaml"));
        XElement applicationMenuProperty = Assert.Single(
            document.Descendants(RibbonKitNamespace + "Ribbon.ApplicationMenu"));
        XElement applicationMenu = Assert.Single(
            applicationMenuProperty.Elements(RibbonKitNamespace + "RibbonApplicationMenu"));

        Assert.Equal("DemoApplicationMenu", (string?)applicationMenu.Attribute(Xaml + "Name"));
        Assert.Contains(
            applicationMenu.Elements(RibbonKitNamespace + "RibbonApplicationMenuItem"),
            item => (string?)item.Attribute("Header") == "حفظ باسم — Save As");

        XElement ltrDocument = Assert.Single(
            applicationMenu.Descendants(RibbonKitNamespace + "RibbonApplicationMenuPaneItem"),
            item => (string?)item.Attribute("Content") == "2  Report-2026-Q3.docx");
        Assert.Equal("LeftToRight", (string?)ltrDocument.Attribute("FlowDirection"));

        string mainCode = File.ReadAllText(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml.cs"));
        string demoCode = File.ReadAllText(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "LocalizationRtlDemo.xaml.cs"));

        Assert.Contains("demo.AttachApplicationSurfaceSource(this);", mainCode);
        Assert.Contains("ApplicationSurfaceChanged?.Invoke", mainCode);
        Assert.Contains("DemoRibbon.ApplicationButtonShape = state.ApplicationButtonShape;", demoCode);
        Assert.Contains("DemoRibbon.ApplicationMenu = state.UsesApplicationMenu ? _applicationMenu : null;", demoCode);
        Assert.Contains("DemoBackstage.Design = state.BackstageDesign;", demoCode);
        Assert.Contains("DemoBackstage.Translucent = state.BackstageTranslucent;", demoCode);
        Assert.Contains("source.ApplicationSurfaceChanged -= OnApplicationSurfaceChanged;", demoCode);
    }

    [Fact]
    public void Showcase_application_menu_footer_uses_localized_conventional_actions()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml"));
        XElement footer = Assert.Single(
            document.Descendants(RibbonKitNamespace + "RibbonApplicationMenu.FooterContent"));
        string[] labels = footer
            .Descendants(RibbonKitNamespace + "RibbonApplicationMenuButton")
            .Select(button => (string?)button.Attribute("Content"))
            .OfType<string>()
            .ToArray();

        Assert.Equal(
            ["{rk:RibbonString Key=Options}", "{rk:RibbonString Key=Exit}"],
            labels);
        Assert.DoesNotContain("RibbonKit Options", labels);
        Assert.DoesNotContain("Exit RibbonKit", labels);
    }

    [Fact]
    public void Application_button_size_changes_refresh_tab_selection_visuals_after_layout()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Controls",
            "RibbonTabControl.cs"));

        Assert.Contains(
            "_applicationButton.SizeChanged -= OnApplicationButtonSizeChanged;",
            source);
        Assert.Contains(
            "_applicationButton.SizeChanged += OnApplicationButtonSizeChanged;",
            source);
        Assert.Contains(
            "private void OnApplicationButtonSizeChanged(object sender, SizeChangedEventArgs e)",
            source);
        Assert.Contains("DispatcherPriority.Loaded", source);
        Assert.Contains("RefreshSelectionVisuals();", source);
    }

    private static string? Header(ContextMenu menu, int index) =>
        Assert.IsType<MenuItem>(menu.Items[index]).Header?.ToString();

    private static T Invoke<T>(object target, string methodName)
    {
        MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic));
        return Assert.IsType<T>(method.Invoke(target, null));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }

    private sealed class SingleStringProvider(RibbonString key, string value) : IRibbonLocalizationProvider
    {
        public string? GetString(RibbonString requested, CultureInfo culture) =>
            requested == key ? value : null;
    }

    private sealed class PrefixProvider(string prefix) : IRibbonLocalizationProvider
    {
        public string GetString(RibbonString key, CultureInfo culture) => $"{prefix}:{key}";
    }
}
