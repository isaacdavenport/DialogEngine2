using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for WizardFormDialog.xaml
    /// </summary>
    public partial class WizardFormDialog : UserControl,INotifyPropertyChanged
    {
        private IWizardDataProvider mWizardDataProvider;
        private int mSelectedWizardIndex;

        public WizardFormDialog(IWizardDataProvider _wizardDataProvider)
        {
            mWizardDataProvider = _wizardDataProvider;
            DataContext = this;
            InitializeComponent();

            SelectedWizardIndex = 0;
        }


        private void _start_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(SelectedWizardIndex, sender as Button);
        }

        private void _close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, sender as Button);
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public List<Wizard> Wizards
        {
            get { return mWizardDataProvider.GetAll(); }
        }

        public int SelectedWizardIndex
        {
            get { return mSelectedWizardIndex; }
            set
            {
                mSelectedWizardIndex = value;
                OnPropertyChanged("SelectedWizardIndex");
            }
        }
    }
}
