using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DialogGenerator.Model.Attributes
{
    public class ValidCharacterPrefixAttribute:ValidationAttribute
    {
        // TODO make this a global constant to use in character.cs line 52 as well
        private const string mcPattern = @"^[-a-zA-Z0-9_' ]+$";
        public ValidCharacterPrefixAttribute()
        {
            ErrorMessage = "Only letters and numbers in names please.";
        }
        public override bool IsValid(object value)
        {
            string initials = value.ToString();
            if (string.IsNullOrEmpty(initials))
                return false;

            string[] _initialsAndGuid = initials.Split('_');
            return Regex.IsMatch(_initialsAndGuid[0], mcPattern);
        }
    }
}
