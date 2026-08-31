using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RibbonKit.Writer;

public partial class MainWindow
{
    private const string WriterOrbTemplateKey = "Writer.Templates.ApplicationOrbChrome";
    private bool _writerIdentityInitialized;

    private void InitializeWriterIdentity()
    {
        if (_writerIdentityInitialized)
            return;

        _writerIdentityInitialized = true;
        Loaded += OnWriterIdentityLoaded;
    }

    private void OnWriterIdentityLoaded(object sender, RoutedEventArgs e) => QueueWriterOrbTemplate();

    private void QueueWriterOrbTemplate()
    {
        if (!_writerIdentityInitialized)
            return;

        _ = Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(ApplyWriterOrbTemplate));
    }

    private void ApplyWriterOrbTemplate()
    {
        if (!_writerIdentityInitialized
            || TryFindResource(WriterOrbTemplateKey) is not DataTemplate writerTemplate)
        {
            return;
        }

        if (GetApplicationOrbPresenter() is ContentPresenter orb)
            orb.ContentTemplate = writerTemplate;
    }

    internal bool HasWriterOrbTemplate()
    {
        ApplyWriterOrbTemplate();
        return GetApplicationOrbPresenter() is ContentPresenter orb
            && ReferenceEquals(orb.ContentTemplate, TryFindResource(WriterOrbTemplateKey));
    }

    private ContentPresenter? GetApplicationOrbPresenter()
    {
        MainRibbon.ApplyTemplate();
        Control? applicationButton = MainRibbon.Template.FindName("PART_ApplicationButton", MainRibbon) as Control
            ?? FindVisualDescendant(MainRibbon, "PART_ApplicationButton") as Control;
        if (applicationButton is null)
            return null;

        applicationButton.ApplyTemplate();
        return applicationButton.Template?.FindName("Orb", applicationButton) as ContentPresenter
            ?? FindVisualDescendant(applicationButton, "Orb") as ContentPresenter;
    }

    private static DependencyObject? FindVisualDescendant(DependencyObject root, string name)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is FrameworkElement element && element.Name == name)
                return child;

            DependencyObject? nested = FindVisualDescendant(child, name);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private void DisposeWriterIdentity()
    {
        Loaded -= OnWriterIdentityLoaded;
        _writerIdentityInitialized = false;
    }
}
