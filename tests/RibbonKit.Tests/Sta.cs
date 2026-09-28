using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Linq;
using RibbonKit.Theming;

namespace RibbonKit.Tests;

/// <summary>
/// Runs WPF test bodies on one STA thread with a live <see cref="Dispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// WPF objects are thread-affine and must be created on an STA thread, but xunit's runner threads
/// are MTA. WPF also caches theme resources and WindowChrome objects process-wide, so moving each
/// test to a new STA can make later tests read objects owned by a dead dispatcher. The shared host
/// serializes WPF bodies while each test still owns and closes the windows it creates.
/// </para>
/// <para>
/// The host does not create an Application or show a window by itself. Tests that need realized
/// windows create them offscreen and close them before returning.
/// </para>
/// </remarks>
internal static class Sta
{
    private static readonly TimeSpan BodyTimeout = TimeSpan.FromSeconds(30);
    private static readonly object Gate = new();
    private static readonly Lazy<Dispatcher> Host = new(StartHost);
    private static Application? _application;

    /// <summary>Runs <paramref name="body"/> on the shared STA, rethrowing any failure here.</summary>
    public static void Run(Action body)
    {
        lock (Gate)
        {
            var dispatcher = Host.Value;
            if (dispatcher.CheckAccess())
            {
                body();
                return;
            }

            var completed = new TaskCompletionSource<ExceptionDispatchInfo?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            dispatcher.BeginInvoke(new Action(() =>
            {
                ExceptionDispatchInfo? failure = null;
                try { body(); }
                catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
                finally { completed.SetResult(failure); }
            }));

            if (!completed.Task.Wait(BodyTimeout))
            {
                throw new TimeoutException(
                    $"The STA test body did not finish within {BodyTimeout.TotalSeconds:0}s — most likely " +
                    "it is waiting on dispatcher work that never gets pumped.");
            }

            completed.Task.Result?.Throw();
        }
    }

    /// <summary>Uses the single WPF Application allowed in this test process.</summary>
    public static Application UseApplication(bool showcaseResources = false)
    {
        if (!Host.Value.CheckAccess())
            throw new InvalidOperationException("Application access must run inside Sta.Run.");

        if (_application is null)
        {
            _application = new Application
            { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        _application.Resources = showcaseResources
            ? LoadShowcaseResources()
            : new ResourceDictionary();
        return _application;
    }

    private static ResourceDictionary LoadShowcaseResources()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
            directory = directory.Parent;
        if (directory is null) throw new FileNotFoundException("RibbonKit.sln was not found.");

        var appXaml = XDocument.Load(Path.Combine(
            directory.FullName, "samples", "RibbonKit.Showcase", "App.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var dictionary = appXaml.Root!.Element(presentation + "Application.Resources")!
            .Element(presentation + "ResourceDictionary")!;
        var context = new ParserContext
        {
            BaseUri = new Uri("pack://application:,,,/RibbonKit.Showcase;component/App.xaml"),
        };
        return (ResourceDictionary)XamlReader.Parse(dictionary.ToString(), context);
    }

    /// <summary>Releases per-test windows, theme settings, and application resources.</summary>
    public static void ResetApplication()
    {
        if (!Host.Value.CheckAccess())
            throw new InvalidOperationException("Application reset must run inside Sta.Run.");
        if (_application is null) return;

        foreach (Window window in _application.Windows.Cast<Window>().ToArray())
            window.Close();
        ThemeManager.ClearAccent(_application);
        ThemeManager.SetDarkMode(_application, false);
        ThemeManager.SetAccentedTitleBar(_application, false);
        ThemeManager.SetTitleBarBackdrop(_application, false);
        ThemeManager.Apply(_application, RibbonTheme.Office2024);
        _application.Resources = new ResourceDictionary();
        // ThemeManager tracks its last dictionary statically. The next test gets a new
        // Application.Resources scope and must exercise its first-Apply path again.
        typeof(ThemeManager).GetField("_current", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, null);
    }

    private static Dispatcher StartHost()
    {
        var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            ready.SetResult(dispatcher);
            Dispatcher.Run();
        }) { IsBackground = true, Name = "RibbonKit test STA" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return ready.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs everything already queued on this thread's dispatcher at <paramref name="priority"/>
    /// or above, then returns.
    /// </summary>
    /// <remarks>
    /// RibbonKit defers state changes that must not happen mid-dispatch (returning borrowed menu
    /// items, publishing overflow) to <see cref="DispatcherPriority.Background"/>. Nothing pumps
    /// the queue in a test — there is no message loop — so a test that asserts on the result of
    /// deferred work must call this first. Invoking an empty callback at the same priority is the
    /// pump: same-priority operations run in the order they were queued, so ours going last means
    /// everything before it has already run.
    /// </remarks>
    public static void Drain(DispatcherPriority priority = DispatcherPriority.Background) =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, priority);
}
