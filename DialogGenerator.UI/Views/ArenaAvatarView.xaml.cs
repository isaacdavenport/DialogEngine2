using DialogGenerator.Core;
using DialogGenerator.UI.ViewModels;
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
using System.Windows.Media.Animation;
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

        private Storyboard mStoryboard;
        private DoubleAnimation mHorizontalAnimation;
        private DoubleAnimation mVerticalAnimation;

        public ArenaAvatarView()
        {
            InitializeComponent();
            ((ArenaAvatarViewModel)DataContext).PropertyChanged += ArenaAvatarViewModel_PropertyChanged;
            DataContextChanged += ArenaAvatarView_DataContextChanged;
        }

        private void ArenaAvatarView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((ArenaAvatarViewModel)DataContext).PropertyChanged -= ArenaAvatarViewModel_PropertyChanged;
            ((ArenaAvatarViewModel)DataContext).PropertyChanged += ArenaAvatarViewModel_PropertyChanged;
        }

        private void ArenaAvatarViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    ArenaAvatarViewModel _model = (ArenaAvatarViewModel)sender;
                    if (e.PropertyName.Equals("Left"))
                    {
                        if (_model.Left + this.ActualWidth > Session.Get<double>(Constants.ARENA_WIDTH))
                        {
                            double _difference = _model.Left + this.ActualWidth - Session.Get<double>(Constants.ARENA_WIDTH);
                            _model.Left -= ((int)_difference + 5);
                        }

                        if (_model.Left < 0)
                        {
                            _model.Left = 0;
                        }

                    }

                    if (e.PropertyName.Equals("Top"))
                    {
                        if (_model.Top + this.ActualHeight > Session.Get<double>(Constants.ARENA_HEIGHT))
                        {
                            double _difference = _model.Top + this.ActualHeight - Session.Get<double>(Constants.ARENA_HEIGHT);
                            _model.Top -= ((int)_difference + 5);
                        }

                        if (_model.Top < 0)
                        {
                            _model.Top = 0;
                        }

                    }
                });
            }
            catch (System.Threading.Tasks.TaskCanceledException) { }
                                                    
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {                
                mDrag = true;
                mMouseLeftPosition = e.GetPosition(sender as IInputElement).X;
                mMouseTopPosition = e.GetPosition(sender as IInputElement).Y;
                Mouse.Capture(sender as IInputElement);
                
            }

            if(e.RightButton == MouseButtonState.Pressed &&
               e.LeftButton != MouseButtonState.Pressed &&
               e.MiddleButton != MouseButtonState.Pressed)
            {
                DataObject dragData = new DataObject(typeof(ArenaAvatarView), sender as ArenaAvatarView);
                DragDrop.DoDragDrop(sender as ArenaAvatarView, dragData, DragDropEffects.Copy);
            }
            
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
                double _deltaX = e.GetPosition(sender as IInputElement).X - mMouseLeftPosition;
                double _deltaY = e.GetPosition(sender as IInputElement).Y - mMouseTopPosition;
                double _left = (double)this.GetValue(Canvas.LeftProperty);
                double _top = (double)this.GetValue(Canvas.TopProperty);
                _left += _deltaX;
                _top += _deltaY;

                if(_checkBounds(_left, _deltaX, _top, _deltaY))
                {
                    //this.SetValue(Canvas.LeftProperty, _left);
                    //this.SetValue(Canvas.TopProperty, _top);
                    ((ArenaAvatarViewModel)DataContext).Left = (int)_left;
                    ((ArenaAvatarViewModel)DataContext).Top = (int)_top;
                }                
            }
        }

        private bool _checkBounds(double _Left, double _DeltaX, double _Top, double _DeltaY)
        {
            double _newLeft = _Left + _DeltaX;
            double _newTop = _Top + _DeltaY;

            if (_newLeft < 0 || _newTop < 0)
            {
                return false;
            }

            _newLeft += this.ActualWidth;
            _newTop += this.ActualHeight;

            if(_newLeft > Session.Get<double>(Constants.ARENA_WIDTH) ||
                _newTop > Session.Get<double>(Constants.ARENA_HEIGHT))
            {
                return false;
            }

            return true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ArenaAvatarViewModel _model = ((ArenaAvatarViewModel)this.DataContext);

            _model.Width = this.ActualWidth;
            _model.Height = this.ActualHeight;
        }
    }
}
