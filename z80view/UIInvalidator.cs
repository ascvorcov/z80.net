using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace z80view
{
    public interface IUIInvalidator
    {
        Task Invalidate();
    }

    public class UIInvalidator : IUIInvalidator
    {
        private readonly IControl control;
        public UIInvalidator(IControl control)
        {
            this.control = control;
        }

        public Task Invalidate()
        {
            return Dispatcher.UIThread.InvokeAsync(() => this.control.InvalidateVisual());
        }
    }
}
