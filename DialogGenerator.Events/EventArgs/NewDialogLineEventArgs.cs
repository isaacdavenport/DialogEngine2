using DialogGenerator.Model;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;

namespace DialogGenerator.Events.EventArgs
{
    public class NewDialogLineEventArgs : BindableBase
    {
        private bool mSelected = false;

        public Character Character { get; set; }
        public string DialogLine { get; set; }

        public bool Selected
        {
            get
            {
                return mSelected;
            }

            set
            {
                mSelected = value;
                RaisePropertyChanged();
            }
        }
        

        public DelegateCommand CopyCommand { get; set; }

        public NewDialogLineEventArgs()
        {
            CopyCommand = new DelegateCommand(_copyContentCommand);
        }

        private void _copyContentCommand()
        {
            Clipboard.SetText(DialogLine);
        }
    }
}
