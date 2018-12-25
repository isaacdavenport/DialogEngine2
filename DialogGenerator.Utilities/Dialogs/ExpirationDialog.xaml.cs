using MaterialDesignThemes.Wpf;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DialogGenerator.Utilities.Dialogs
{
    /// <summary>
    /// Interaction logic for ExpirationDialog.xaml
    /// </summary>
    public partial class ExpirationDialog : UserControl,INotifyPropertyChanged
    {
        private readonly TimeSpan mExpirationTime;
        private readonly DispatcherTimer mTimer;
        private DateTime mStartedTime;
        private string mTimeCounter;
        public event PropertyChangedEventHandler PropertyChanged;

        public ExpirationDialog(TimeSpan _exprationTime, string message, string tittle, string _okBtnContent, string _cancelBtnContent)
        {           
            InitializeComponent();
            DataContext = this;

            mExpirationTime = _exprationTime;
            mTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            Message = message;
            Tittle = tittle;
            OKBtnContent = _okBtnContent;
            CancelBtnContent = _cancelBtnContent;
            TimeCounter = "";

            mTimer.Tick += _timer_Tick;
            mStartedTime = DateTime.Now;
            _bindCommands();
            mTimer.Start();
        }

        public ICommand CancelCommand { get; set; } 
        public ICommand ContinueCommand { get; set; }

        private void _timer_Tick(object sender, EventArgs e)
        {
            TimeSpan expired = DateTime.Now - mStartedTime;
            if (expired > mExpirationTime)
                _cancel_Execute();

            TimeCounter = ((int)(mExpirationTime - expired).TotalSeconds).ToString();
        }

        private void _bindCommands()
        {
            CancelCommand = new DelegateCommand(_cancel_Execute);
            ContinueCommand = new DelegateCommand(_continue_Execute);
        }

        private void _continue_Execute()
        {
            mTimer.Stop();
            DialogHost.CloseDialogCommand.Execute(MessageDialogResult.OK, this.OkBtn);
        }

        private void _cancel_Execute()
        {
            mTimer.Stop();
            DialogHost.CloseDialogCommand.Execute(MessageDialogResult.Cancel, this.OkBtn);
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        public string Tittle { get; set; }
        public string Message { get; set; }
        public string OKBtnContent { get; set; }
        public string CancelBtnContent { get; set; }
        public string TimeCounter
        {
            get { return mTimeCounter; }
            set
            {
                mTimeCounter = value;
                OnPropertyChanged(nameof(TimeCounter));
            }
        }
    }
}
