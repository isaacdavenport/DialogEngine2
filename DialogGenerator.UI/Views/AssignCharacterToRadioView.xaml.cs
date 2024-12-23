﻿using DialogGenerator.UI.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for AsignCharacterToRadioView.xaml
    /// </summary>
    public partial class AssignCharacterToRadioView : UserControl
    {
        public AssignCharacterToRadioView()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if(await (this.DataContext as AssignCharacterToRadioViewModel).SaveRadioSettings())
            {
                DialogHost.CloseDialogCommand.Execute(null, this.CloseButton);
            }            
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, this.CloseButton);
        }
    }
}
