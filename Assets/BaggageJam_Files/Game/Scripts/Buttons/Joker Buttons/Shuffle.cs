namespace EKStudio
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;


    public class Shuffle : JokerButtonBase
    {

        [Header("Empty Stock Behaviour")]
        [Tooltip("ON  = when the player runs out of this booster, show the 'watch ad to get reward' popup (AdSDK/AdConfirm), grant Jokers Per Ad and use it right away. (default)\nOFF = open the legacy coin shop panel instead.")]
        [SerializeField] private bool rewardedAdWhenEmpty = true;

        [Tooltip("How many Jokers one rewarded video grants.")]
        [SerializeField] private int jokersPerAd = 3;

        void Start()
        {
            Button.onClick.AddListener(BaggageShuffleButton);
        }

        void BaggageShuffleButton()
        {
            if (TryHandleEmptyStock())
            {
                return;
            }

            UseJoker();
        }

        void UseJoker()
        {
            List<AIStateManager> allAI = new List<AIStateManager>();

            List<AIRowSlot> AISlots = new List<AIRowSlot>();

            AIManager AIManager = AIManager.Instance;
            for (int i = 0; i < AIManager.allRow.Count; i++)
            {
                for (int j = 0; j < AIManager.allRow[i].AIRowSlots.Count; j++)
                {
                    if (AIManager.allRow[i].AIRowSlots[j].CurrentAI != null)
                    {
                        allAI.Add(AIManager.allRow[i].AIRowSlots[j].CurrentAI);
                        AISlots.Add(AIManager.allRow[i].AIRowSlots[j]);
                    }
                }
            }

            if (allAI.Count < 2)
                return;


            List<AIStateManager> lastAllAI = allAI;


            bool isMix = true;

            while (isMix)
            {
                int n = allAI.Count;

                for (int i = n - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);

                    AIStateManager temp = allAI[i];
                    allAI[i] = allAI[j];
                    allAI[j] = temp;
                }

                for (int i = 0; i < allAI.Count; i++)
                {
                    if (allAI[i] == lastAllAI[i])
                    {
                        isMix = false;
                        break;
                    }
                }
            }


            for (int i = 0; i < allAI.Count; i++)
            {
                allAI[i].Agent.enabled = false;


                ParticleFactory.SpawnParticle(ParticleType.BaggageMerge, allAI[i].transform.position, null);

                allAI[i].targetTransform = AISlots[i].transform;
                allAI[i].transform.position = allAI[i].targetTransform.position;

                AISlots[i].SetAI(allAI[i]);
            }

            foreach (var item in allAI)
            {
                item.transform.rotation = Quaternion.identity;
                item.Agent.enabled = true;
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
