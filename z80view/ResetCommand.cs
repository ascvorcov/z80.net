using System;
using System.Windows.Input;

namespace z80view
{
  public class ActionCommand : ICommand
  {
    private readonly Action action;

    public event EventHandler CanExecuteChanged;

    public ActionCommand(Action action)
    {
      this.action = action;
    }
    public bool CanExecute(object parameter)
    {
      return true;
    }

    public void Execute(object parameter)
    {
      action();
    }
  }
}
