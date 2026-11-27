using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ghosts
{
    [Serializable] public sealed class StoryLine
    {
        public string id, speaker, text, sfx;
        public string[] aliases;
    }

    [Serializable] public sealed class CharacterLayer
    {
        public string art;
        public float x, y, height;
    }
 
    [Serializable] public sealed class StoryScene
    {
        public string id, place, time, pov, art, music, gallery;
        public StoryLine[] lines;
        public CharacterLayer[] layers;
    }

    [Serializable] public sealed class Episode
    {
        public string id, title;
        public StoryScene[] scenes;
    }

    [Serializable] public sealed class StoryBook
    {
        public Episode[] episodes;
    }

    public sealed class Story
    {
        public readonly struct Beat
        {
            public readonly Episode Episode;
            public readonly StoryScene Scene;
            public readonly StoryLine Line;
            public Beat(Episode episode, StoryScene scene, StoryLine line)
            {
                Episode = episode;
                Scene = scene;
                Line = line;
            }
        }

        public readonly StoryBook Book;
        public readonly List<Beat> Beats = new();
        public readonly Dictionary<string, int> LineIndex = new();
        public readonly Dictionary<string, int> EpisodeStart = new();
        public readonly List<StoryScene> Gallery = new();

        public Story(TextAsset source)
        {
            Book = JsonUtility.FromJson<StoryBook>(source.text);
            foreach (var episode in Book.episodes)
            {
                EpisodeStart.Add(episode.id, Beats.Count);
                foreach (var scene in episode.scenes)
                {
                    if (!string.IsNullOrEmpty(scene.gallery)) Gallery.Add(scene);
                    foreach (var line in scene.lines)
                    {
                        LineIndex.Add(line.id, Beats.Count);
                        if (line.aliases != null)
                            foreach (var alias in line.aliases)
                                LineIndex.Add(alias, Beats.Count);
                        Beats.Add(new Beat(episode, scene, line));
                    }
                }
            }
            if (Beats.Count == 0)
                throw new InvalidOperationException("Story has no playable lines.");
        }
    }
}
