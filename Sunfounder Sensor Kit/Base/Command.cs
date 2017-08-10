using System;
using System.Windows.Input;

namespace FD.Base
{
    public class CommandBase : ICommand
    {
        //These delegates store methods to be called that contains the body of the Execute and CanExecue methods
        //for each particular instance of DelegateCommand.
        private readonly Predicate<object> canExecute;

        private readonly Action<object> execute;

        public CommandBase()
        {
        }

        public CommandBase(Predicate<object> canExecute, Action<object> execute)
        {
            this.canExecute = canExecute;
            this.execute = execute;
        }

        //CanExecute and Execute come from ICommand
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }
            execute(parameter);
        }

        /// <summary>
        ///     Not a part of ICommand, but commonly added so you can trigger a manual refresh on the result of CanExecute.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}