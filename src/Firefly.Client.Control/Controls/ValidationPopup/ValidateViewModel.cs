// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace Firefly.Client.Control
{
    public interface IValidateDate
    {
        bool HasErrors();
        IEnumerable GetErrors([CallerMemberName] string propertyName = null);
        string Validate(object sender, object value, [CallerMemberName] string propertyName = null, string notifyPropertyName = null);
    }

    public class ValidateViewModel : IValidateDate
    {
        private IDictionary<string, List<string>> errorsDic = new Dictionary<string, List<string>>();

        public bool HasErrors()
        {
            return errorsDic.Any();
        }

        public IEnumerable GetErrors([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                return null;
            }

            return errorsDic.ContainsKey(propertyName) ? errorsDic[propertyName] : null;
        }

        public string Validate(object sender, object obj, [CallerMemberName] string propertyName = null, string notifyPropertyName = null)
        {
            if (errorsDic.ContainsKey(propertyName))
            {
                errorsDic.Remove(propertyName);
            }

            if (!string.IsNullOrWhiteSpace(notifyPropertyName) && errorsDic.ContainsKey(notifyPropertyName))
            {
                errorsDic.Remove(notifyPropertyName);
            }

            ValidationContext context = new ValidationContext(sender) {MemberName = propertyName};
            List<ValidationResult> results = new List<ValidationResult>();

            string notifyProperty = string.IsNullOrWhiteSpace(notifyPropertyName) ? propertyName : notifyPropertyName;
            if (!Validator.TryValidateProperty(obj, context, results))
            {
                errorsDic[notifyProperty] = results.Select(x => x.ErrorMessage).ToList();
            }

            return notifyProperty;
        }
    }
}