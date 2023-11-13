using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Components
{
    internal class InteractableBuffController : MonoBehaviour
    {
        private PurchaseInteraction _purchaseInteraction;
        public PurchaseInteraction purchaseInteraction
        {
            get
            {
                if (_purchaseInteraction)
                    return _purchaseInteraction;

                _purchaseInteraction = GetComponent<PurchaseInteraction>();
                return _purchaseInteraction;
            }
        }

        public BuffDef buffDef;
        public float duration = -1;

        internal virtual void Start()
        {
            purchaseInteraction.onPurchase.AddListener(OnPurchase);
        }

        private void OnPurchase(Interactor interactor)
        {
            if (buffDef == null)
                return;

            CharacterBody interactorBody = interactor.GetComponent<CharacterBody>();
            if (interactorBody)
            {
                if(NetworkServer.active)
                    AddDebuff(interactorBody);
            }
        }

        internal virtual void AddDebuff(CharacterBody interactorBody)
        {
            if (duration <= 0)
            {
                interactorBody.AddBuff(buffDef);
            }
            else
            {
                interactorBody.AddTimedBuff(buffDef, duration);
            }
        }
    }
}
