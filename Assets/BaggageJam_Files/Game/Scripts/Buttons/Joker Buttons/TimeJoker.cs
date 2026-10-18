namespace EKStudio
{
    using System.Collections;
    using System.Collections.Generic;
    using DG.Tweening;
    using Dreamteck.Splines;
    using UnityEngine;


    public class TimeJoker : JokerButtonBase
    {

        [Header("Empty Stock Behaviour")]
        [Tooltip("ON  = when the player runs out of this booster, show the 'watch ad to get reward' popup (AdSDK/AdConfirm), grant Jokers Per Ad and use it right away. (default)\nOFF = open the legacy coin shop panel instead.")]
        [SerializeField] private bool rewardedAdWhenEmpty = true;

        [Tooltip("How many Jokers one rewarded video grants.")]
        [SerializeField] private int jokersPerAd = 3;

        void Start()
        {
            Button.onClick.AddListener(TimeStop);
        }

        void TimeStop()
        {
            if (TryHandleEmptyStock())
            {
                return;
            }

            UseJoker();
        }

        void UseJoker()
        {
            ConveyorBelt conveyor = ConveyorBelt.Instance;

            foreach (GameObject item in conveyor.allRampBaggage)
            {
                item.GetComponent<SplineFollower>().followSpeed = 0;
            }

            foreach (GameObject item in conveyor.allConveyor)
            {
                item.GetComponent<SplineFollower>().followSpeed = 0;
            }

            DOVirtual.DelayedCall(3, () => TimeContinue());


            JokerDataSO.JokerCount--;

            EventManager.Broadcast(GameEvent.OnJoker);
        }

        void TimeContinue()
        {
            ConveyorBelt conveyor = ConveyorBelt.Instance;

            foreach (GameObject item in conveyor.allRampBaggage)
            {
                item.GetComponent<SplineFollower>().followSpeed = 1;
            }

            foreach (GameObject item in conveyor.allConveyor)
            {
                item.GetComponent<SplineFollower>().followSpeed = 1;
            }
        }

        /// <summary>
        /// Handles the click when the player has no booster left.
        /// Returns true when this method already took over the click.
        /// </summary>
        bool TryHandleEmptyStock()
        {
            if (JokerDataSO.JokerCount > 0)
            {
                return false;
            }

            if (!rewardedAdWhenEmpty)
            {
                buyJokerPanel.GetComponentInChildren<JokerBuyButton>().InteractableCheckAndTextUpdate();
                buyJokerPanel.SetActive(true);
                return true;
            }

            // Rewarded video entry point: use the AdSDK popup / AdManager as-is.
            AdConfirm.Show(() =>
            {
                JokerDataSO.JokerCount += jokersPerAd;
                EventManager.Broadcast(GameEvent.OnJoker);
                UseJoker();
            });

            return true;
        }
    }

}
