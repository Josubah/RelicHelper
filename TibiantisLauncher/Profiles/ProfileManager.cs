using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;
using RelicHelper.Clients;
using RelicHelper.Validation;

namespace RelicHelper.Profiles
{
    internal class ProfileManager
    {
        private static ProfileManager? _instance;
        public static ProfileManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProfileManager();

                return _instance;
            }
        }

        public const int MaxProfileId = 99;
        public const int MaxProfileCount = 20;
        
        private const string ProfilesFilePath = @"profiles\profiles.xml";
        public const string ProfilesDirectory = "profiles";

        public static string ProfilesFileFullPath { get => Path.Combine(GameClient.ClientDirectoryFullPath, ProfilesFilePath); }
        public static string ProfilesDirectoryFullPath { get => Path.Combine(GameClient.ClientDirectoryFullPath, ProfilesDirectory); }

        public ObservableCollection<Profile> Profiles = new ObservableCollection<Profile>();

        //private SortedDictionary<string, Profile> _profiles { get; set; } = new SortedDictionary<string, Profile>();


        //public ICollection<Profile> Profiles { get => _profiles.Values; }

        public void CreateProfilesDirectory()
        {
            var profilesDir = ProfilesDirectoryFullPath;
            if (!Directory.Exists(profilesDir))
                Directory.CreateDirectory(profilesDir);
        }

        public void LoadProfiles()
        {
            if (!File.Exists(ProfilesFileFullPath))
                SaveProfiles();

            var serializer = new XmlSerializer(typeof(ProfilesData));

            ProfilesData? dto;
            using (var fileStream = new FileStream(ProfilesFileFullPath, FileMode.Open, FileAccess.Read))
            {
                dto = (ProfilesData?)serializer.Deserialize(fileStream);
            }

            Profiles.Clear();

            if (dto == null)
                return;

            foreach (var profile in dto.Profiles)
            {
                Profiles.Add(profile);
            }
        }

        public void SaveProfiles()
        {
            var dto = new ProfilesData
            {
                Profiles = Profiles.ToList()
            };

            var serializer = new XmlSerializer(typeof(ProfilesData));

            using (var fileStream = new FileStream(ProfilesFileFullPath, FileMode.Create))
            {
                serializer.Serialize(fileStream, dto);
            }
        }

        public Profile AddProfile(string name)
        {
            ProfileValidator.ValidateProfileName(name);
            
            var profile = new Profile {
                Name = name,
                CfgId = GetNewCfgId()
            };

            Profiles.Add(profile);
            SaveProfiles();

            return profile;
        }

        public void RenameProfile(Profile profile, string newName)
        {
            var _profile = Profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (_profile is null)
                return;

            _profile.Name = newName;
            SaveProfiles();
        }

        public void RemoveProfile(Profile profile) {
            File.Delete(GetProfileCfgFullPath(profile));

            Profiles.Remove(profile);

            SaveProfiles();
        }

        private string GetProfileCfgFullPath(Profile profile) => Path.Combine(ProfilesDirectoryFullPath, profile.CfgFile);

        private byte GetNewCfgId()
        {
            for (byte i = 0; i <= MaxProfileId; i++)
            {
                bool exists = false;
                foreach (var profile in Profiles)
                {
                    if (profile.CfgId == i)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    return i;
            }

            throw new ApplicationException("Failed to get cfg id for new profile.");
        }
    }
}
