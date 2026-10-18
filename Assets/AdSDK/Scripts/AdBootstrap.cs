using UnityEngine;

/// <summary>
/// Makes sure an AdManager instance exists at runtime.
///
/// This project ships no AdManager component in any scene or prefab, so
/// AdManager.Instance would be null and every rewarded-video call would fail.
/// The bootstrap only *creates* the manager, preferring
/// Assets/AdSDK/Resources/AdManager.prefab so the app id and the ad unit ids
/// stay editable in the Inspector. The ad logic inside AdManager and
/// TikTokPlatform is left untouched.
///
/// Runs AfterSceneLoad on purpose: a manager provided by a scene always wins.
/// </summary>
public static class AdBootstrap
{
    private const string AdManagerPrefabPath = "AdManager";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAdManager()
    {
        if (AdManager.Instance != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(AdManagerPrefabPath);
        if (prefab != null)
        {
            UnityEngine.Object.Instantiate(prefab);
            if (AdManager.Instance != null)
            {
                return;
            }
        }

        GameObject host = new GameObject("[AdManager]");
        host.AddComponent<AdManager>();
        Debug.LogWarning("[AdBootstrap] Resources/AdManager.prefab not found, " +
                         "created AdManager with its default settings.");
    }
}
