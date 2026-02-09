using System;
using System.Collections.Generic;

namespace RelicHelper.Profiles
{
    [Serializable]
    public class ProfilesData
    {
        public List<Profile> Profiles { get; set; } = new List<Profile>();
    }
}