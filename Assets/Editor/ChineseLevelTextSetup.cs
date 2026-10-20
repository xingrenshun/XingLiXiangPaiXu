namespace EKStudio
{
    using System.Collections.Generic;
    using TMPro;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 关卡文本中文化（Lvl.N → 第N关）的配套工具。
    ///
    /// 为什么用编辑器脚本、而不是直接改 .prefab / .asset 的 YAML：
    /// Unity 开着工程时会用内存里的副本覆盖外部对序列化资产的改动，
    /// 所以必须让编辑器自己改、自己存（与 HomeSceneSetup 是同一思路）。
    ///
    /// 这个脚本做两件事：
    ///   1) 给 TMP 挂中文回退。Level_Text 用的 Fredoka 只有 45 个拉丁字符、没有汉字字形，
    ///      直接把文本改成中文会渲染成空白/方块。这里把 DroidSansFallback SDF 挂到
    ///      Fredoka 的 fallbackFontAssetTable，并写进 TMP 全局回退表（双保险）。
    ///      数字仍由 Fredoka 出字形，只有「第」「关」走回退字体。
    ///   2) 把 Canvas.prefab 里 Level_Text 的初始文本从 "Lvl.1" 改成「第1关」。
    ///      运行时会由 UIManager.UpdateText() 每秒刷新成「第N关」。
    ///
    /// 执行方式：[InitializeOnLoad] 自动执行一次（EditorPrefs 记标记），
    /// 或菜单 Tools / 关卡文本中文化（第N关）+ TMP 中文回退 手动重跑。
    /// </summary>
    public static class ChineseLevelTextSetup
    {
        // 换版本号可强制重新执行
        private const string DoneKey = "EKStudio.ChineseLevelTextSetup.v1";

        private const string FredokaPath = "Assets/BaggageJam_Files/Game/Font/Fredoka/Fredoka_1.asset";
        private const string CnFontPath = "Assets/AdSDK/Font/DroidSansFallback SDF.asset";
        private const string TmpSettingsPath = "Assets/BaggageJam_Files/TextMesh Pro/Resources/TMP Settings.asset";
        private const string CanvasPrefabPath = "Assets/BaggageJam_Files/Game/Prefabs/Managers/Canvas.prefab";

        private const string LevelTextName = "Level_Text";
        private const string LevelTextValue = "第1关";

        [InitializeOnLoadMethod]
        private static void AutoRun()
        {
            if (EditorPrefs.GetBool(DoneKey, false)) return;

            // 等资源库就绪再改，避免刚启动时拿不到资产
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Apply();
                    EditorPrefs.SetBool(DoneKey, true);
                }
                catch (System.Exception e)
                {
                    // 刻意不写标记：下次编译会自动重试
                    Debug.LogError("[中文关卡文本] 处理失败，下次编译将重试：" + e);
                }
            };
        }

        [MenuItem("Tools/关卡文本中文化（第N关）+ TMP 中文回退")]
        private static void ApplyMenu()
        {
            Apply();
            Debug.Log("[中文关卡文本] 手动执行完成");
        }

        private static void Apply()
        {
            TMP_FontAsset cnFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CnFontPath);
            if (cnFont == null)
            {
                Debug.LogError("[中文关卡文本] 找不到中文字体资产：" + CnFontPath);
                return;
            }

            AttachFallbackToFredoka(cnFont);
            AttachFallbackToTmpSettings(cnFont);
            FixCanvasPrefabText();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Fredoka 自带的回退表：挂上中文字体。</summary>
        private static void AttachFallbackToFredoka(TMP_FontAsset cnFont)
        {
            TMP_FontAsset fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FredokaPath);
            if (fredoka == null)
            {
                Debug.LogWarning("[中文关卡文本] 找不到字体资产：" + FredokaPath);
                return;
            }

            if (fredoka.fallbackFontAssetTable == null)
                fredoka.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (fredoka.fallbackFontAssetTable.Contains(cnFont)) return;

            fredoka.fallbackFontAssetTable.Add(cnFont);
            EditorUtility.SetDirty(fredoka);
            Debug.Log("[中文关卡文本] 已给 " + fredoka.name + " 挂上中文回退：" + cnFont.name);
        }

        /// <summary>TMP 全局回退表：兜住所有没单独配置回退的字体。</summary>
        private static void AttachFallbackToTmpSettings(TMP_FontAsset cnFont)
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning("[中文关卡文本] 找不到 TMP Settings：" + TmpSettingsPath);
                return;
            }

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty list = so.FindProperty("m_fallbackFontAssets");
            if (list == null)
            {
                Debug.LogWarning("[中文关卡文本] TMP Settings 里没有 m_fallbackFontAssets 字段");
                return;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == cnFont) return;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = cnFont;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            Debug.Log("[中文关卡文本] 已把 " + cnFont.name + " 写入 TMP 全局回退表");
        }

        /// <summary>把 Canvas.prefab 里 Level_Text 的初始文本改成中文。</summary>
        private static void FixCanvasPrefabText()
        {
            GameObject probe = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
            if (probe == null)
            {
                Debug.LogWarning("[中文关卡文本] 找不到预制体：" + CanvasPrefabPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
            if (root == null) return;

            try
            {
                bool changed = false;
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].gameObject.name != LevelTextName) continue;
                    if (texts[i].text == LevelTextValue) continue;

                    Debug.Log("[中文关卡文本] Level_Text 初始文本 \"" + texts[i].text + "\" → \"" + LevelTextValue + "\"");
                    texts[i].text = LevelTextValue;
                    changed = true;
                }

                if (!changed) return;

                PrefabUtility.SaveAsPrefabAsset(root, CanvasPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
