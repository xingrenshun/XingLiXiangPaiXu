namespace EKStudio
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    public class BaggageHint : JokerButtonBase
    {

        [Header("Empty Stock Behaviour")]
        [Tooltip("ON  = when the player runs out of this booster, show the 'watch ad to get reward' popup (AdSDK/AdConfirm), grant Jokers Per Ad and use it right away. (default)\nOFF = open the legacy coin shop panel instead.")]
        [SerializeField] private bool rewardedAdWhenEmpty = true;

        [Tooltip("How many Jokers one rewarded video grants.")]
        [SerializeField] private int jokersPerAd = 3;

        void Start()
        {
            Button.onClick.AddListener(BaggageHintButton);
        }

        void BaggageHintButton()
        {
            if (TryHandleEmptyStock())
            {
                return;
            }

            UseJoker();
        }

        void UseJoker()
        {
            List<Baggage> allBaggage = new List<Baggage>();

            foreach (var item in ConveyorBelt.Instance.allConveyor)
            {
                if (item.transform.childCount > 0)
                {
                    allBaggage.Add(item.transform.GetChild(0).GetComponent<Baggage>());
                }
            }

            foreach (var item in SlotManager.Instance.allBaggage)
            {
                allBaggage.Add(item);
            }

            AIManager AIManager = AIManager.Instance;
            BaggageType AIType = BaggageType.None;

            for (int i = 0; i < AIManager.allRow.Count; i++)
            {
                if (AIManager.allRow[i].AIRowSlots[0].AIType != BaggageType.None)
                {
                    AIType = AIManager.allRow[i].AIRowSlots[0].AIType;
                }
            }

            List<Baggage> mergeBaggages = new List<Baggage>();
            int mergeBaggageCount = 0;

            foreach (var item in allBaggage)
            {
                if (item.baggageType == AIType)
                {
                    mergeBaggageCount++;
                    mergeBaggages.Add(item);
                }
            }

            if (AIType == BaggageType.None)
            {
                return;
            }

            if (mergeBaggageCount > 2)
            {
                SlotManager.Instance.Merge(mergeBaggages);
            }
            else
            {
                for (int i = 0; i < 3 - mergeBaggageCount; i++)
                {
                    Vector3 pos = Camera.main.transform.position + new Vector3(0, -8f, 5 + (i * 2.5f));

                    BaggageFactory.SpawnBaggage(AIType, pos, Quaternion.identity, out GameObject factoryProduct);
                    mergeBaggages.Add(factoryProduct.GetComponent<Baggage>());
                }

                SlotManager.Instance.Merge(mergeBaggages);
            }

            JokerDataSO.JokerCount--;
            EventManager.Broadcast(GameEvent.OnJoker);
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
