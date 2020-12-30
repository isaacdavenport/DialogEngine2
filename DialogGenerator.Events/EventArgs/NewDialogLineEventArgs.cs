using DialogGenerator.Model;
using Prism.Commands;
using System;
using System.Windows;

namespace DialogGenerator.Events.EventArgs
{
    public class NewDialogLineEventArgs
    {
        public Character Character { get; set; }
        public string DialogLine { get; set; }

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
