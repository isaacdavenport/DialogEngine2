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

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// Interaction logic for CharacterSlotControl.xaml
    /// </summary>
    public partial class CharacterSlotControl : UserControl
    {
        public CharacterSlotControl()
        {
            InitializeComponent();
            var _context = this.DataContext;
            this.DataContextChanged += CharacterSlotControl_DataContextChanged;            
        }

        private void CharacterSlotControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var _context = this.DataContext;
        }
    }
}
