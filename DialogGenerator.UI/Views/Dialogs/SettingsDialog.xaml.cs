using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.UI.Wrapper;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : UserControl,INotifyPropertyChanged
    {
        private ApplicationDataWrapper mSettings;
        private IEventAggregator mEventAggregator;
        private CollectionViewSource mDialogs;
        private ObservableCollection<string> mDialogModels = new ObservableCollection<string>();
        private IDialogModelRepository mDialogModelRepository;
        private string mPreferredDialogName = string.Empty;
        private ILogger mLogger;


        public SettingsDialog(IEventAggregator _EventAggregator, IDialogModelRepository _DialogModelRepository, ILogger _Logger)
        {
            DataContext = this;

            WebsiteCommand = new DelegateCommand(_websiteCommand_Execute);
            CloseCommand = new DelegateCommand(_closeCommand_Execute);
            SelectBackgroundCommand = new DelegateCommand(_selectBackgroundImage_Execute);
            mEventAggregator = _EventAggregator;
            mDialogModelRepository = _DialogModelRepository;

            _initDialogModelsList();
            mDialogs = new CollectionViewSource();
            mDialogs.Source = mDialogModels;
            mLogger = _Logger;

            Settings = new ApplicationDataWrapper(ApplicationData.Instance);
            using(SpeechSynthesizer _speech = new SpeechSynthesizer())
            {
                foreach(var _voiceType in _speech.GetInstalledVoices())
                {
                    VoiceTypesCollection.Add(_voiceType.VoiceInfo.Name);
                }
            }

            InitializeComponent();

            _handleDialogNameEntry(textBox.Text);
            resultStackBorder.Visibility = Visibility.Collapsed;
            
        }        

        private void _initDialogModelsList()
        {
            
            foreach (var _dlgInfo in mDialogModelRepository.GetAll())
            {
                foreach( var _dialog in _dlgInfo.ArrayOfDialogModels)
                {
                    mDialogModels.Add(_dialog.Name);
                }
            }
        }

        public ICommand WebsiteCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        public ICommand SelectBackgroundCommand { get; set; }

        private void _websiteCommand_Execute()
        {
            Process.Start(ApplicationData.Instance.WebsiteUrl);
        }

        private void _closeCommand_Execute()
        {
            Settings.Model.Save();
            DialogHost.CloseDialogCommand.Execute(null,this.CloseBtn);

            if(Settings.HasPreferredDialog)
            {
                mLogger.Debug($"SETTINGS DIALOG - Preferred dialog set to '{Settings.PreferredDialogName}'!");
            }
        }

        private void _selectBackgroundImage_Execute()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = ApplicationData.Instance.ImagesDirectory;
            if(openFileDialog.ShowDialog() == true)
            {
                Settings.BackgroundImage = openFileDialog.FileName;
                mEventAggregator.GetEvent<ArenaBackgroundChangedEvent>().Publish();
            }
        }

        public ICollectionView Dialogs
        {
            get
            {
                return mDialogs.View;
            }
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        public ApplicationDataWrapper Settings
        {
            get { return mSettings; }
            set
            {
                mSettings = value;
                OnPropertyChanged("Settings");
            }
        }

        public ObservableCollection<string> VoiceTypesCollection { get; set; } = new ObservableCollection<string>();

        public string Website
        {
            get { return ApplicationData.Instance.WebsiteUrl; }
        }

        public string Version
        {
            get { return $"v: { FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Instance.RootDirectory, "DialogGenerator.exe")).FileVersion.ToString()}"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            string query = (sender as TextBox).Text;
            _handleDialogNameEntry(query);
        }

        private void _handleDialogNameEntry(string query)
        {
            var data = mDialogModels;
            var border = (resultStack.Parent as ScrollViewer).Parent as Border;

            resultStack.Children.Clear();
            if (string.IsNullOrEmpty(query))
            {
                foreach (var item in data)
                {
                    addEntry(item);
                }
            }
            else
            {
                foreach (var item in data)
                {
                    if (item.ToLower().Contains(query.ToLower()))
                    {
                        addEntry(item);
                    }
                }
            }

            if(resultStack.Children.Count == 0)
            {
                border.Visibility = Visibility.Collapsed;
            } else
            {
                border.Visibility = Visibility.Visible;
            }
        }

        private void addEntry(string item)
        {
            TextBlock block = new TextBlock();
            block.Text = item;
            block.Margin = new Thickness(2, 3, 2, 3);
            block.Cursor = Cursors.Hand;

            block.MouseLeftButtonUp += (sender, e) =>
            {
                //textBox.Text = (sender as TextBlock).Text;
                Settings.PreferredDialogName = (sender as TextBlock).Text;
            };

            block.MouseEnter += (sender, e) =>
            {
                TextBlock b = sender as TextBlock;
                b.Background = Brushes.PeachPuff;
            };

            block.MouseLeave += (sender, e) =>
            {
                TextBlock b = sender as TextBlock;
                b.Background = Brushes.Transparent;
            };

            resultStack.Children.Add(block);
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(resultStackBorder.Visibility == Visibility.Collapsed)
                resultStackBorder.Visibility = Visibility.Visible;
            
        }

        private void textBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (resultStackBorder.Visibility == Visibility.Visible)
                resultStackBorder.Visibility = Visibility.Collapsed;
            else
                resultStackBorder.Visibility = Visibility.Visible;
        }


        private void UIElement_OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                TextBox tb = sender as TextBox;
                if (tb != null)
                {
                    var val = Convert.ToInt16(tb.Text);
                    if (val <= 0) tb.Text = Convert.ToString(1);
                    if (val > 10) tb.Text = Convert.ToString(10);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("The value must be an integer between 1 and 10!");
            }
        }
    }
}
