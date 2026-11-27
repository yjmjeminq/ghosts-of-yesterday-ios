using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ghosts
{
    [Serializable] public sealed class Profile
    {
        public const int SlotCount = 6;
        public int version;
        public string[] slots = new string[SlotCount];
        public string current, quick;
        public bool finished, reducedMotion;
        public float music = .55f, effects = .6f, speed = 42, autoDelay = 1.6f;
        public List<string> seen = new(), episodes = new(), gallery = new();
    }

    public sealed class SaveStore
    {
        private readonly string path;
        public string Warning { get; private set; }

        public SaveStore(string directory)
        {
            path = Path.Combine(directory, "profile.json");
        }

        public Profile Load()
        {
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var result = JsonUtility.FromJson<Profile>(File.ReadAllText(candidate));
                    if (result == null || result.version != 1 || result.seen == null ||
                        result.episodes == null || result.gallery == null)
                        throw new InvalidDataException("Invalid save.");
                    if (result.slots == null || result.slots.Length != Profile.SlotCount)
                    {
                        var slots = result.slots;
                        result.slots = new string[Profile.SlotCount];
                        if (slots != null) Array.Copy(slots, result.slots, Math.Min(slots.Length, result.slots.Length));
                    }
                    if (candidate != path)
                    {
                        File.Copy(candidate, path + ".recover", true);
                        if (File.Exists(path)) File.Replace(path + ".recover", path, null);
                        else File.Move(path + ".recover", path);
                        Warning = "백업에서 진행을 복구했습니다.";
                    }
                    return result;
                }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException ||
                                               error is UnauthorizedAccessException)
                {
                    Warning = "저장 파일을 읽을 수 없습니다.";
                    Debug.LogWarning(error.Message);
                }
            }
            return new Profile { version = 1 };
        }

        public bool Write(Profile profile)
        {
            var temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(file))
                {
                    writer.Write(JsonUtility.ToJson(profile));
                    writer.Flush();
                    file.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                Warning = null;
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Warning = "저장할 수 없습니다.";
                Debug.LogWarning(error.Message);
                return false;
            }
        }
    }
}
