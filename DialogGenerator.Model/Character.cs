using DialogGenerator.Core;
using DialogGenerator.Model.Attributes;
using DialogGenerator.Model.Enum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DialogGenerator.Model
{
    public class Character : INotifyPropertyChanged, IEquatable<Character>
    {
        #region - fields -

        private string mCharacterName;
        private ObservableCollection<PhraseEntry> mPhrases;
        private int mRadioNum =-1;
        private PhraseEntry mPhraseTotals;
        private string mCharacterImage = ApplicationData.Instance.DefaultImage;
        private string mAuthor = String.Empty;
        private bool mHasNoVoice = false;
        private string mVoice = string.Empty;
        private int mSpeechRate = -1;

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

        [JsonProperty("CharacterAge"),Required]
        public int CharacterAge { get; set; } = 10;

        [RegularExpression(@"^(?:M|F)$", ErrorMessage = "Alloed letters: 'M' (Male) and 'F' (Female).")]
        [JsonProperty("CharacterGender")]
        public string CharacterGender { get; set; } = "M";

        [RegularExpression(Constants.FILENAME_CHECK_REGEX)]
        [StringLength(50,MinimumLength =3)]
        [JsonProperty("CharacterName"),Required(ErrorMessage ="Character name is required.")]
        public string CharacterName
        {
            get { return mCharacterName; }
            set
            {
                mCharacterName = value;
                OnPropertyChanged("CharacterName");
            }
        }

        [ValidCharacterPrefix]
        [JsonProperty("CharacterPrefix"),Required(ErrorMessage ="Character initials is required.")]
        public string CharacterPrefix { get; set; } = "";

        [FileExtensions(Extensions ="jpg,jpe,jpeg,png,gif,jfif,tiff,apng,avif,webp",ErrorMessage ="Allowed image file extensions: jpg,jpe,jpeg,png,gif,jfif,tiff,apng,avif,webp.")]
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
        //[JsonProperty("RadioNum")]
        [JsonIgnore]
        public int RadioNum
        {
            get { return mRadioNum; }
            set
            {
                mRadioNum = value;
                OnPropertyChanged("RadioNum");                
            }
        }



        [JsonProperty("Phrases")]
        public ObservableCollection<PhraseEntry> Phrases
        {
            get
            {
                if (mPhrases == null)
                    mPhrases = new ObservableCollection<PhraseEntry>();

                return mPhrases;
            }

            set
            {
                mPhrases = value;
            }
        }

        [JsonProperty("Author", NullValueHandling = NullValueHandling.Ignore)]
        public string Author
        {
            get
            {
                return mAuthor;
            }

            set
            {
                mAuthor = value;
                OnPropertyChanged("Author");
            }
        }

        [JsonProperty("HasNoVoice", NullValueHandling = NullValueHandling.Ignore)]
        public bool HasNoVoice
        {
            get
            {
                return mHasNoVoice;
            }

            set
            {
                mHasNoVoice = value;
                OnPropertyChanged("HasNoVoice");
            }
        }

        [JsonProperty("Voice", NullValueHandling = NullValueHandling.Ignore)]
        public string Voice
        {
            get
            {
                return mVoice;
            }

            set
            {
                mVoice = value;
                OnPropertyChanged("Voice");
            }
        }

        [JsonProperty("SpeechRate", NullValueHandling = NullValueHandling.Ignore)]
        public int SpeechRate { 
            get
            {
                return mSpeechRate;
            }

            set
            {
                mSpeechRate = value;
                OnPropertyChanged("SpeechRate");
            }
        }

        // json ignore properties
        
        [JsonIgnore]
        public Queue<PhraseEntry> RecentPhrases = new Queue<PhraseEntry>();
        [JsonIgnore]
        public const int RecentPhrasesQueueSize = 8;
        [JsonIgnore]
        public string FileName { get; set; }
        [JsonIgnore]
        public int JsonArrayIndex { get; set; }

        [JsonIgnore]
        public bool Unassigned { get; set; }
        [JsonIgnore]
        public bool Editable { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public override string ToString()
        {
            return CharacterName;
        }

        public bool Equals(Character other)
        {
            return other.CharacterPrefix.Equals(this.CharacterPrefix);
        }

        public void ClearRecentPhrases()
        {
            int _queueLength = RecentPhrases.Count;
            RecentPhrases.Clear();
            for (int i = 0; i < _queueLength; i++)
            {
                RecentPhrases.Enqueue(new PhraseEntry
                {
                    PhraseWeights = new Dictionary<string, double>(),
                    PhraseRating = "10",
                    DialogStr = "...",
                    FileName = "empty"
                });
            }
        }

        public void Merge(Character other)
        {
            if (!this.Equals(other))
                return;

            this.Phrases.Clear();
            foreach(var phrase in other.Phrases)
            {
                this.Phrases.Add(phrase);
            }
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }
    }
}
