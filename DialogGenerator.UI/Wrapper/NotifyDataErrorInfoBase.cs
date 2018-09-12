using GalaSoft.MvvmLight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DialogGenerator.UI.Wrapper
{
    public class NotifyDataErrorInfoBase : ViewModelBase, INotifyDataErrorInfo
    {
        private Dictionary<string, List<string>> mErrorsByPropertyName
            = new Dictionary<string, List<string>>();

        public bool HasErrors => mErrorsByPropertyName.Any();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string _propertyName)
        {
            return mErrorsByPropertyName.ContainsKey(_propertyName)
                ? mErrorsByPropertyName[_propertyName]
                : null;
        }

        protected void addErrors(string _propertyName, string error)
        {
            if (!mErrorsByPropertyName.ContainsKey(_propertyName))
            {
                mErrorsByPropertyName[_propertyName] = new List<string>();
            }

            if (!mErrorsByPropertyName[_propertyName].Contains(error))
            {
                mErrorsByPropertyName[_propertyName].Add(error);
                OnErrorsChanged(_propertyName);
            }
        }

        protected void clearErrors(string _propertyName)
        {
            if (mErrorsByPropertyName.ContainsKey(_propertyName))
            {
                mErrorsByPropertyName.Remove(_propertyName);
                OnErrorsChanged(_propertyName);
            }
        }

        protected virtual void OnErrorsChanged(string _propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(_propertyName));
            base.RaisePropertyChanged(nameof(HasErrors));
        }
    }
}
