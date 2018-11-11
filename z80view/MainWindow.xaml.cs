using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace z80view
{
    public class MainWindow : Window
    {
        private EmulatorViewModel _viewModel;
        private IControl _img;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            this._viewModel.KeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            this._viewModel.KeyUp(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoaderPortableXaml.Load(this);

            _img = ((Grid) Content).Children.First();

            _viewModel = new EmulatorViewModel(() =>
                Dispatcher.UIThread.InvokeAsync(() => _img.InvalidateVisual()));
            this.Closed += (s,e) => _viewModel.Stop();
        }
    }
}
