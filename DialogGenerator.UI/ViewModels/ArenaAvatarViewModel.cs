using DialogGenerator.Model;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private CancellationTokenSource mCancellationTokenSource;

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

        public async Task StartAnimation()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() =>
            {
                do
                {
                    Random random = new Random();
                    int _decision = random.Next(1, 8);
                    switch(_decision)
                    {
                        case 1: // up
                        case 5:
                            Top -= 2;
                            break;
                        case 2: // right
                        case 6:
                            Left += 2;
                            break;
                        case 3: // bottom
                        case 7:
                            Top += 2;
                            break;
                        case 4: // left
                        case 8:
                            Left -= 2;
                            break;
                    }

                    Thread.Sleep(500);

                } while (!mCancellationTokenSource.IsCancellationRequested);
            });
        }

        public void StopAnimation()
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
