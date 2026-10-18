namespace EKStudio
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// home 场景「开始游戏」按钮：点击后跳转到目标场景（默认 Game_Scene）。
    /// 目标场景必须已加入 Build Settings，否则无法加载。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HomePlayButton : ButtonBase
    {
        [SerializeField] private string sceneName = "Game_Scene";

        void Start()
        {
            Button.onClick.AddListener(OnPlayButtonClicked);
        }

        void OnPlayButtonClicked()
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[HomePlayButton] 未填写目标场景名（sceneName），无法跳转。", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[HomePlayButton] 场景 \"{sceneName}\" 未加入 Build Settings，无法加载。请到 File > Build Settings > Scenes In Build 添加。", this);
                return;
            }

            // 防止加载前的瞬间连点
            Button.interactable = false;
            SceneManager.LoadScene(sceneName);
        }
    }
}
