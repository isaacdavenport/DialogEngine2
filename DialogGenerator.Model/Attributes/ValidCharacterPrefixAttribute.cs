using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DialogGenerator.Model.Attributes
{
    public class ValidCharacterPrefixAttribute:ValidationAttribute
    {
        private const string mcPattern = @"^[a-zA-Z]+$";
        public ValidCharacterPrefixAttribute()
        {
            ErrorMessage = "Allowed only letters.";
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
