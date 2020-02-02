using DialogGenerator.Core;
using DialogGenerator.UI.Core;
using System;
using System.Collections.Generic;

namespace DialogGenerator.UI.Wrapper
{
    public class ApplicationDataWrapper : ModelWrapper<ApplicationData>
    {
        public ApplicationDataWrapper(ApplicationData _applicationData): 
            base(_applicationData)
        {

        }

        bool _isDecimalFormat(string input)
        {
            Decimal dummy;
            return Decimal.TryParse(input, out dummy);
        }

        protected override IEnumerable<string> validateProperty(string _propertyName)
        {
            List<string> errors = new List<string>();

            switch (_propertyName)
            {
                case nameof(DelayBetweenPhrases):
                    {
                        if (!_isDecimalFormat(DelayBetweenPhrases.ToString()))
                            errors.Add("Decimal number required.");
                        break;
                    }
                case nameof(MaxTimeToPlayFile):
                    {
                        if (!_isDecimalFormat(MaxTimeToPlayFile.ToString()))
                            errors.Add("Decimal number required.");
                        break;
                    }
                case nameof(RadioMovesTimeSensitivity):
                    {
                        if (!_isDecimalFormat(RadioMovesTimeSensitivity.ToString()))
                            errors.Add("Decimal number required.");
                        break;
                    }
                case nameof(RadioMovesSignalStrengthSensitivity):
                    {
                        if (!_isDecimalFormat(RadioMovesSignalStrengthSensitivity.ToString()))
                            errors.Add("Decimal number required.");
                        break;
                    }
            }
            return errors;
        }

        public bool TagUsageCheck
        {
            get { return getValue<bool>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(TagUsageCheck));
            }
        }

        public bool TextDialogsOn
        {
            get { return getValue<bool>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(TextDialogsOn));
            }
        }

        public bool IgnoreRadioSignals
        {
            get { return getValue<bool>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(IgnoreRadioSignals));
            }
        }

        public double MaxTimeToPlayFile
        {
            get { return getValue<double>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(MaxTimeToPlayFile));
            }
        }

        public string CurrentParentalRating
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(CurrentParentalRating));
            }
        }

        public double DelayBetweenPhrases
        {
            get { return getValue<double>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(DelayBetweenPhrases));
            }
        }

        public double RadioMovesTimeSensitivity
        {
            get { return getValue<double>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(RadioMovesTimeSensitivity));
            }
        }

        public double RadioMovesSignalStrengthSensitivity
        {
            get { return getValue<double>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(RadioMovesSignalStrengthSensitivity));
            }
        }


        public bool DebugModeOn
        {
            get { return getValue<bool>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(DebugModeOn));
            }
        }
    }
}
