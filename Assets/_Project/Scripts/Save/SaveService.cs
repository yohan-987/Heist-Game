using System;
using System.IO;
using Game.Economy;
using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// Reads and writes the player profile as JSON. Writes are atomic (temp file, then swap) and the
    /// previous save is kept as a backup, so a crash mid-write can't destroy progress.
    /// The directory is injectable so tests don't touch the real save.
    /// </summary>
    public sealed class SaveService
    {
        private const string FileName = "profile.json";
        private const string BackupSuffix = ".bak";
        private const string TempSuffix = ".tmp";

        private readonly string _directory;

        public string ProfilePath => Path.Combine(_directory, FileName);
        private string BackupPath => ProfilePath + BackupSuffix;
        private string TempPath => ProfilePath + TempSuffix;

        public SaveService(string directory = null)
        {
            _directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        public bool HasSave => File.Exists(ProfilePath) || File.Exists(BackupPath);

        public void Save(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            Directory.CreateDirectory(_directory);
            profile.Version = PlayerProfile.CurrentVersion;
            string json = JsonUtility.ToJson(profile, prettyPrint: true);

            File.WriteAllText(TempPath, json);

            if (File.Exists(ProfilePath))
            {
                // Replace keeps the old file as the backup in one step.
                File.Replace(TempPath, ProfilePath, BackupPath);
            }
            else
            {
                File.Move(TempPath, ProfilePath);
            }
        }

        /// <summary>Loads the profile, falling back to the backup, then to a fresh profile.</summary>
        public PlayerProfile Load()
        {
            if (TryRead(ProfilePath, out var profile)) return profile;
            if (TryRead(BackupPath, out profile)) return profile;
            return new PlayerProfile();
        }

        public void Delete()
        {
            foreach (var path in new[] { ProfilePath, BackupPath, TempPath })
                if (File.Exists(path)) File.Delete(path);
        }

        private static bool TryRead(string path, out PlayerProfile profile)
        {
            profile = null;
            try
            {
                if (!File.Exists(path)) return false;
                var loaded = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
                if (loaded == null) return false;
                profile = Migrate(loaded);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not read save '{path}': {e.Message}");
                return false;
            }
        }

        /// <summary>Upgrade older save versions here as the format changes.</summary>
        private static PlayerProfile Migrate(PlayerProfile profile)
        {
            if (profile.Upgrades == null) profile.Upgrades = new System.Collections.Generic.List<UpgradeLevel>();
            // if (profile.Version < 2) { ... }
            profile.Version = PlayerProfile.CurrentVersion;
            return profile;
        }
    }
}
