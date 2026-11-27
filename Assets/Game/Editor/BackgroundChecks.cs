using System;
using System.IO;
using System.Linq;
using Ghosts;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class BackgroundChecks
{
    [MenuItem("Ghosts/Validate Story and Saves")]
    public static void Run()
    {
        BuildGame.Validate();
        CheckSaves();
        CheckNarrative();
    }

    public static void CheckSaves()
    {
        var scratch = Path.Combine(Path.GetTempPath(), "ghosts-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            var story = new Story(Resources.Load<TextAsset>("Story/book"));
            var first = story.Beats[0].Line.id;
            var second = story.Beats[1].Line.id;
            var file = Path.Combine(scratch, "profile.json");
            Directory.CreateDirectory(scratch);
            File.WriteAllText(file, "{\"version\":1,\"seen\":[],\"episodes\":[],\"gallery\":[]}");
            var saves = new SaveStore(scratch);
            var profile = saves.Load();
            Require(profile.current == null && profile.slots.Length == Profile.SlotCount,
                "Previous version profile was not given manual slots.");
            profile.current = first;
            profile.quick = first;
            profile.slots[0] = first;
            profile.slots[5] = story.Beats.Last().Line.id;
            profile.seen.Add(first);
            profile.episodes.Add(story.Beats[0].Episode.id);
            Require(saves.Write(profile), "Initial save failed.");
            profile.current = second;
            Require(saves.Write(profile), "Save replacement failed.");

            var persisted = new SaveStore(scratch).Load();
            Require(persisted.current == second && persisted.quick == first && persisted.slots[0] == first &&
                persisted.slots[5] == story.Beats.Last().Line.id && persisted.seen.SequenceEqual(new[] { first }),
                "Persisted profile differs from the saved state.");

            File.WriteAllText(file, "{}");
            var recovered = new SaveStore(scratch).Load();
            Require(recovered.current == first && recovered.quick == first && recovered.slots[0] == first,
                "Backup did not restore the previous save.");
            Require(new SaveStore(scratch).Load().current == first, "Recovery did not replace the invalid primary.");
            Debug.Log("GHOSTS_SAVE_OK legacy profile, six manual slots, quick save, corrupt primary recovery");
        }
        finally
        {
            foreach (var name in new[] { "profile.json", "profile.json.bak", "profile.json.tmp", "profile.json.recover" })
            {
                var file = Path.Combine(scratch, name);
                if (File.Exists(file)) File.Delete(file);
            }
            if (Directory.Exists(scratch)) Directory.Delete(scratch);
        }
    }

    static void CheckNarrative()
    {
        var story = new Story(Resources.Load<TextAsset>("Story/book"));
        var lookups = new[] { "ep002-distance", "ep003-class", "ep009-underpass", "ep015-name",
            "ep020-confess", "ep021-kiss", "ep033-intercept", "ep033-rescue", "ep047-find", "ep050-last" };
        var indices = lookups.Select(scene => story.Beats.FindIndex(beat => beat.Scene.id == scene)).ToArray();
        Require(indices.All(index => index >= 0) && indices.SequenceEqual(indices.OrderBy(index => index)),
            "Reveal scenes are incomplete or out of order.");
        Require(story.Beats.All(beat => story.Beats[story.LineIndex[beat.Line.id]].Line == beat.Line),
            "A story beat is not directly loadable.");
        Require(story.Beats.All(beat => beat.Line.aliases == null || beat.Line.aliases.All(
                alias => story.Beats[story.LineIndex[alias]].Line == beat.Line)),
            "A checkpoint from a cut line must resume on its next playable beat.");
        Require(story.Gallery.Count >= 10 &&
            story.Gallery.All(scene => story.Beats.Any(beat => beat.Scene == scene)),
            "Some gallery entries have no reachable unlock scene.");
        Require(story.Beats.Last().Scene.id == "ep050-last", "The ending does not lead out of episode 50.");
        Debug.Log("GHOSTS_NARRATIVE_OK 50 episode order, reveal scene anchors, all beat load IDs, gallery reachability");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new BuildFailedException(message);
    }
}
