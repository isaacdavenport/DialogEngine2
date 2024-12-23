﻿using DialogGenerator.Model;
using Prism.Mvvm;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.UI.ViewModels
{
    public class ArenaAvatarViewModel : BindableBase,IEquatable<ArenaAvatarViewModel>
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
        private string mCharacterName;
        private bool mAboutToRemove = false;

        public Guid Id { get; set; } = Guid.NewGuid();
        public double Width { get; set; }
        public double Height { get; set; }
        public bool AboutToRemove
        {
            get
            {
                return mAboutToRemove;
            }
            set
            {
                mAboutToRemove = value;
                RaisePropertyChanged();
            }
        }

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
                CharacterName = mCharacter.CharacterName;
            }
        }

        public string CharacterName
        {
            get
            {
                return mCharacterName;
            }

            set
            {
                if(value.Length > 30)
                {
                    mCharacterName = value.Substring(0, 30);
                } else
                {
                    mCharacterName = value;
                }
                
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

        public Random Random { get;set; }
        
        public ArenaAvatarViewModel Clone()
        {
            ArenaAvatarViewModel _clone = new ArenaAvatarViewModel
            {
                Character = this.Character,
                Left = this.Left,
                Top = this.Top,
                Active = this.Active,
                InPlayground = this.InPlayground,
                Random = this.Random

            };

            return _clone;
        }

        public async Task StartAnimation()
        {
            mCancellationTokenSource = new CancellationTokenSource();            
            mDecision = Random.Next() % 4;            

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

        public bool Equals(ArenaAvatarViewModel other)
        {
            if (Id.Equals(other.Id))
                return true;

            return false;
        }
    }
}
