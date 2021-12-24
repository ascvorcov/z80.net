namespace z80view
{
    using Avalonia;
    using Avalonia.ReactiveUI;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class Program
    {
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
        }

        public static void Main(string[] args)
        {
            // Initialization code. Don't use any Avalonia, third-party APIs or any
            // SynchronizationContext-reliant code before AppMain is called: things aren't
            // initialized yet and stuff might break.
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }
}
