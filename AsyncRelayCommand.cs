using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Exporter
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> execute;
        private readonly Func<bool> canExecute;

        // Initializes a new instance of AsyncRelayCommand.
        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        // Determines whether the command can execute.
        public bool CanExecute(object parameter) => canExecute?.Invoke() ?? true;

        // Executes the command asynchronously.
        public async void Execute(object parameter)
        {
            await ExecuteAsync();
        }

        // Internal method to invoke the async delegate.
        private async Task ExecuteAsync()
        {
            await execute();
        }

        // Event to raise when the ability to execute changes.
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}