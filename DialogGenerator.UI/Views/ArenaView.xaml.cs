using DialogGenerator.UI.ViewModels;
using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

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
            //if(_model != null)
            //{
            //    if(_model.AvatarGalleryItems.Count > 0)
            //    {
            //        for(int i = 0; i < 2; i++)
            //        {
            //            ArenaAvatarViewModel _avatarModel = _model.AvatarGalleryItems[i];
            //            _avatarModel.Left = (i + 1) * 100;
            //            _avatarModel.Top = (i + 1) * 100;
            //            _avatarModel.Active = true;
            //            _model.PlaygroundAvatars.Add(_avatarModel);
            //        }                    
            //    }
            //}

            this.Playground.Children.Clear();

            foreach(ArenaAvatarViewModel _am in _model.PlaygroundAvatars)
            {
                ArenaAvatarView _avatarView = new ArenaAvatarView();
                _avatarView.DataContext = _am;
                _avatarView.SetValue(Canvas.LeftProperty, (double)_am.Left);
                _avatarView.SetValue(Canvas.TopProperty, (double)_am.Top);
                
                this.Playground.Children.Add(_avatarView);
            }
        }

        private void Card_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
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
                    if(!_model.PlaygroundAvatars.Contains(_am))
                    {
                        Point pos = e.GetPosition(sender as IInputElement);
                        ArenaAvatarView _aView = new ArenaAvatarView();                        
                        _aView.DataContext = _am;
                        _aView.SetValue(Canvas.LeftProperty, pos.X);
                        _aView.SetValue(Canvas.TopProperty, pos.Y);
                        _am.Left = (int)pos.X;
                        _am.Top = (int)pos.Y;

                        this.Playground.Children.Add(_aView);
                        
                        _model.AddAvatarToPlayground(_am);
                    } else
                    {
                        MessageDialogService _dialogService = new MessageDialogService();
                        await _dialogService.ShowMessage("Error", string.Format("Playground already contains '{0}'", _am.Character.CharacterName));
                    }                    
                }
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
    }
}
