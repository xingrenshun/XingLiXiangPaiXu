namespace EKStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class GameManager : InstanceManager<GameManager>
    {
        public GameData data;
    
        void Awake()
        {
    #if !UNITY_EDITOR
        AllSaveManager.Load(data);
    #endif
    
        }
    
        void Start()
        {
            InvokeRepeating(nameof(SaveData), 1, .1f);
    
            EventManager.Broadcast(GameEvent.OnJoker);
    
            Time.timeScale = 20;
            Invoke(nameof(TimeScaleNormal), 20);
        }
    
        void Update()
        {
#if UNITY_EDITOR
            // ===== 编辑器调试快捷键：按空格直接通关 =====
            // 走的是和真实胜利完全一样的链路（AIStateManager 检测到通关后也是这么广播的）：
            //   OnWin -> UIManager 弹出胜利面板 + NextButton 刷新金币文本
            // 点“下一关”按钮后依旧是正常的加金币 / 进下一关流程。
            // 只在编辑器里编译，不会打进真机包，对线上玩法零影响。
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[调试] 空格快捷键：直接触发本关胜利");
                EventManager.Broadcast(GameEvent.OnWin);
            }
#endif
        }
        private void SaveData()
        {
            AllSaveManager.Save(data);
        }
    
        private void TimeScaleNormal()
        {
            Time.timeScale = 1;
        }
    }
    
}
