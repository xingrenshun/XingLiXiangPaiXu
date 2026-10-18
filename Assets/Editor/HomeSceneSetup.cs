using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// home 场景初始化（幂等，可反复执行）：
///   1) 把 home.unity 放到 Build Settings 第 0 位（启动场景），Game_Scene.unity 紧随其后；
///   2) 保证 home 场景里的 play 对象挂着 EKStudio.HomePlayButton（sceneName = Game_Scene）；
///   3) 修主界面 UI 自适应：CanvasScaler = Scale With Screen Size，
///      参考分辨率 750x1334（= home 的设计尺寸），Screen Match Mode = Shrink。
///
/// 为什么用 Editor 脚本而不是直接改文件：
///   运行中的 Unity 内存里持有 .unity / EditorBuildSettings.asset 的旧副本，
///   保存时会把手改的磁盘内容整体覆盖掉。必须让 Unity 自己改自己存。
///
/// 自动执行一次（EditorPrefs 记标记，避免每次重编译都去开场景），
/// 需要重跑时用菜单 Tools/Home 场景初始化（Build 顺序 + play 按钮 + UI 自适应）。
/// </summary>
public static class HomeSceneSetup
{
    private const string HomeScenePath = "Assets/BaggageJam_Files/Game/Scene/home.unity";
    private const string GameScenePath = "Assets/BaggageJam_Files/Game/Scene/Game_Scene.unity";
    private const string PlayObjectName = "play";
    private const string GameSceneName = "Game_Scene";
    private const string DoneKey = "EKStudio.HomeSceneSetup.Done.v2";   // v1 → v2：新增 UI 自适应修复

    /// <summary>home 的 UI 就是按 750x1334 这块设计尺寸摆的（bg 的 RectTransform 实测 750x1334）。</summary>
    private static readonly Vector2 DesignResolution = new Vector2(750f, 1334f);

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;

        // 等这次编译/导入彻底结束、编辑器空闲后再动场景
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Run(false)) EditorPrefs.SetBool(DoneKey, true);
        };
    }

    [MenuItem("Tools/Home 场景初始化（Build 顺序 + play 按钮 + UI 自适应）")]
    private static void RunFromMenu()
    {
        bool ok = Run(true);
        EditorPrefs.SetBool(DoneKey, ok);
    }

    /// <returns>是否已处于目标状态（true = 无需再自动重跑）</returns>
    private static bool Run(bool verbose)
    {
        bool buildChanged = EnsureBuildSettings();
        bool sceneChanged = EnsureHomeScene();

        if (buildChanged || sceneChanged)
            Debug.Log($"[HomeSceneSetup] 完成：Build Settings 变更={buildChanged}，home 场景变更={sceneChanged}。");
        else if (verbose)
            Debug.Log("[HomeSceneSetup] 已是目标状态，无需改动。");

        return !buildChanged && !sceneChanged;
    }

    /// <summary>把 home 放到索引 0、Game_Scene 放到索引 1，其余原样跟在后面。</summary>
    private static bool EnsureBuildSettings()
    {
        var current = EditorBuildSettings.scenes.ToList();

        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(HomeScenePath)) ||
            string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(GameScenePath)))
        {
            Debug.LogError("[HomeSceneSetup] 找不到 home.unity 或 Game_Scene.unity，跳过 Build Settings 处理。");
            return false;
        }

        // 已经加过就不再动，避免以后和用户自己维护的顺序打架
        if (current.Any(s => s.path == HomeScenePath)) return false;

        var target = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(HomeScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        target.AddRange(current.Where(s => s.path != HomeScenePath && s.path != GameScenePath));

        EditorBuildSettings.scenes = target.ToArray();
        return true;
    }

    /// <summary>打开 home 场景，修 play 按钮 + Canvas 自适应，改完保存。</summary>
    private static bool EnsureHomeScene()
    {
        Scene scene = FindOpenScene(HomeScenePath);
        bool openedHere = false;

        if (!scene.IsValid())
        {
            scene = EditorSceneManager.OpenScene(HomeScenePath, OpenSceneMode.Additive);
            openedHere = true;
        }

        if (!scene.IsValid())
        {
            Debug.LogError($"[HomeSceneSetup] 打不开 {HomeScenePath}，跳过 home 场景处理。");
            return false;
        }

        bool changed = EnsurePlayButton(scene);
        changed |= EnsureCanvasScaling(scene);

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (openedHere) EditorSceneManager.CloseScene(scene, true);

        return !changed;
    }

    /// <summary>保证 play 对象上有 HomePlayButton 且 sceneName 正确。</summary>
    private static bool EnsurePlayButton(Scene scene)
    {
        bool changed = false;
        int matched = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != PlayObjectName) continue;

                var button = t.GetComponent<Button>();
                if (button == null) continue;   // 只认真正带 Button 的那个 play
                matched++;

                var comp = t.GetComponent<EKStudio.HomePlayButton>();
                if (comp == null)
                {
                    comp = t.gameObject.AddComponent<EKStudio.HomePlayButton>();
                    changed = true;
                }

                var so = new SerializedObject(comp);
                var prop = so.FindProperty("sceneName");
                if (prop != null && prop.stringValue != GameSceneName)
                {
                    prop.stringValue = GameSceneName;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }
        }

        if (matched == 0)
            Debug.LogWarning($"[HomeSceneSetup] 在 home 场景里没找到带 Button 组件的 \"{PlayObjectName}\" 对象，未做任何改动。");

        return changed;
    }

    /// <summary>
    /// 修主界面「UI 太小 / 四周留底色」：CanvasScaler 从 Constant Pixel Size
    /// 改成 Scale With Screen Size（参考 = 750x1334 设计尺寸）+ Shrink。
    /// Shrink = 取宽高两个缩放比里较大的那个，保证 750x1334 的设计画面
    /// 在任何机型上都至少铺满屏幕（只会裁边，不会留黑边）。
    /// </summary>
    private static bool EnsureCanvasScaling(Scene scene)
    {
        bool changed = false;
        int matched = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            {
                matched++;
                bool dirty = false;

                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    dirty = true;
                }

                if (scaler.referenceResolution != DesignResolution)
                {
                    scaler.referenceResolution = DesignResolution;
                    dirty = true;
                }

                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Shrink)
                {
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
                    dirty = true;
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(scaler);
                    changed = true;
                    Debug.Log($"[HomeSceneSetup] 已把 \"{scaler.gameObject.name}\" 的 CanvasScaler 设为 " +
                              $"Scale With Screen Size / {DesignResolution.x}x{DesignResolution.y} / Shrink。");
                }
            }
        }

        if (matched == 0)
            Debug.LogWarning("[HomeSceneSetup] home 场景里没找到 CanvasScaler，UI 自适应未处理。");

        return changed;
    }

    private static Scene FindOpenScene(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.path == path) return s;
        }
        return default;
    }
}
