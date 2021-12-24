using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using z80view.Sound;

namespace z80view
{
    public class MainWindow : Window
    {
        private EmulatorViewModel _viewModel;
        public static MainWindow Instance { get; private set; }
        private IControl _img;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            DataContext = _viewModel;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            this.Focus();
            this._viewModel.KeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            this.Focus();
            this._viewModel.KeyUp(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _img = ((Grid) Content).Children.First();

            var emulator = new z80emu.Emulator();
            var invalidator = new UIInvalidator(((Grid) Content).Children.First());
            var askfile = new AskUserFile();
            var soundDevice = SoundDeviceFactory.Create((uint)emulator.SoundFrameSize);

            _viewModel = new EmulatorViewModel(invalidator, askfile, soundDevice, emulator);
            this.Closed += (s,e) => _viewModel.Stop();
        }
    }
}
