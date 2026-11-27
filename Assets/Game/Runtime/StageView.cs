using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ghosts
{
    public sealed partial class Stage
    {
        static readonly Color Ink = new(.055f, .061f, .102f);
        static readonly Color Paper = new(.98f, .948f, .885f);
        static readonly Color Muted = new(.7f, .71f, .79f);
        static readonly Color Rose = new(1f, .60f, .66f);
        static readonly Color Mint = new(.53f, .89f, .85f);
        static readonly Color Night = new(.08f, .087f, .146f, .96f);
        readonly Dictionary<(int, Color, bool, TextAnchor, bool), GUIStyle> styles = new();
        const float W = 1440, H = 810;

        void OnGUI()
        {
            if (story == null || font == null) return;
            var safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) return;
            float scale = Mathf.Min(safe.width / W, safe.height / H);
            var origin = new Vector3(safe.x + (safe.width - W * scale) / 2,
                Screen.height - safe.yMax + (safe.height - H * scale) / 2);
            Fill(new Rect(0, 0, Screen.width, Screen.height), Ink);
            GUI.matrix = Matrix4x4.TRS(origin, Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.BeginGroup(new Rect(0, 0, W, H));
            DrawArt();
            GUI.enabled = panel == Panel.None;
            if (page == Page.Title) DrawTitle();
            else if (page == Page.End) DrawEnd();
            else DrawStory();
            GUI.enabled = true;
            if (panel != Panel.None) DrawPanel();
            if (noticeTime > 0) DrawNotice();
            GUI.EndGroup();
            GUI.matrix = Matrix4x4.identity;
        }

        void DrawArt()
        {
            if (art == null) return;
            float progress = profile.reducedMotion ? 1 : Mathf.Clamp01(panelClock / .34f);
            var area = new Rect(0, 0, W, H);
            if (lastArt != null && progress < 1) DrawComposition(area, lastArt, lastScene, 1);
            DrawComposition(area, art, currentScene, lastArt == null ? 1 : progress);
            Frame(new Rect(20, 20, 1400, 770), new Color(1, 1, 1, .24f), 1);
        }

        void DrawComposition(Rect area, Texture2D background, StoryScene scene, float opacity = 1)
        {
            var before = GUI.color;
            GUI.color = new Color(1, 1, 1, opacity);
            Texture(area, background);
            GUI.BeginGroup(area);
            if (scene?.layers != null)
            {
                bool nightTint = scene.art == "balcony" || scene.art == "roof";
                GUI.color = nightTint ? new Color(.78f, .80f, .90f, opacity) : new Color(1, 1, 1, opacity);
                foreach (var layer in scene.layers)
                {
                    var actor = GetTexture("Sprites/" + layer.art);
                    if (actor == null) continue;
                    float height = layer.height * area.height / H;
                    float width = height * actor.width / actor.height;
                    var rect = new Rect(layer.x * area.width / W - width / 2,
                        layer.y * area.height / H - height, width, height);
                    GUI.DrawTexture(rect, actor, ScaleMode.StretchToFill, true);
                }
            }
            GUI.EndGroup();
            GUI.color = before;
        }

        void DrawTitle()
        {
            Fill(new Rect(20, 20, 610, 770), new Color(.045f, .05f, .09f, .88f));
            Fill(new Rect(65, 126, 59, 4), Rose);
            Label(new Rect(65, 78, 486, 42), "SPIDER-GWEN", 25, Rose, true);
            Label(new Rect(62, 155, 550, 96), "GHOSTS OF", 74, Paper, true);
            Label(new Rect(60, 234, 556, 105), "YESTERDAY", 78, Paper, true);
            Label(new Rect(66, 350, 534, 45), "어제의 유령들", 30, Muted);
            Fill(new Rect(65, 416, 491, 1), new Color(1, 1, 1, .26f));
            bool resume = profile.current != null && story.LineIndex.ContainsKey(profile.current);
            if (Button(new Rect(65, 454, 491, 62), "이어 읽기", resume, true)) FindAndStart(profile.current);
            if (Button(new Rect(65, 529, 491, 56), "새 이야기"))
            {
                if (resume) Open(Panel.NewGame);
                else StartAt(0, false);
            }
            if (Button(new Rect(65, 610, 155, 53), "챕터")) Open(Panel.Chapters);
            if (Button(new Rect(233, 610, 155, 53), "CG 갤러리")) Open(Panel.Gallery);
            if (Button(new Rect(401, 610, 155, 53), "설정")) Open(Panel.Settings);
            if (Button(new Rect(65, 691, 155, 45), "제작 정보")) Open(Panel.Credits);
            if (Button(new Rect(233, 691, 155, 45), "불러오기")) Open(Panel.Slots);
            if (Application.platform == RuntimePlatform.WindowsPlayer &&
                Button(new Rect(411, 691, 145, 45), "종료")) Application.Quit();
        }

        void DrawStory()
        {
            if (hudHidden)
            {
                if (Button(new Rect(1218, 730, 167, 48), "대사창 표시")) ToggleHud();
                return;
            }
            var beat = Beat;
            Fill(new Rect(40, 39, 535, 57), Night);
            Fill(new Rect(40, 39, 5, 57), Mint);
            Label(new Rect(63, 41, 490, 52), beat.Episode.id.Substring(2) + "  /  " + beat.Episode.title, 24, Paper, true);
            Fill(new Rect(40, 99, 535, 35), new Color(.055f, .061f, .102f, .88f));
            Label(new Rect(63, 99, 490, 35), beat.Scene.place + "  ·  " + beat.Scene.time, 17, Muted);
            if (Button(new Rect(1248, 44, 135, 48), "메뉴")) Open(Panel.Save);

            Fill(new Rect(48, 561, 1344, 214), Night);
            Frame(new Rect(48, 561, 1344, 214), new Color(.78f, .79f, .87f, .65f), 2);
            if (beat.Line.speaker.Length > 0)
            {
                var color = beat.Line.speaker == "그웬" || beat.Line.speaker == "스파이더우먼" ? Rose : Mint;
                Fill(new Rect(72, 540, 220, 42), color);
                Label(new Rect(88, 540, 185, 42), beat.Line.speaker, 22, Ink, true);
            }
            var text = beat.Line.text.Substring(0, shown);
            Label(new Rect(79, 593, 1280, 95), text, 30, Paper, false, TextAnchor.UpperLeft, true);
            Fill(new Rect(78, 697, 1284, 1), new Color(1, 1, 1, .26f));
            if (Button(new Rect(79, 711, 108, 47), "기록")) Open(Panel.Log);
            if (Button(new Rect(196, 711, 130, 47), auto ? "자동 켜짐" : "자동", true, auto)) { auto = !auto; skip = false; }
            var skipButton = new Rect(335, 711, 130, 47);
            if (Button(skipButton, skip ? "스킵 켜짐" : "스킵", !recall, skip)) ToggleSkip();
            if (Button(new Rect(474, 711, 145, 47), "대사창 숨김")) ToggleHud();
            if (recall) Label(new Rect(637, 711, 120, 47), "다시 읽기", 16, Mint);
            if (Button(new Rect(1160, 711, 202, 47), shown >= beat.Line.text.Length ? "다음    ▸" : "대사 표시", true, true)) Advance();

            // Give the visible controls first chance to consume the press and release.
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && page == Page.Story && panel == Panel.None && !hudHidden &&
                (!recall || !skipButton.Contains(Event.current.mousePosition));
            if (GUI.Button(new Rect(0, 0, W, H), GUIContent.none, GUIStyle.none)) Advance();
            GUI.enabled = wasEnabled;
        }

        void DrawEnd()
        {
            Fill(new Rect(250, 218, 940, 360), Night);
            Frame(new Rect(250, 218, 940, 360), Muted, 2);
            Label(new Rect(310, 275, 820, 64), "어제의 유령들 · 끝", 42, Paper, true, TextAnchor.MiddleCenter);
            Label(new Rect(312, 361, 816, 58), "챕터와 CG는 타이틀에서 다시 볼 수 있습니다.", 24, Muted, false, TextAnchor.MiddleCenter);
            if (Button(new Rect(563, 465, 314, 62), "타이틀로", true, true)) ToTitle();
        }

        void DrawPanel()
        {
            Fill(new Rect(0, 0, W, H), new Color(.035f, .04f, .07f, .8f));
            var frame = new Rect(215, 106, 1010, 598);
            Fill(frame, Night);
            Frame(frame, Muted, 2);
            Fill(new Rect(215, 106, 1010, 4), Rose);
            string heading = panel switch {
                Panel.Chapters => "챕터", Panel.Gallery => "CG 갤러리", Panel.Log => "대화 기록",
                Panel.Settings => "설정", Panel.Save => "메뉴", Panel.Slots => "저장과 불러오기", Panel.NewGame => "새 이야기", _ => "제작 정보"
            };
            Label(new Rect(254, 126, 740, 72), heading, 39, Paper, true);
            if (Button(new Rect(1050, 143, 132, 47), "닫기"))
            {
                if (panel == Panel.Settings) Persist();
                Open(Panel.None);
                return;
            }
            Fill(new Rect(255, 207, 925, 1), Muted);
            switch (panel)
            {
                case Panel.Chapters:
                    DrawChapters();
                    break;
                case Panel.Gallery:
                    DrawGallery();
                    break;
                case Panel.Log:
                    DrawLog();
                    break;
                case Panel.Slots:
                    DrawSlots();
                    break;
                case Panel.Save:
                    if (Button(new Rect(255, 235, 447, 63), "빠른 저장    F5", !recall)) QuickSave();
                    if (Button(new Rect(733, 235, 447, 63), "빠른 불러오기    F9", !recall && profile.quick != null))
                        FindAndStart(profile.quick);
                    if (Button(new Rect(255, 317, 447, 63), "설정")) Open(Panel.Settings);
                    if (Button(new Rect(733, 317, 447, 63), "타이틀로")) ToTitle();
                    if (Button(new Rect(255, 399, 447, 63), "저장과 불러오기", !recall)) Open(Panel.Slots);
                    if (Button(new Rect(733, 399, 447, 63), "CG 갤러리")) Open(Panel.Gallery);
                    Label(new Rect(256, 485, 915, 56),
                        recall ? "다시 읽기에서는 본편 저장을 사용하지 않습니다." : "본편 진행은 대사마다 자동 저장됩니다.", 24, Muted);
                    break;
                case Panel.Settings:
                    profile.music = Slider(243, "음악", profile.music, 0, 1, Mathf.RoundToInt(profile.music * 100) + "%");
                    profile.effects = Slider(313, "효과음", profile.effects, 0, 1, Mathf.RoundToInt(profile.effects * 100) + "%");
                    profile.speed = Slider(383, "글자 속도", profile.speed, 15, 110, Mathf.RoundToInt(profile.speed).ToString());
                    profile.autoDelay = Slider(453, "자동 진행 간격", profile.autoDelay, .4f, 4, profile.autoDelay.ToString("0.0") + "초");
                    if (Button(new Rect(255, 554, 445, 62), profile.reducedMotion ? "화면 전환 효과 줄이기  켜짐" : "화면 전환 효과 줄이기  꺼짐", true, profile.reducedMotion))
                        profile.reducedMotion = !profile.reducedMotion;
                    audioPlayer.SetVolume(profile.music, profile.effects);
                    break;
                case Panel.NewGame:
                    Label(new Rect(255, 237, 907, 75), "1화부터 다시 읽습니다. 해금 기록과 빠른 저장은 유지됩니다.", 25, Paper);
                    if (Button(new Rect(255, 346, 380, 65), "1화부터 시작", true, true)) StartAt(0, false);
                    break;
                default:
                    Label(new Rect(255, 238, 907, 280), "Spider-Gwen: Ghosts of Yesterday\n\n개인용 팬 비주얼 노벨\n웹툰 CG · ImageGen\n한국어 서체 · Noto Sans KR (SIL OFL 1.1)\n음악 및 효과음 · 게임용 오리지널 합성", 25, Paper, false, TextAnchor.UpperLeft, true);
                    break;
            }
        }

        void DrawChapters()
        {
            const int perPage = 6;
            var episodes = story.Book.episodes;
            for (int i = chapterPage * perPage; i < Mathf.Min(episodes.Length, (chapterPage + 1) * perPage); i++)
            {
                var episode = episodes[i];
                bool unlocked = profile.episodes.Contains(episode.id);
                if (Button(new Rect(255, 230 + i % perPage * 67, 925, 54),
                    $"{i + 1:D2}  /  " + (unlocked ? episode.title : "잠긴 챕터"), unlocked))
                {
                    StartAt(story.EpisodeStart[episode.id], true);
                    return;
                }
            }
            DrawPager(ref chapterPage, episodes.Length, perPage);
        }

        void DrawGallery()
        {
            if (gallerySelection != null)
            {
                DrawComposition(new Rect(353, 221, 734, 413), GetTexture("Art/" + gallerySelection.art), gallerySelection);
                if (Button(new Rect(254, 638, 170, 45), "목록으로")) gallerySelection = null;
                return;
            }
            const int perPage = 6;
            var entries = story.Gallery;
            for (int i = galleryPage * perPage; i < Mathf.Min(entries.Count, (galleryPage + 1) * perPage); i++)
            {
                int index = i % perPage;
                var scene = entries[i];
                var tile = new Rect(255 + index % 3 * 314, 228 + index / 3 * 199, 286, 185);
                var unlocked = profile.gallery.Contains(scene.gallery);
                Fill(tile, Ink);
                var imageArea = new Rect(tile.x, tile.y, tile.width, tile.width * H / W);
                if (unlocked) DrawComposition(imageArea, GetTexture("Art/" + scene.art), scene);
                else Label(imageArea, "미해금", 26, Muted, false, TextAnchor.MiddleCenter);
                if (Button(new Rect(tile.x, tile.y + 145, tile.width, 41), unlocked ? scene.gallery : "잠긴 CG", unlocked))
                    gallerySelection = scene;
            }
            DrawPager(ref galleryPage, entries.Count, perPage);
        }

        void DrawPager(ref int current, int count, int perPage)
        {
            var pages = Mathf.Max(1, (count + perPage - 1) / perPage);
            if (Button(new Rect(255, 640, 170, 43), "이전 페이지", current > 0)) current--;
            if (Button(new Rect(1010, 640, 170, 43), "다음 페이지", current + 1 < pages)) current++;
            Label(new Rect(620, 640, 200, 43), $"{current + 1} / {pages}", 20, Muted, false, TextAnchor.MiddleCenter);
        }

        void DrawSlots()
        {
            bool canSave = page == Page.Story && !recall;
            for (int i = 0; i < profile.slots.Length; i++)
            {
                var slot = profile.slots[i];
                bool occupied = !string.IsNullOrEmpty(slot) && story.LineIndex.TryGetValue(slot, out _);
                float y = 230 + i * 68;
                string label = occupied
                    ? $"{i + 1:D2}   ·   {story.Beats[story.LineIndex[slot]].Episode.title}"
                    : $"{i + 1:D2}   ·   빈 슬롯";
                Label(new Rect(258, y, 470, 51), label, 23, occupied ? Paper : Muted);
                if (Button(new Rect(754, y, 178, 51), replacingSlot == i ? "덮어쓰기 확인" : "저장", canSave, replacingSlot == i))
                {
                    if (occupied && replacingSlot != i) replacingSlot = i;
                    else WriteSlot(i);
                }
                if (Button(new Rect(951, y, 224, 51), "불러오기", occupied && !recall))
                {
                    FindAndStart(slot);
                    return;
                }
            }
            if (replacingSlot >= 0 && Button(new Rect(754, 644, 178, 40), "덮어쓰기 취소")) replacingSlot = -1;
            if (page == Page.Title && Button(new Rect(951, 644, 224, 40), "빠른 불러오기",
                    !string.IsNullOrEmpty(profile.quick))) FindAndStart(profile.quick);
        }

        void DrawLog()
        {
            var content = new Rect(0, 0, 880, Mathf.Max(432, history.Count * 90));
            scroll = GUI.BeginScrollView(new Rect(255, 230, 927, 431), scroll, content);
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (i == history.Count - 1 && shown < Beat.Line.text.Length) continue;
                var line = history[i].Line;
                float y = (history.Count - 1 - i) * 90;
                Label(new Rect(0, y, 166, 37), line.speaker.Length == 0 ? "내레이션" : line.speaker, 20, Mint, true);
                Label(new Rect(171, y + 4, 699, 80), line.text, 22, Paper, false, TextAnchor.UpperLeft, true);
            }
            GUI.EndScrollView();
        }

        void DrawNotice()
        {
            Fill(new Rect(351, 23, 738, 54), Ink);
            Frame(new Rect(351, 23, 738, 54), Mint, 1);
            Label(new Rect(370, 23, 700, 54), notice, 22, Paper, false, TextAnchor.MiddleCenter);
        }

        float Slider(float y, string label, float value, float min, float max, string display)
        {
            Label(new Rect(255, y, 245, 51), label, 25, Paper);
            Label(new Rect(1040, y, 138, 51), display, 22, Mint, true, TextAnchor.MiddleRight);
            return GUI.HorizontalSlider(new Rect(526, y + 18, 485, 32), value, min, max);
        }

        GUIStyle Style(int size, Color color, bool bold, TextAnchor align, bool wrap)
        {
            var key = (size, color, bold, align, wrap);
            if (styles.TryGetValue(key, out var style)) return style;
            style = new GUIStyle
            {
                font = font, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = align, wordWrap = wrap, clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            styles.Add(key, style);
            return style;
        }

        void Label(Rect rect, string text, int size, Color color, bool bold = false,
            TextAnchor align = TextAnchor.MiddleLeft, bool wrap = false) =>
            GUI.Label(rect, text, Style(size, color, bold, align, wrap));

        bool Button(Rect rect, string text, bool enabled = true, bool primary = false)
        {
            var hovered = enabled && rect.Contains(Event.current.mousePosition);
            var fill = !enabled ? Night : primary ? (hovered ? Paper : Rose) :
                hovered ? new Color(.24f, .24f, .35f) : Ink;
            Fill(rect, fill);
            Frame(rect, primary ? fill : new Color(.78f, .79f, .87f, enabled ? .6f : .28f), 1);
            var before = GUI.enabled;
            GUI.enabled = before && enabled;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = before;
            Label(rect, text, 22, !enabled ? Muted : primary ? Ink : Paper, true, TextAnchor.MiddleCenter);
            return clicked;
        }

        static void Texture(Rect area, Texture2D tex)
        {
            if (tex != null) GUI.DrawTexture(area, tex, ScaleMode.ScaleAndCrop, true);
        }

        static void Fill(Rect area, Color color)
        {
            var before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = before;
        }

        static void Frame(Rect rect, Color color, float t)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, t), color);
            Fill(new Rect(rect.x, rect.yMax - t, rect.width, t), color);
            Fill(new Rect(rect.x, rect.y, t, rect.height), color);
            Fill(new Rect(rect.xMax - t, rect.y, t, rect.height), color);
        }
    }
}
