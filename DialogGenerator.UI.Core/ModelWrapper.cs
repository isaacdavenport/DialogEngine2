using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DialogGenerator.UI.Core
{
    public abstract class ModelWrapper<T> : INotifyPropertyChanged where T : class
    {
        public T Model { get; private set; }

        protected ModelWrapper(T model)
        {
            Model = model;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual TValue getValue<TValue>([CallerMemberName] string _propertyName = null)
        {
            var prop = typeof(T).GetProperty(_propertyName);
            if (prop == null)
                throw new InvalidOperationException($"Property '{_propertyName}' not found on type '{typeof(T).FullName}'.");

            return (TValue)prop.GetValue(Model);
        }

        protected virtual void setValue<TValue>(TValue _value, [CallerMemberName] string _propertyName = null)
        {
            var prop = typeof(T).GetProperty(_propertyName);
            if (prop == null)
                throw new InvalidOperationException($"Property '{_propertyName}' not found on type '{typeof(T).FullName}'.");

            prop.SetValue(Model, _value);
            RaisePropertyChanged(_propertyName);
        }

        protected abstract IEnumerable<string> validateProperty(string _propertyName);

        public IEnumerable<string> GetErrors(string _propertyName)
        {
            return validateProperty(_propertyName);
        }

        public bool HasErrors
        {
            get
            {
                var props = typeof(T).GetProperties();
                foreach (var p in props)
                {
                    var err = validateProperty(p.Name);
                    if (err != null && err.Any()) return true;
                }

                return false;
            }
        }
    }
}
