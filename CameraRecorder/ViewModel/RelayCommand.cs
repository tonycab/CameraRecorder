using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CameraRecorder
{
    using System;
    using System.Windows.Input;

 
        public class RelayCommand : ICommand
        {
            private readonly Action<Object> _execute;
            private readonly Func<object, bool> _canExecute;

            public RelayCommand(Action<object> execute, Func<object,bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
            public void Execute(object parameter) => _execute(parameter);

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    


    //public class RelayCommand: ICommand
    //{
    //    private readonly Action<object> execute;
    //    private readonly Predicate<object> canExecute;

    //    public event EventHandler CanExecuteChanged;

    //    public RelayCommand(Action<object> execute) : this(execute, null) { }

    //    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    //    {
    //        if (execute == null)
    //            throw new ArgumentNullException("execute");

    //        this.execute = execute;
    //        this.canExecute = canExecute;
    //    }

    //    public bool CanExecute(object parameter)
    //    {
    //        return canExecute == null ? true : canExecute(parameter);
    //    }


    //    public void Execute(object parameter)
    //    {
    //        execute(parameter);
    //    }

    //    public void RaiseCanExecuteChanged()
    //    {
    //        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    //    }
    //}
}
