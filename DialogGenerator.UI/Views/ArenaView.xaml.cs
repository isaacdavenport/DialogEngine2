using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.ViewModels;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

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
            _model.RemoveAvatarRequested += _removeAvatarRequested;         

            if(this.Playground.Children.Count > 0)
            {
                foreach(var _arenaAvatarView in this.Playground.Children)
                {
                    ArenaAvatarViewModel _aaVM = ((ArenaAvatarView)_arenaAvatarView).DataContext as ArenaAvatarViewModel;
                    _aaVM.StopAnimation();
                }
                
            }
            this.Playground.Children.Clear();

            foreach(ArenaAvatarViewModel _am in _model.PlaygroundAvatars)
            {
                ArenaAvatarView _avatarView = new ArenaAvatarView();
                _avatarView.DataContext = _am;
                _avatarView.SetValue(Canvas.LeftProperty, (double)_am.Left);
                _avatarView.SetValue(Canvas.TopProperty, (double)_am.Top);

                _setBindings(_avatarView, _am);
                _am.StartAnimation();
                Thread.Sleep(200);
                
                this.Playground.Children.Add(_avatarView);
            }

            _model.FindClosestAvatarPair(true);
            _model.PlaygroundAvatars.CollectionChanged += PlaygroundAvatars_CollectionChanged;
        }

        private void _removeAvatarRequested(object sender, RemoveArenaAvatarViewEventArgs e)
        {
            List<ArenaAvatarView> _itemsToRemove = new List<ArenaAvatarView>();
            ArenaAvatarViewModel _avatarViewModel = e.AvatarModel;
            foreach(var _item in this.Playground.Children)
            {
                if(_item is ArenaAvatarView)
                {
                    ArenaAvatarView _aav = (ArenaAvatarView)_item;
                    if(((ArenaAvatarViewModel)_aav.DataContext).Equals(_avatarViewModel))
                    {
                        _itemsToRemove.Add(_aav);
                    }
                }
            }

            foreach(var _item in _itemsToRemove)
            {
                _removeAvatarFromPlayground(_item);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.Playground.Children.Count > 0)
            {
                foreach (var _arenaAvatarView in this.Playground.Children)
                {
                    ArenaAvatarViewModel _aaVM = ((ArenaAvatarView)_arenaAvatarView).DataContext as ArenaAvatarViewModel;
                    _aaVM.StopAnimation();
                }

            }
            this.Playground.Children.Clear();
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            _model.RemoveAvatarRequested -= _removeAvatarRequested;
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
                //Card _ic = (Card)sender;
                //ArenaAvatarViewModel model = (ArenaAvatarViewModel)_ic.DataContext;
                //DataObject dragData = new DataObject(typeof(ArenaAvatarViewModel), model);
                //DragDrop.DoDragDrop(_ic, dragData, DragDropEffects.Copy);
            }
        }

        private void Card_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Card _ic = (Card)sender;
            ArenaAvatarViewModel model = (ArenaAvatarViewModel)_ic.DataContext;
            DataObject dragData = new DataObject(typeof(ArenaAvatarViewModel), model);
            DragDrop.DoDragDrop(_ic, dragData, DragDropEffects.Copy);
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
                        _am.StartAnimation();
                        

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
                ArenaAvatarViewModel _avModel = _closest.DataContext as ArenaAvatarViewModel;
                _avModel.StopAnimation();
                this.Playground.Children.Remove(_closest);
                _model.PlaygroundAvatars.Remove(_avModel);
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
        }


        private void Playground_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            _model.CanvasBounds = e.NewSize;
            Session.Set(Constants.ARENA_WIDTH, e.NewSize.Width);
            Session.Set(Constants.ARENA_HEIGHT, e.NewSize.Height);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            _model.ControlBounds = e.NewSize;
            Session.Set(Constants.ARENA_TOTAL_WIDTH, e.NewSize.Width);
            Session.Set(Constants.ARENA_TOTAL_HEIGHT, e.NewSize.Height);
        }

        private bool HitGallery(Point pt)
        {
            Rect _rc = new Rect
            {
                X = Session.Get<double>(Constants.ARENA_WIDTH),
                Y = 0,
                Width = Session.Get<double>(Constants.ARENA_TOTAL_WIDTH) - Session.Get<double>(Constants.ARENA_WIDTH),
                Height = Session.Get<double>(Constants.ARENA_TOTAL_HEIGHT)
            };

            return _rc.Contains(pt);
        }

        private void AvatarGallery_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ArenaAvatarView)))
            {
                ArenaAvatarView _aaV = e.Data.GetData(typeof(ArenaAvatarView)) as ArenaAvatarView;
                _removeAvatarFromPlayground(_aaV);             
            }
        }

        private void _removeAvatarFromPlayground(ArenaAvatarView arenaAvatarView)
        {
            ArenaAvatarViewModel _avModel = arenaAvatarView.DataContext as ArenaAvatarViewModel;
            _avModel.StopAnimation();
            this.Playground.Children.Remove(arenaAvatarView);
            ArenaViewModel _model = this.DataContext as ArenaViewModel;
            _model.PlaygroundAvatars.Remove(_avModel);

            ObservableCollection<Character> _characters = Session.Get<ObservableCollection<Character>>(Constants.CHARACTERS);
            var _result = _characters.Select((c, i) => new { i, c }).Where(r => r.c.Equals(_avModel.Character)).First();
            int _firstSelected = Session.Get<int>(Constants.NEXT_CH_1);
            int _secondSelected = Session.Get<int>(Constants.NEXT_CH_2);
            if (_firstSelected == _result.i)
            {
                // Check for the duplicate avatars
                if (_model.PlaygroundAvatars.Where(a => a.Character.Equals(_result.c)).Count() == 0 || (_firstSelected == _secondSelected))
                {
                    Session.Set(Constants.NEXT_CH_1, -1);
                }
            }

            if (_secondSelected == _result.i)
            {
                // Check for the duplicate avatars
                if (_model.PlaygroundAvatars.Where(a => a.Character.Equals(_result.c)).Count() == 0 || (_firstSelected == _secondSelected))
                {
                    Session.Set(Constants.NEXT_CH_2, -1);
                }
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
