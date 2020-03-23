using DialogGenerator.Core;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogGenerator.UI.Views
{
    /// <summary>
    /// Interaction logic for AvatarArenaView.xaml
    /// </summary>
    public partial class ArenaView : UserControl
    {
        
        public ArenaView()
        {
            InitializeComponent();            
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ArenaViewModel _model = this.DataContext as ArenaViewModel;            

            this.Playground.Children.Clear();

            foreach(ArenaAvatarViewModel _am in _model.PlaygroundAvatars)
            {
                ArenaAvatarView _avatarView = new ArenaAvatarView();
                _avatarView.DataContext = _am;
                _avatarView.SetValue(Canvas.LeftProperty, (double)_am.Left);
                _avatarView.SetValue(Canvas.TopProperty, (double)_am.Top);

                _setBindings(_avatarView, _am);
                
                this.Playground.Children.Add(_avatarView);
            }

            _model.FindClosestAvatarPair(true);
            _model.PlaygroundAvatars.CollectionChanged += PlaygroundAvatars_CollectionChanged;
        }

        private void _setBindings(ArenaAvatarView _avatarView, ArenaAvatarViewModel _am)
        {
            Binding leftBinding = new Binding("Left");
            leftBinding.Source = _am;
            _avatarView.SetBinding(Canvas.LeftProperty, leftBinding);

            Binding topBinding = new Binding("Top");
            topBinding.Source = _am;
            _avatarView.SetBinding(Canvas.TopProperty, topBinding);
        }

        private void PlaygroundAvatars_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            _model.FindClosestAvatarPair(true);
        }

        private void Card_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Card _ic = (Card)sender;
                ArenaAvatarViewModel model = (ArenaAvatarViewModel)_ic.DataContext;
                DataObject dragData = new DataObject(typeof(ArenaAvatarViewModel), model);
                DragDrop.DoDragDrop(_ic, dragData, DragDropEffects.Copy);
            }
        }

        private async void Playground_Drop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(typeof(ArenaAvatarViewModel)))
            {
                ArenaAvatarViewModel _am = e.Data.GetData(typeof(ArenaAvatarViewModel)) as ArenaAvatarViewModel;
                if(_am != null)
                {
                    ArenaViewModel _model = this.DataContext as ArenaViewModel;
                    if(/* !_model.PlaygroundAvatars.Contains(_am) */ _canAdd(_am) )
                    {
                        Point pos = e.GetPosition(sender as IInputElement);
                        ArenaAvatarView _aView = new ArenaAvatarView();

                        _am = _am.Clone();
                        _aView.DataContext = _am;
                        _setBindings(_aView, _am);
                        _am.Left = (int)pos.X;
                        _am.Top = (int)pos.Y;
                        
                        this.Playground.Children.Add(_aView);
                        _model.PlaygroundAvatars.Add(_am);
                        

                        if(this.Playground.Children.Count > 6)
                        {
                            _removeClosest(_aView);
                        }
                    } else
                    {
                        MessageDialogService _dialogService = new MessageDialogService();
                        await _dialogService.ShowMessage("Error", string.Format("Playground already contains maximum count of '{0}' avatars!", _am.Character.CharacterName));
                    }                    
                }
            }
        }

        private bool _canAdd(ArenaAvatarViewModel am)
        {
            ArenaViewModel _model = (ArenaViewModel)this.DataContext;
            if(_model.PlaygroundAvatars.Where(_avm => _avm.Character.CharacterPrefix.Equals(am.Character.CharacterPrefix)).Count() > 1)
            {
                return false;
            }

            return true;
        }

        private void _removeClosest(ArenaAvatarView _Aav)
        {
            Size _shortestDistance = new Size(double.MaxValue, double.MaxValue);
            double _left = (double)_Aav.GetValue(Canvas.LeftProperty);
            double _top = (double)_Aav.GetValue(Canvas.TopProperty);
            ArenaAvatarView _closest = null;
            ArenaViewModel _model = this.DataContext as ArenaViewModel;

            foreach (ArenaAvatarView _aav in this.Playground.Children)
            {
                if(_aav.Equals(_Aav))
                {
                    continue;
                }

                double _diffX = Math.Abs(_left - (double)_aav.GetValue(Canvas.LeftProperty));
                double _diffY = Math.Abs(_top - (double)_aav.GetValue(Canvas.TopProperty));

                if (_diffX < _shortestDistance.Width && _diffY < _shortestDistance.Height)
                {
                    _closest = _aav;
                    _shortestDistance = new Size(_diffX, _diffY);
                }
            }

            if(_closest != null)
            {
                this.Playground.Children.Remove(_closest);
                _model.PlaygroundAvatars.Remove(_closest.DataContext as ArenaAvatarViewModel);
            }
        }

        private void Playground_DragEnter(object sender, DragEventArgs e)
        {
            if(!e.Data.GetDataPresent(typeof(ArenaAvatarViewModel)))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Playground_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            AvatarPair _avatarPair = _model.FindClosestAvatarPair();
            int a = 1;
        }


        private void Playground_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            _model.CanvasBounds = e.NewSize;
            Session.Set(Constants.ARENA_WIDTH, e.NewSize.Width);
            Session.Set(Constants.ARENA_HEIGHT, e.NewSize.Height);
        }

        private void AvatarGallery_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ArenaAvatarView)))
            {
                ArenaAvatarView _aaV = e.Data.GetData(typeof(ArenaAvatarView)) as ArenaAvatarView;
                this.Playground.Children.Remove(_aaV);
                ArenaViewModel _model = this.DataContext as ArenaViewModel;
                _model.PlaygroundAvatars.Remove(_aaV.DataContext as ArenaAvatarViewModel);
            }
        }

        private void AvatarGallery_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(ArenaAvatarView)))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void AvatarGallery_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}
