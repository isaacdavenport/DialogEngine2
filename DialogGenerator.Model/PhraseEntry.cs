using System;
using System.Collections.Generic;

namespace DialogGenerator.Model
{
    public class PhraseEntry:IEquatable<PhraseEntry>
    {
        /// <summary>
        /// Represents content which charachter will say
        /// </summary>
        public string DialogStr { get; set; }

        /// <summary>
        /// When you make an audio recording of dialogue, you'll use this to name the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///  G: General Audiences - PG: Parental Guidance Suggested
        /// </summary>
        public string PhraseRating { get; set; }

        /// <summary>
        /// "Key" represents phrase type. Phrase type determines what situations your character will say the dialogue in
        /// 
        /// <example> 
        /// your character will use a “Greeting” when they want to say
        /// hello or introduce themselves.
        /// </example>
        /// 
        /// "Value" This determines how often the
        /// character will say this phrase, compared to other phrases of the same type. The bigger
        /// the number, the more often they will say this phrase.
        /// </summary>
        public Dictionary<string, double> PhraseWeights { get; set; }//to replace PhraseWeights, uses string tags.

        public bool Equals(PhraseEntry other)
        {
            return this.FileName.Equals(other.FileName);
        }
    }
}
