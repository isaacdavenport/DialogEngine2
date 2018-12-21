using DialogGenerator.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace DialogGenerator.Model.Attributes
{
    public class RadioNumRangeAttribute: ValidationAttribute
    {
        public RadioNumRangeAttribute()
        {
            ErrorMessage = $"Value for 'RadioNum' property must be withing range (-1,{ApplicationData.Instance.NumberOfRadios-1})";
        }
        public override bool IsValid(object value)
        {
            try
            {
                int _radioNum = int.Parse(value.ToString());

                return _radioNum >= -1 && _radioNum < ApplicationData.Instance.NumberOfRadios;
            }
            catch {
                return false;
            }
        }
    }
}
