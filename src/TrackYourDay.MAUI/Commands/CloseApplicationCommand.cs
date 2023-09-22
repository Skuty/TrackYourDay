using System.Windows;
using System.Windows.Input;

namespace TrackYourDay.MAUI.Commands
{
    internal class CloseApplicationCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}
