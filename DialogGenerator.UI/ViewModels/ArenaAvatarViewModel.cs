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
        private const int mMaxIterationsCount = 3;
        private int mCurrentIteration = mMaxIterationsCount;
        private const int mStep = 1;        
        private int mDecision = 1;
        private int mSleepInterval = 50;

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
                mLeft = value >= 0 ? value : 0;
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
                mTop = value >= 0 ? value : 0;
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
                    if(mCurrentIteration == mMaxIterationsCount)
                    {
                        Random random = new Random();
                        mDecision = random.Next(1, 8);
                        mCurrentIteration = 1;
                    }
                    
                    switch(mDecision)
                    {
                        case 1: // up
                        case 5:
                            Top -= mStep;
                            break;
                        case 2: // right
                        case 6:
                            Left += mStep;
                            break;
                        case 3: // bottom
                        case 7:
                            Top += mStep;
                            break;
                        case 4: // left
                        case 8:
                            Left -= mStep;
                            break;
                    }

                    mCurrentIteration++;

                    Thread.Sleep(mSleepInterval);

                } while (!mCancellationTokenSource.IsCancellationRequested);
            });
        }

        public void StopAnimation()
        {
            mCancellationTokenSource.Cancel();
        }
    }
}
