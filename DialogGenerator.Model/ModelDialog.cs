using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DialogGenerator.Model
{
    /// <summary>
    /// ModelDialog is a sequence of phrase  that represent an exchange between characters 
    /// the model dialog will be filled with randomly selected character phrases of the appropriate phrase type
    /// </summary>
    public class ModelDialog : IEquatable<ModelDialog>
    {
        private string mName;

        [JsonProperty("AddedOnDateTime")]
        public DateTime AddedOnDateTime = new DateTime(2016, 1, 2, 3, 4, 5);

        [JsonProperty("Adventure")]
        public string Adventure = "";

        [JsonProperty("DialogName")]
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(mName))
                    return mName;
                else
                {
                    mName = string.Join(",", PhraseTypeSequence.ToArray());
                    return mName;
                }
            }
            set
            {
                mName = value;
            }
        }

        [JsonProperty("PhraseTypeSequence")]
        public List<string> PhraseTypeSequence = new List<string>();

        [JsonProperty("Popularity")]
        public double Popularity = 1.0;

        [JsonProperty("Provides")]
        public List<string> Provides = new List<string>();

        [JsonProperty("Requires")]
        public List<string> Requires = new List<string>();

        public bool AreDialogsRequirementsMet()
        {
            return true;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(ModelDialog other)
        {
            if (!AddedOnDateTime.Equals(other.AddedOnDateTime))
                return false;

            if (!Adventure.Equals(other.Adventure))
                return false;

            //if (!Name.Equals(other.Name))
            //    return false;

            if(PhraseTypeSequence.Count != other.PhraseTypeSequence.Count)
            {
                return false;
            } else
            {
                for(int i = 0; i < PhraseTypeSequence.Count; i++)
                {
                    if (!PhraseTypeSequence[i].Equals(other.PhraseTypeSequence[i]))
                        return false;
                }
            }

            if (Popularity != other.Popularity)
                return false;

            if (Provides.Count != other.Provides.Count)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < Provides.Count; i++)
                {
                    if (!Provides[i].Equals(other.Provides[i]))
                        return false;
                }
            }

            if (Requires.Count != other.Requires.Count)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < Requires.Count; i++)
                {
                    if (!Requires[i].Equals(other.Requires[i]))
                        return false;
                }
            }

            return true;
        }
    }
}
