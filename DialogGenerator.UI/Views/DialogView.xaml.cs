using DialogGenerator.Events.EventArgs;
using DialogGenerator.UI.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for DialogView.xaml
    /// </summary>
    public partial class DialogView : UserControl
    {
        private ScrollViewer mScrollViewer;
        public DialogView()
        {
            InitializeComponent();

            ((INotifyCollectionChanged)TextOutput.Items).CollectionChanged += _textOutput_CollectionChanged;

            DialogViewModel model = this.DataContext as DialogViewModel;
            this.ArenaView.DataContext = model.ArenaViewModel;
            this.AssignedRadiosControl.DataContext = model.AssignedRadiosViewModel;
        }

        private void _textOutput_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                if(mScrollViewer == null)
                    mScrollViewer = VisualTreeHelper.GetChild(TextOutput, 0) as ScrollViewer;

                mScrollViewer.ScrollToBottom();
            }
        }

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ArenaView.Height = e.NewSize.Height;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Get height of the main window
            var mainWindowHeight = Application.Current.MainWindow.ActualHeight - 40;

            // Get height of the button bar.
            var bottomHeight = ButtonsPanel.ActualHeight;

            // Get the max height of the dialog lines area. 
            var maxHeight = mainWindowHeight - (350 + bottomHeight);

            // Set it.
            DialogLinesRowDefinition.MaxHeight = maxHeight;


        }

        private void DockPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {            
            DockPanel dPanel = (DockPanel)sender;
            NewDialogLineEventArgs args = dPanel.DataContext as NewDialogLineEventArgs;
            args.Selected = true;
        }
    }
}
