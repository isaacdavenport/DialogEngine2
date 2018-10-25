using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for DebugView.xaml
    /// </summary>
    public partial class DebugView : UserControl
    {
        public DebugView()
        {
            InitializeComponent();
        }

        private void _debugView_VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue)
            {
                if(this.LoggerMessagesTabControl.SelectedContent is ListView)
                {
                   var _listView = this.LoggerMessagesTabControl.SelectedContent as ListView;
                   var _gridView = _listView.View as GridView;

                    BindingOperations.GetBindingExpression(_gridView.Columns[0], GridViewColumn.WidthProperty)?.UpdateTarget();
                }
            }
        }

        private void _debugView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.LoggerMessagesTabControl.SelectedContent is ListView)
            {
                var _listView = this.LoggerMessagesTabControl.SelectedContent as ListView;
                var _gridView = _listView.View as GridView;

                BindingOperations.GetBindingExpression(_gridView.Columns[0], GridViewColumn.WidthProperty)?.UpdateTarget();
            }
        }
    }
}
