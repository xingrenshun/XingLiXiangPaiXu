using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用「看激励视频领奖励」按钮。
///
/// 注意：本工程（行李箱排序 / BaggageJam）里并不存在 ManagerMecanique、ManagerAds
/// 这两个类型（原文件是从别的工程整体拷贝过来的），旧写法会让整个工程编译失败
/// （Btn_Ad.cs 4 处 CS0246），从而无法打出抖音小游戏包。这里改成自包含实现：
///   · 广告调用统一走 AdSDK 既有入口（AdConfirm 弹窗 / AdManager），不改动 AdManager；
///   · 具体奖励改成 UnityEvent，在 Inspector 里按界面挂接，不同界面可复用。
/// </summary>
public class Btn_Ad : MonoBehaviour
{
    [Tooltip("看完激励视频后触发，奖励逻辑请在 Inspector 里挂接")]
    public UnityEvent onRewarded;

    [Tooltip("勾选后先弹 AdConfirm 确认框（与游戏内其它广告入口保持一致）")]
    [SerializeField] private bool useConfirmPopup = true;

    [Tooltip("防连点间隔（秒，使用 unscaledTime，广告暂停游戏时同样有效）")]
    [SerializeField] private float clickCooldown = 0.5f;

    private float lastClickTime = -999f;

    /// <summary>
    /// 按钮 OnClick 绑定的方法名，保持与原文件一致，已有绑定不受影响。
    /// </summary>
    public void Btn_AdFlashOnClick()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
        {
            Debug.LogWarning("[Btn_Ad] 点击过于频繁，已忽略本次请求");
            return;
        }
        lastClickTime = Time.unscaledTime;

        if (AdManager.Instance == null)
        {
            Debug.LogError("[Btn_Ad] AdManager 未初始化，无法播放激励视频");
            return;
        }

        if (useConfirmPopup)
        {
            AdConfirm.Show(OnAdWatched);
        }
        else
        {
            AdManager.Instance.ShowRewardedVideoAd(OnAdWatched, OnAdFailed);
        }
    }

    private void OnAdWatched()
    {
        Debug.Log("[Btn_Ad] 激励视频观看完成，发放奖励");
        onRewarded?.Invoke();
    }

    private void OnAdFailed(string error)
    {
        Debug.LogError($"[Btn_Ad] 激励视频播放失败：{error}");
    }
}
