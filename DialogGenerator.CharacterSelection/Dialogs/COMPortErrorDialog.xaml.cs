using DialogGenerator.Core;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace DialogGenerator.CharacterSelection.Dialogs
{
    /// <summary>
    /// Interaction logic for COMPortErrorDialog.xaml
    /// </summary>
    public partial class COMPortErrorDialog : UserControl,INotifyPropertyChanged
    {
        private const string mErrorMessage = "No valid COM ports";
        private ObservableCollection<string> mAwailablePorts;
        private string mSelectedPort;

        public COMPortErrorDialog()
        {
            InitializeComponent();
            this.DataContext = this;

            mAwailablePorts = new ObservableCollection<string>();

            _bindCommands();
            _populateData();

            SelectedPort = mAwailablePorts.First();
        }

        public ICommand SaveChangesCommand { get; set; }
        public ICommand RefreshCommand { get; set; }


        private void _bindCommands()
        {
            SaveChangesCommand = new DelegateCommand(_saveChanges_Execute,_saveChanges_CanExecute);
            RefreshCommand = new DelegateCommand(_refresh_Execute);
        }

        private bool _saveChanges_CanExecute()
        {
            return !AvailablePorts.First().Equals(mErrorMessage) 
                   && !string.IsNullOrEmpty(SelectedPort);
        }

        private void _refresh_Execute()
        {
            _populateData();
        }

        private void _saveChanges_Execute()
        {
            ApplicationData.Instance.ComPortName = SelectedPort;

            DialogHost.CloseDialogCommand.Execute(MessageDialogResult.OK, this);
        }

        private void _populateData()
        {
            AvailablePorts.Clear();

            foreach(var port in SerialPort.GetPortNames())
            {
                AvailablePorts.Add(port);
            }

            if(AvailablePorts.Count == 0)
            {
                AvailablePorts.Add(mErrorMessage);
            }

            ((DelegateCommand)SaveChangesCommand).RaiseCanExecuteChanged();
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> AvailablePorts
        {
            get { return mAwailablePorts; }
            set
            {
                mAwailablePorts = value;
                OnPropertyChanged("AvailablePorts");
            }
        }

        public string SelectedPort
        {
            get { return mSelectedPort; }
            set
            {
                mSelectedPort = value;
                OnPropertyChanged("SelectedPort");
            }
        }
    }
}
