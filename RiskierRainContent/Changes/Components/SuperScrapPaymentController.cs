using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Changes.Components
{
    public class SuperScrapPaymentController : MonoBehaviour
    {
        public PurchaseInteraction purchaseInteraction;
        public int paymentCreditsRemaining;
        public void Start()
        {
            purchaseInteraction = GetComponent<PurchaseInteraction>();
            if(purchaseInteraction == null)
            {
                Debug.LogError("No PurchaseInteraction on SuperScrapPaymentController. Aborting!");
                Destroy(this);
            }
        }
        public void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                Interactor activator = purchaseInteraction.lastActivator;
                if (purchaseInteraction.available)
                {
                    purchaseInteraction.onPurchase.Invoke(activator);
                    paymentCreditsRemaining -= purchaseInteraction.cost;
                }
            }

            if ((paymentCreditsRemaining < purchaseInteraction.cost && !ChimeraScrap.shouldSuperScrapOverBuy) 
                || paymentCreditsRemaining <= 0)
            {
                Destroy(this);
            }
        }
    }
}
