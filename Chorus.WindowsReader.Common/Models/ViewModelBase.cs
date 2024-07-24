using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Chorus.WindowsReader.Common.Models
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed, automatically provided by the compiler if not specified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the value of a property and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="storage">Reference to the backing field storing the property value.</param>
        /// <param name="value">The new value to be assigned to the property.</param>
        /// <param name="propertyName">Name of the property, automatically provided by the compiler if not specified.</param>
        /// <returns>True if the value was changed and PropertyChanged event was raised; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
       
        /// <summary>
        /// A command implementation that relays execution to a specified Action and checks if execution is allowed through a specified Func.
        /// </summary>
        public class RelayCommand : ICommand
        {
            private readonly Action execute;
            private readonly Func<bool> canExecute;

            /// <summary>
            /// Initializes a new instance of the RelayCommand class.
            /// </summary>
            /// <param name="execute">The action to execute when the command is invoked.</param>
            /// <param name="canExecute">The function that determines whether the command can execute.</param>
            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
                this.canExecute = canExecute;
            }
           
            /// <summary>
            /// Determines whether the command can execute in its current state.
            /// </summary>
            /// <param name="parameter">Data used by the command. This parameter is ignored in this implementation.</param>
            /// <returns>True if this command can be executed; otherwise, false.</returns>
            public bool CanExecute(object parameter)
            {
                return canExecute == null || canExecute();
            }
            
            /// <summary>
            /// Executes the command action.
            /// </summary>
            /// <param name="parameter">Data used by the command. This parameter is ignored in this implementation.</param>
            public void Execute(object parameter)
            {
                execute();
            }
            
            /// <summary>
            /// Occurs when changes occur that affect whether the command should execute.
            /// </summary>
            public event EventHandler CanExecuteChanged;
            
            /// <summary>
            /// Raises the CanExecuteChanged event to notify that the command's ability to execute has changed.
            /// </summary>
            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// A command implementation for asynchronous operations that relays execution to a specified asynchronous function and checks if execution is allowed through a specified synchronous function.
        /// </summary>
        public class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> execute;
            private readonly Func<bool> canExecute;
            private bool isExecuting;

            /// <summary>
            /// Initializes a new instance of the AsyncRelayCommand class.
            /// </summary>
            /// <param name="execute">The asynchronous function to execute when the command is invoked.</param>
            /// <param name="canExecute">The synchronous function that determines whether the command can execute.</param>
            public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
            {
                this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
                this.canExecute = canExecute;
            }
            
            /// <summary>
            /// Determines whether the command can execute in its current state.
            /// </summary>
            /// <param name="parameter">Data used by the command. This parameter is ignored in this implementation.</param>
            /// <returns>True if this command can be executed; otherwise, false.</returns>
            public bool CanExecute(object parameter)
            {
                return !isExecuting && (canExecute == null || canExecute());
            }
           
            /// <summary>
            /// Executes the command asynchronously.
            /// </summary>
            public async void Execute(object parameter)
            {
                try
                {
                    isExecuting = true;
                    RaiseCanExecuteChanged();
                    await execute();
                }
                finally
                {
                    isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
            /// <summary>
            /// Occurs when changes occur that affect whether the command should execute.
            /// </summary>
            public event EventHandler CanExecuteChanged;
            
            /// <summary>
            /// Raises the CanExecuteChanged event to notify that the command's ability to execute has changed.
            /// </summary>
            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
