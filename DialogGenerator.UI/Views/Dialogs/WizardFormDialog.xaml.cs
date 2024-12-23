﻿using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for WizardFormDialog.xaml
    /// </summary>
    public partial class WizardFormDialog : UserControl,INotifyPropertyChanged
    {
        private IWizardDataProvider mWizardDataProvider;
        private int mSelectedWizardIndex;
        private ILogger mLogger;

        public WizardFormDialog(IWizardDataProvider _wizardDataProvider, ILogger _Logger)
        {            
            InitializeComponent();
            DataContext = this;
            Loaded += _wizardFormDialog_Loaded;

            mWizardDataProvider = _wizardDataProvider;
            mLogger = _Logger;            

            StartWizardCommand = new DelegateCommand(_startWizard_Execute, _startWizard_CanExecute);
        }

        private void _wizardFormDialog_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (mWizardDataProvider == null)
                {
                    mLogger.Info("No loaded wizards!");
                }
                else
                {
                    mLogger.Info(string.Format("Loaded wizards count is {0}!", Wizards.Count));
                }
            } catch (Exception exp)
            {
                mLogger.Info(exp.Message);
            }
            

            this.WizardTypesCbx.Items.Refresh();
            SelectedWizardIndex = Wizards.Count > 0 ? 0 : -1;
            ((DelegateCommand)StartWizardCommand).RaiseCanExecuteChanged();
        }

        private bool _startWizard_CanExecute()
        {
            return SelectedWizardIndex >= 0;
        }

        private void _startWizard_Execute()
        {
            if(StartWizardCommand.CanExecute(null))
                DialogHost.CloseDialogCommand.Execute(SelectedWizardIndex, this.StartBtn);
        }

        public ICommand StartWizardCommand { get; set; }

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
                ((DelegateCommand)StartWizardCommand).RaiseCanExecuteChanged();
            }
        }
    }
}
