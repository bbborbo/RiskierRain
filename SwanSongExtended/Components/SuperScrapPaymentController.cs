using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.Items;

namespace SwanSongExtended.Components
{
    public class SuperScrapPaymentController : MonoBehaviour
    {
        public static float pollInterval = 0.1f;
        private float pollCountdown = 0.2f;
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
            if (!NetworkServer.active)
                return;

            pollCountdown -= Time.fixedDeltaTime;
            if (pollCountdown > 0)
                return;
            pollCountdown += pollInterval;

            Interactor activator = purchaseInteraction.lastActivator;
            if (purchaseInteraction.available && activator != null)
            {
                CostTypeDef.PayCostContext payCostContext;
                using (CostTypeDef.PayCostContext.pool.Request(out payCostContext))
				{
                    CharacterBody component = activator.GetComponent<CharacterBody>();
                    CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(purchaseInteraction.costType);

                    payCostContext.activator = activator;
					payCostContext.activatorBody = component;
					payCostContext.activatorMaster = (component ? component.master : null);
					payCostContext.activatorInventory = (component ? component.inventory : null);
					payCostContext.purchasedObject = base.gameObject;
					payCostContext.purchaseInteraction = purchaseInteraction;
					payCostContext.costTypeDef = costTypeDef;
					payCostContext.cost = purchaseInteraction.cost;
					payCostContext.rng = purchaseInteraction.rng;
					CostTypeDef.PayCostResults payCostResults;
					using (CostTypeDef.PayCostResults.pool.Request(out payCostResults))
					{
                        MultiShopCardUtils.OnNonMoneyPurchase(payCostContext);
                        //costTypeDef.PayCost(payCostContext, payCostResults);
                        purchaseInteraction.onPurchase.Invoke(activator);
                        purchaseInteraction.onDetailedPurchaseServer.Invoke(payCostContext, payCostResults);
                        paymentCreditsRemaining -= purchaseInteraction.cost;
                    }
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
