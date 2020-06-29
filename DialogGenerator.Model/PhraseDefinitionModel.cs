using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.Model
{
    public class PhraseDefinitionModel : IEquatable<PhraseDefinitionModel>
    {
        public string Text { get; set; } = string.Empty;
        public PhraseEntry PhraseEntry { get; set; }
        public string Description { get; set; }
        public Character Character { get; set; }
        public int SlotNumber { get; set; } = 0;

        public bool Equals(PhraseDefinitionModel other)
        {
            if(this.Text != other.Text)
            {
                return false;
            }

            if (this.PhraseEntry != other.PhraseEntry)
            {
                return false;
            }

            if (!this.Description.Equals(other.Description))
            {
                return false;
            }

            if(this.Character != other.Character)
            {
                return false;
            }

            if(this.SlotNumber != other.SlotNumber)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Text;
        }
    }
}
