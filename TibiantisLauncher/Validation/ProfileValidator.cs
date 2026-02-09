using System.Collections.Generic;
using RelicHelper.Profiles;

namespace RelicHelper.Validation
{
    internal static class ProfileValidator
    {
        public static void ValidateProfileName(string? profileName, IEnumerable<string>? possibleDuplicates = null)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                throw new ValidationException($"Profile name is empty.");

            if (profileName.Length > Profile.ProfileNameMaxLength)
                throw new ValidationException($"Profile name must not be longer than {Profile.ProfileNameMaxLength} characters.");

            if (possibleDuplicates == null)
                return;

            foreach (var possibleDuplicate in possibleDuplicates)
            {
                if (profileName == possibleDuplicate)
                    throw new ValidationException($"A profile named \"{profileName}\" already exists.");
            }
        }
    }
}
