using System;
using System.IO;
using System.Linq;
using Ghosts;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildGame
{
    const string ScenePath = "Assets/Game/Scenes/Novel.unity";

    [MenuItem("Ghosts/Build Windows")]
    public static void Windows()
    {
        PreparePlayer();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath },
            "Builds/Windows/GhostsOfYesterday.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        RequireBuild(report, "GHOSTS_BUILD_OK");
    }

    [MenuItem("Ghosts/Export iOS Xcode Project")]
    public static void IOS()
    {
        PreparePlayer();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.ghostsnovel.ghostsofyesterday");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        PlayerSettings.iOS.buildNumber = "2";
        var output = Environment.GetEnvironmentVariable("GHOSTS_IOS_XCODE_PATH");
        if (string.IsNullOrWhiteSpace(output))
            output = "Builds/iOS/Xcode";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.iOS, BuildOptions.None);
        RequireBuild(report, "GHOSTS_IOS_XCODE_OK");
    }

    static void PreparePlayer()
    {
        BackgroundChecks.Run();
        PlayerSettings.companyName = "GhostsNovel";
        PlayerSettings.productName = "Ghosts of Yesterday";
        PlayerSettings.bundleVersion = "1.0.2";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Visual Novel", typeof(Camera), typeof(AudioListener), typeof(Ghosts.Stage));
            var camera = root.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static void RequireBuild(BuildReport report, string marker)
    {
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.result.ToString());
        Debug.Log(marker + " " + report.summary.totalSize + " bytes");
    }

    [MenuItem("Ghosts/Validate Story")]
    public static void Validate()
    {
        var story = new Story(Resources.Load<TextAsset>("Story/book"));
        var font = Resources.Load<Font>("Type/NotoSansKR-Regular");
        if (story.Book.episodes.Length != 50 || story.Book.episodes
            .Where((episode, index) => episode.id != $"ep{index + 1:D3}").Any())
            throw new BuildFailedException("Windows release requires all 50 episodes in order.");
        if (font == null) throw new BuildFailedException("Missing Korean font.");
        foreach (var character in story.Beats.SelectMany(beat =>
                     beat.Line.speaker + beat.Line.text + beat.Scene.place + beat.Scene.time + beat.Episode.title + beat.Scene.gallery).Distinct())
            if (!font.HasCharacter(character))
                throw new BuildFailedException("Missing glyph: " + character);
        foreach (var scene in story.Book.episodes.SelectMany(e => e.scenes))
        {
            if (Resources.Load<Texture2D>("Art/" + scene.art) == null)
                throw new BuildFailedException("Missing art: " + scene.art);
            if (Resources.Load<AudioClip>("Audio/" + scene.music) == null)
                throw new BuildFailedException("Missing music: " + scene.music);
            foreach (var layer in scene.layers)
            {
                var actor = Resources.Load<Texture2D>("Sprites/" + layer.art);
                if (actor == null)
                    throw new BuildFailedException("Missing sprite: " + layer.art);
                var halfWidth = layer.height * actor.width / actor.height / 2;
                if (layer.x - halfWidth < 0 || layer.x + halfWidth > 1440 || layer.y - layer.height < 0 || layer.y > 810)
                    throw new BuildFailedException("Clipped character layer: " + scene.id);
            }
            foreach (var effect in scene.lines.Select(l => l.sfx).Where(id => !string.IsNullOrEmpty(id)).Distinct())
                if (Resources.Load<AudioClip>("Audio/" + effect) == null)
                    throw new BuildFailedException("Missing effect: " + effect);
        }
        Debug.Log("GHOSTS_STORY_OK " + story.Book.episodes.Length + " episodes / " + story.Beats.Count + " lines");
    }
}
