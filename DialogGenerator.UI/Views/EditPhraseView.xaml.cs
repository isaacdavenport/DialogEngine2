using DialogGenerator.Core;
using DialogGenerator.UI.ViewModels;
using MaterialDesignThemes.Wpf;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for EditPhraseView.xaml
    /// </summary>
    public partial class EditPhraseView : UserControl
    {
        public EditPhraseView()
        {
            InitializeComponent();
            this.DataContextChanged += EditPhraseView_DataContextChanged;
        }

        private void EditPhraseView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EditPhraseViewModel _model = (EditPhraseViewModel)DataContext;
            this.SoundRecorder.DataContext = _model.MediaRecorderControlViewModel;

            _model.PropertyChanged -= EditPhraseView_PropertyChanged;
            _model.PropertyChanged += EditPhraseView_PropertyChanged;

            _model.MediaRecorderControlViewModel.FilePath = _model.EditFileName;

            //SoundRecorder.FilePath = ((EditPhraseViewModel)this.DataContext).EditFileName;
        }

        private void EditPhraseView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
           if(this.Dispatcher.CheckAccess())
            {
                EditPhraseViewModel _model = (EditPhraseViewModel)DataContext;
                if (e.PropertyName.Equals("FileName"))
                {
                    _model.MediaRecorderControlViewModel.FilePath = ((EditPhraseViewModel)sender).FileName;
                }
            } else
            {
                this.Dispatcher.Invoke(() =>
                {
                    EditPhraseViewModel _model = (EditPhraseViewModel)DataContext;
                    if (e.PropertyName.Equals("FileName"))
                    {
                        _model.MediaRecorderControlViewModel.FilePath = ((EditPhraseViewModel)sender).FileName;
                    }
                });
            }            

        }

        private async void CloseDialogButton_Click(object sender, RoutedEventArgs e)
        {
            await ((EditPhraseViewModel)DataContext).SaveChanges2();
            ((EditPhraseViewModel)DataContext).PropertyChanged -= EditPhraseView_PropertyChanged;
            DialogHost.CloseDialogCommand.Execute(null, CloseDialogButton);
        }

        private void CancelDialogButton_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, CancelDialogButton);
        }

    }
}
