using DialogGenerator.Core;

namespace DialogGenerator.UI.Wrapper
{
    public class ApplicationDataWrapper : ModelWrapper<ApplicationData>
    {
        public ApplicationDataWrapper(ApplicationData _applicationData): base(_applicationData)
        {

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

        public bool UseSerialPort
        {
            get { return getValue<bool>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(UseSerialPort));
            }
        }

        public string ComPortName
        {
            get { return getValue<string>(); }
            set
            {
                setValue(value);
                validateProperty(nameof(ComPortName));
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
    }
}
