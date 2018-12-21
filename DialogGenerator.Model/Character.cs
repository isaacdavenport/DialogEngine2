﻿using DialogGenerator.Core;
using DialogGenerator.Model.Attributes;
using DialogGenerator.Model.Enum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DialogGenerator.Model
{
    public class Character : INotifyPropertyChanged, IEquatable<Character>
    {
        #region - fields -

        private string mCharacterName;
        private CharacterState mState;
        private List<PhraseEntry> mPhrases;
        private int mRadioNum =-1;
        private PhraseEntry mPhraseTotals;
        private string mCharacterImage = ApplicationData.Instance.DefaultImage;

        #endregion

        #region - properties -

        [JsonIgnore]
        public PhraseEntry PhraseTotals
        {
            get
            {
                if (mPhraseTotals == null)
                    mPhraseTotals = new PhraseEntry();

                return mPhraseTotals;
            }

            set
            {
                mPhraseTotals = value;
            }
        }

        [Range(1,100)]
        [JsonProperty("CharacterAge"),Required]
        public int CharacterAge { get; set; } = 10;

        [RegularExpression(@"^(?:M|F)$",ErrorMessage = "Alloed letters: 'M' (Male) and 'F' (Female).")]
        [JsonProperty("CharacterGender")]
        public string CharacterGender { get; set; } = "M";


        [StringLength(30,MinimumLength =3)]
        [JsonProperty("CharacterName"),Required]
        public string CharacterName
        {
            get { return mCharacterName; }
            set
            {
                mCharacterName = value;
                OnPropertyChanged("CharacterName");
            }
        }

        [StringLength(3,MinimumLength =2,ErrorMessage ="Field requires 3 characters.")]
        [RegularExpression("^[a-zA-Z0-9]+$",ErrorMessage ="Allowed only letters and numbers.")]
        [JsonProperty("CharacterPrefix")]
        public string CharacterPrefix { get; set; } = "";

        [FileExtensions(Extensions ="jpg,jpe,jpeg,png",ErrorMessage ="Allowed extensions: jpg,jpe,jpeg,png.")]
        [JsonProperty("CharacterImage")]
        public string CharacterImage
        {
            get { return mCharacterImage; }
            set
            {
                mCharacterImage = value;
                OnPropertyChanged("CharacterImage");
            }
        }

        /// <summary>
        /// Radio number assigned to character
        /// Default value is unassigned ( -1 )
        /// </summary>
        [RadioNumRange]
        [JsonProperty("RadioNum")]
        public int RadioNum
        {
            get { return mRadioNum; }
            set
            {
                mRadioNum = value;
                OnPropertyChanged("RadioNum");                
            }
        }

        /// <summary>
        /// Represents state of character
        /// Default state is Available
        /// States are [Avaialble,On,Off]
        /// Available - character can be random selected
        /// On - character is forced in selection
        /// Off - character can't be selected
        /// </summary>
        [JsonProperty("State")]
        public CharacterState State
        {
            get { return mState; }
            set
            {
                mState = value;
                OnPropertyChanged("State");
            }
        }

        [JsonProperty("Phrases")]
        public List<PhraseEntry> Phrases
        {
            get
            {
                if (mPhrases == null)
                    mPhrases = new List<PhraseEntry>();

                return mPhrases;
            }

            set
            {
                mPhrases = value;
            }
        }

        // json ignore properties

        [JsonIgnore]
        public Queue<PhraseEntry> RecentPhrases = new Queue<PhraseEntry>();
        [JsonIgnore]
        public const int RecentPhrasesQueueSize = 8;

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public int JsonArrayIndex { get; set; }

        #endregion

        public override string ToString()
        {
            return CharacterName;
        }

        public bool Equals(Character other)
        {
            return other.CharacterPrefix.Equals(this.CharacterPrefix);
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }
    }
}
