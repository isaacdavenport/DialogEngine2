using System.ComponentModel;

namespace DialogGenerator.Model
{
    public class FileItem:INotifyPropertyChanged
    {
        private bool mIsChecked;

        public string Name { get; set; }
        public string Thumbnail { get; set; }
        public bool IsChecked
        {
            get { return mIsChecked; }
            set
            {
                mIsChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }
    }
}
