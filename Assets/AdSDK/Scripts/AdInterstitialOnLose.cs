using System.Collections;
using UnityEngine;
using EKStudio;   // EventManager / GameEvent 在 EKStudio 命名空间下

/// <summary>
/// 失败结算后延时展示抖音插屏广告。
///
/// 背景：AdSDK 里原本的 DelayShowInterAd 是「面板 OnEnable / OnDisable」驱动的，
/// 但本工程的场景（Game_Scene）和所有预制体里都没有挂过它，
/// 所以 AdManager.ShowInterstitialAd() 实际上从来没有被调用过。
///
/// 这里改成监听游戏自带的事件总线 GameEvent.OnLose（由 SlotManager 广播），
/// 用代码自安装，无需改动任何场景 / 预制体，也不依赖 AdManager 之外的任何东西。
/// 频率限制在 TikTokPlatform 里还会再兜一层（启动 15s 内不展示、两次插屏间隔 ≥30s）。
///
/// 不需要插屏广告时，直接删掉本文件即可。
/// </summary>
public class AdInterstitialOnLose : MonoBehaviour
{
    [Tooltip("失败后延时多少秒展示插屏广告")]
    [SerializeField] private float delayTime = 3f;

    [Tooltip("两次插屏广告之间的最小间隔（秒），防止连续失败时频繁弹出")]
    [SerializeField] private float minInterval = 60f;

    private static AdInterstitialOnLose instance;
    private Coroutine delayRoutine;
    private float lastShowTime = -999f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject("[AdInterstitialOnLose]");
        DontDestroyOnLoad(host);
        host.AddComponent<AdInterstitialOnLose>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EventManager.AddHandler(GameEvent.OnLose, OnLose);
    }

    private void OnDestroy()
    {
        EventManager.RemoveHandler(GameEvent.OnLose, OnLose);

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnLose()
    {
        if (Time.realtimeSinceStartup - lastShowTime < minInterval)
        {
            Debug.LogWarning($"[AdInterstitialOnLose] 距上次插屏不足 {minInterval} 秒，本次跳过");
            return;
        }

        if (delayRoutine != null)
        {
            StopCoroutine(delayRoutine);
        }

        delayRoutine = StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        Debug.Log($"[AdInterstitialOnLose] 游戏失败，{delayTime} 秒后展示插屏广告");
        yield return new WaitForSecondsRealtime(delayTime);
        delayRoutine = null;

        if (AdManager.Instance == null)
        {
            Debug.LogError("[AdInterstitialOnLose] AdManager 未初始化，无法展示插屏广告");
            yield break;
        }

        lastShowTime = Time.realtimeSinceStartup;
        AdManager.Instance.ShowInterstitialAd();
    }
}
