using DialogGenerator.Model;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DialogGenerator.UI.ViewModels
{
    public class ArenaAvatarViewModel : BindableBase
    {
        private Character mCharacter;
        private int mLeft;
        private int mTop;
        private bool mActive = false;
        private bool mInPlayground = false;

        public Character Character
        {
            get
            {
                return mCharacter;
            }

            set
            {
                mCharacter = value;
                RaisePropertyChanged();
            }
        }

        public int Left
        {
            get
            {
                return mLeft;
            }

            set
            {
                mLeft = value;
                RaisePropertyChanged();
            }
        }

        public int Top
        {
            get
            {
                return mTop;
            }

            set
            {
                mTop = value;
                RaisePropertyChanged();
            }
        }

        public bool Active
        {
            get
            {
                return mActive;
            }

            set
            {
                mActive = value;
                RaisePropertyChanged();
            }
        }

        public bool InPlayground
        {
            get
            {
                return mInPlayground;
            }

            set
            {
                mInPlayground = value;
                RaisePropertyChanged();
            }
        }
        
        public ArenaAvatarViewModel Clone()
        {
            ArenaAvatarViewModel _clone = new ArenaAvatarViewModel
            {
                Character = this.Character,
                Left = this.Left,
                Top = this.Top,
                Active = this.Active,
                InPlayground = this.InPlayground
            };

            return _clone;
        }
    }
}
