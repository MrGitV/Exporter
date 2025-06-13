using System;
using System.Windows.Input;

namespace Exporter
{
    public class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        // Initializes a new instance of RelayCommand.
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        // Determines whether the command can execute.
        public bool CanExecute(object parameter) => canExecute?.Invoke() ?? true;

        // Executes the command action.
        public void Execute(object parameter) => execute();

        // Occurs when changes occur that affect whether or not the command should execute.
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // Notify WPF that the ability of the command to execute may have changed
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

    }
}