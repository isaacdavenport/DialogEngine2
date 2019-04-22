using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DialogGenerator.Core;

namespace DialogGenerator.Model.Attributes
{
    public class ValidCharacterPrefixAttribute:ValidationAttribute
    {
        private const string mcPattern = Constants.FILENAME_CHECK_REGEX;
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
