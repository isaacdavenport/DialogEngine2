using DialogGenerator.Model;
using Prism.Commands;
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
        private const int mStep = 3;        
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
            Random random = new Random();
            mDecision = random.Next(1, 10000) % 4;            

            await Task.Run(() =>
            {
                do
                {
                    if(mDecision == 4)
                    {
                        mDecision = 0;
                    }

                    switch(mDecision)
                    {
                        case 0: // up
                            for (int i = 0; i < mStep; i++)
                            {
                                Top--;
                                Thread.Sleep(50);
                            }
                            break;
                        case 1: // right
                            for (int i = 0; i < mStep; i++)
                            {
                                Left++;
                                Thread.Sleep(50);
                            }
                            break;
                        case 2: // bottom
                            for (int i = 0; i < mStep; i++)
                            {
                                Top++;
                                Thread.Sleep(50);
                            }
                            break;
                        case 3: // left
                            for(int i = 0; i < mStep; i++)
                            {
                                Left--;
                                Thread.Sleep(50);
                            }
                            
                            break;
                    }

                    mDecision++;

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
