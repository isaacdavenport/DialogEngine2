using DialogGenerator.UI.ViewModels;
using System.Collections.Specialized;
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
    }
}
