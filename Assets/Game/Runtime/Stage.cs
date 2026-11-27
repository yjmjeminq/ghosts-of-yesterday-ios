using System.Collections.Generic;
using UnityEngine;

namespace Ghosts
{
    public sealed partial class Stage : MonoBehaviour
    {
        enum Page { Title, Story, End }
        enum Panel { None, Chapters, Gallery, Log, Settings, Save, Slots, Credits, NewGame }
        Story story;
        StoryAudio audioPlayer;
        SaveStore store;
        Profile profile;
        Page page;
        Panel panel;
        int cursor, shown;
        bool auto, skip, hudHidden, recall, fresh;
        float typed, lineClock, panelClock, noticeTime;
        string notice;
        StoryScene currentScene, lastScene, gallerySelection;
        int chapterPage, galleryPage, replacingSlot = -1;
        readonly Dictionary<string, Texture2D> textures = new();
        Vector2 scroll;
        readonly List<Story.Beat> history = new();
        readonly HashSet<string> seen = new();
        Font font;
        Texture2D art, lastArt;
        Story.Beat Beat => story.Beats[cursor];

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            story = new Story(Resources.Load<TextAsset>("Story/book"));
            font = Resources.Load<Font>("Type/NotoSansKR-Regular");
            store = new SaveStore(Application.persistentDataPath);
            profile = store.Load();
            seen.UnionWith(profile.seen);
            audioPlayer = gameObject.AddComponent<StoryAudio>();
            audioPlayer.SetVolume(profile.music, profile.effects);
            ToTitle();
            if (store.Warning != null) Notify(store.Warning);
        }

        void ToTitle()
        {
            panel = Panel.None;
            page = Page.Title;
            auto = skip = recall = hudHidden = false;
            currentScene = lastScene = null;
            art = GetTexture("Art/balcony");
            lastArt = null;
            audioPlayer.PlayTrack("city");
        }

        void FindAndStart(string id)
        {
            if (id != null && story.LineIndex.TryGetValue(id, out var index)) StartAt(index, false);
            else Notify("이어서 읽을 저장 지점이 없습니다.");
        }

        void StartAt(int index, bool asRecall)
        {
            page = Page.Story;
            recall = asRecall;
            panel = Panel.None;
            hudHidden = auto = skip = false;
            currentScene = null;
            history.Clear();
            SetLine(index);
        }

        void SetLine(int index)
        {
            cursor = index;
            var beat = Beat;
            fresh = !seen.Contains(beat.Line.id);
            typed = shown = 0;
            lineClock = 0;
            history.Add(beat);
            if (beat.Scene != currentScene)
            {
                lastScene = currentScene;
                currentScene = beat.Scene;
                lastArt = art;
                art = GetTexture("Art/" + beat.Scene.art);
                panelClock = 0;
                audioPlayer.PlayTrack(beat.Scene.music);
            }
            if (!skip) audioPlayer.PlayEffect(beat.Line.sfx);
            if (recall) return;
            profile.current = beat.Line.id;
            profile.finished = false;
            AddUnique(profile.episodes, beat.Episode.id);
            if (!string.IsNullOrEmpty(beat.Scene.gallery)) AddUnique(profile.gallery, beat.Scene.gallery);
            Persist();
        }

        static void AddUnique(List<string> list, string id)
        {
            if (!list.Contains(id)) list.Add(id);
        }

        void MarkRead()
        {
            if (recall || !seen.Add(Beat.Line.id)) return;
            profile.seen.Add(Beat.Line.id);
        }

        void Advance()
        {
            if (shown < Beat.Line.text.Length)
            {
                typed = shown = Beat.Line.text.Length;
                lineClock = 0;
                return;
            }
            MarkRead();
            var next = cursor + 1;
            if (recall && (next == story.Beats.Count || story.Beats[next].Episode != Beat.Episode))
            {
                ToTitle();
                Open(Panel.Chapters);
                return;
            }
            if (next < story.Beats.Count) SetLine(next);
            else
            {
                profile.finished = true;
                Persist();
                auto = skip = false;
                page = Page.End;
            }
        }

        void Persist()
        {
            if (!store.Write(profile)) Notify(store.Warning);
        }

        void Notify(string text)
        {
            notice = text;
            noticeTime = 3;
        }

        void Open(Panel name)
        {
            panel = name;
            scroll = Vector2.zero;
            gallerySelection = null;
            replacingSlot = -1;
        }

        Texture2D GetTexture(string key)
        {
            if (!textures.TryGetValue(key, out var texture))
            {
                texture = Resources.Load<Texture2D>(key);
                textures.Add(key, texture);
            }
            return texture;
        }

        void WriteSlot(int index)
        {
            if (page != Page.Story || recall) return;
            var previous = profile.slots[index];
            profile.slots[index] = Beat.Line.id;
            MarkReadIfVisible();
            if (store.Write(profile))
            {
                replacingSlot = -1;
                Notify($"{index + 1}번 슬롯에 저장했습니다.");
            }
            else
            {
                profile.slots[index] = previous;
                Notify(store.Warning);
            }
        }

        void ToggleSkip()
        {
            skip = !skip;
            auto = false;
            if (skip && fresh)
            {
                skip = false;
                Notify("처음 읽는 대사에서 스킵이 멈춥니다.");
            }
        }

        void QuickSave()
        {
            MarkReadIfVisible();
            profile.quick = Beat.Line.id;
            if (store.Write(profile)) Notify("빠른 저장을 마쳤습니다.");
            else Notify(store.Warning);
        }

        void MarkReadIfVisible()
        {
            if (shown >= Beat.Line.text.Length) MarkRead();
        }

        void ToggleHud()
        {
            hudHidden = !hudHidden;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            noticeTime -= dt;
            panelClock += dt;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (panel != Panel.None)
                {
                    if (panel == Panel.Settings) Persist();
                    Open(Panel.None);
                }
                else if (page == Page.Story) { hudHidden = false; Open(Panel.Save); }
                else if (page == Page.End) ToTitle();
            }
            if (page != Page.Story || panel != Panel.None || hudHidden) return;
            if (Input.GetKeyDown(KeyCode.F5) && !recall) QuickSave();
            if (Input.GetKeyDown(KeyCode.F9) && !recall) FindAndStart(profile.quick);
            if (Input.GetKeyDown(KeyCode.H)) { ToggleHud(); return; }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Advance();
                return;
            }
            int length = Beat.Line.text.Length;
            typed = Mathf.Min(length, typed + dt * profile.speed);
            shown = (int)typed;
            MarkReadIfVisible();
            if (skip && fresh) ToggleSkip();
            if (skip)
            {
                typed = shown = length;
                lineClock += dt;
                if (lineClock >= .085f) Advance();
            }
            else if (shown == length)
            {
                lineClock += dt;
                if (auto && lineClock >= profile.autoDelay + length * .018f) Advance();
            }
        }

        void OnApplicationPause(bool pause)
        {
            if (pause && profile != null) Persist();
        }

        void OnApplicationQuit()
        {
            if (profile != null) Persist();
        }
    }
}
