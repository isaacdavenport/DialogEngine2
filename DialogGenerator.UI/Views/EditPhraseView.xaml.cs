using DialogGenerator.Core;
using DialogGenerator.UI.ViewModels;
using MaterialDesignThemes.Wpf;
using System.IO;
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
            ((EditPhraseViewModel)this.DataContext).PropertyChanged += EditPhraseView_PropertyChanged;          
            SoundRecorder.FilePath = ((EditPhraseViewModel)this.DataContext).EditFileName;
        }

        private void EditPhraseView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals("FileName"))
            {
                this.SoundRecorder.FilePath = ((EditPhraseViewModel)sender).FileName;
            }
        }

        private async void CloseDialogButton_Click(object sender, RoutedEventArgs e)
        {
            await ((EditPhraseViewModel)DataContext).SaveChanges();
            DialogHost.CloseDialogCommand.Execute(null, CloseDialogButton);
        }

        private void CancelDialogButton_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, CancelDialogButton);
        }
    }
}
