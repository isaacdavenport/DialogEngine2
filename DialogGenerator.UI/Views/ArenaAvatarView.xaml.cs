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
    /// Interaction logic for ArenaAvatarView.xaml
    /// </summary>
    public partial class ArenaAvatarView : UserControl
    {
        private bool mDrag = false;
        private double mMouseLeftPosition = 0.0;
        private double mMouseTopPosition = 0.0;

        public ArenaAvatarView()
        {
            InitializeComponent();
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mDrag = true;
            mMouseLeftPosition = e.GetPosition(sender as IInputElement).X;
            mMouseTopPosition = e.GetPosition(sender as IInputElement).Y;
            Mouse.Capture(sender as IInputElement);
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mDrag = false;
            Mouse.Capture(null);
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (mDrag)
            {
                double deltaX = e.GetPosition(sender as IInputElement).X - mMouseLeftPosition;
                double deltaY = e.GetPosition(sender as IInputElement).Y - mMouseTopPosition;
                double _left = (double)this.GetValue(Canvas.LeftProperty);
                double _top = (double)this.GetValue(Canvas.TopProperty);
                _left += deltaX;
                _top += deltaY;
                this.SetValue(Canvas.LeftProperty, _left);
                this.SetValue(Canvas.TopProperty, _top);
            }
        }
    }
}
