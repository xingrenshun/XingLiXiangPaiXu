namespace EKStudio
{
    using System.Collections;
    using System.Collections.Generic;
    using DG.Tweening;
    using UnityEngine;


    public class BaggageBackBand : JokerButtonBase
    {

        [Header("Empty Stock Behaviour")]
        [Tooltip("ON  = when the player runs out of this booster, show the 'watch ad to get reward' popup (AdSDK/AdConfirm), grant Jokers Per Ad and use it right away. (default)\nOFF = open the legacy coin shop panel instead.")]
        [SerializeField] private bool rewardedAdWhenEmpty = true;

        [Tooltip("How many Jokers one rewarded video grants.")]
        [SerializeField] private int jokersPerAd = 3;

        void Start()
        {
            Button.onClick.AddListener(BaggageBack);
        }

        void BaggageBack()
        {
            if (TryHandleEmptyStock())
            {
                return;
            }

            UseJoker();
        }

        void UseJoker()
        {
            SlotManager slotManager = SlotManager.Instance;

            List<Transform> conveyorList = new List<Transform>();

            while (conveyorList.Count < slotManager.allBaggage.Count)
            {
                int randomIndex = Random.Range(0, ConveyorBelt.Instance.allConveyor.Count);

                bool isContains = conveyorList.Contains(ConveyorBelt.Instance.allConveyor[randomIndex].transform);
                bool isChildNull = ConveyorBelt.Instance.allConveyor[randomIndex].transform.childCount == 0;

                if (!isContains && isChildNull)
                {
                    conveyorList.Add(ConveyorBelt.Instance.allConveyor[randomIndex].transform);
                }
            }

            for (int i = 0; i < slotManager.allBaggage.Count; i++)
            {
                Baggage baggage = slotManager.allBaggage[i];

                baggage.CurrentSlot.ClearSlot();

                baggage.transform.parent = conveyorList[i];

                baggage.transform.DOLocalRotate(new Vector3(90f, 0f, 90f), .5f).SetDelay(i * .1f);

                baggage.transform.DOLocalMove(Vector3.zero, .5f).SetDelay(i * .1f).
                OnComplete(() =>
                {
                    baggage.isHaveSlot = false;
                    baggage.isSelect = false;

                    baggage.GetComponent<Collider>().enabled = true;
                    baggage.GetComponent<Collider>().isTrigger = false;

                    baggage.GetComponent<Rigidbody>().isKinematic = false;
                });

            }

            slotManager.allBaggage.Clear();

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
