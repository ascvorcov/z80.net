using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Live.Avalonia;

using ReactiveUI;

using System;
using System.Diagnostics;
using System.Reactive;

namespace z80view
{
    public class App : Application, ILiveView
    {

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        private static bool IsProduction()
        {
#if DEBUG
            return false;
#else
        return true;
#endif
        }

        // When any of the source files change, a new version of the assembly is
        // built, and this method gets called. The returned content gets embedded
        // into the LiveViewHost window.
        public object CreateView(Window window) => new MainWindow();

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                if (Debugger.IsAttached || IsProduction())
                {
                    // Debugging requires pdb loading etc, so we disable live reloading
                    // during a test run with an attached debugger.
                    var mainWindow = new MainWindow();
                    mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                }
                else
                {
                    // Here, we create a new LiveViewHost, located in the 'Live.Avalonia'
                    // namespace, and pass an ILiveView implementation to it. The ILiveView
                    // implementation should have a parameterless constructor! Next, we
                    // start listening for any changes in the source files. And then, we
                    // show the LiveViewHost window. Simple enough, huh?
                    var window = new LiveViewHost(this, Console.WriteLine);
                    window.StartWatchingSourceFilesForHotReloading();
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    window.Show();
                }

                // Here we subscribe to ReactiveUI default exception handler to avoid app
                // termination in case if we do something wrong in our view models. See:
                // https://www.reactiveui.net/docs/handbook/default-exception-handler/
                //
                // In case if you are using another MV* framework, please refer to its
                // documentation explaining global exception handling.
                RxApp.DefaultExceptionHandler = Observer.Create<Exception>(Console.WriteLine);

                base.OnFrameworkInitializationCompleted();
            }
        }
    }
}
