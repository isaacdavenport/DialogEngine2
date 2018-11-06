using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace DialogGenerator.UI.Core
{
    public class ModelWrapper<T> : NotifyDataErrorInfoBase
    {
        public ModelWrapper(T model)
        {
            Model = model;
        }

        private void _validatePropertyInternal(string _propertyName, object _currentValue)
        {
            clearErrors(_propertyName);

            _validateDataAnnotations(_propertyName, _currentValue);

            _validateCustomErrors(_propertyName);
        }

        private void _validateDataAnnotations(string _propertyName, object _currentValue)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(Model) { MemberName = _propertyName };
            Validator.TryValidateProperty(_currentValue, context, results);

            foreach (var result in results)
            {
                addErrors(_propertyName, result.ErrorMessage);
            }
        }

        private void _validateCustomErrors(string _propertyName)
        {
            var errors = validateProperty(_propertyName);
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    addErrors(_propertyName, error);
                }
            }
        }

        protected virtual IEnumerable<string> validateProperty(string _propertyName)
        {
            return null;
        }

        protected virtual void setValue<TValue>(TValue value, [CallerMemberName]string _propertyName = null)
        {
            typeof(T).GetProperty(_propertyName).SetValue(Model, value);
            RaisePropertyChanged(_propertyName);
            _validatePropertyInternal(_propertyName, value);
        }

        protected virtual TValue getValue<TValue>([CallerMemberName]string _propertyName = null)
        {
            return (TValue)typeof(T).GetProperty(_propertyName).GetValue(Model);
        }

        public T Model { get; }
    }
}
