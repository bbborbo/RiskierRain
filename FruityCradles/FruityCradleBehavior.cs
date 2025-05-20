using RoR2;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace FruityCradles
{
    class FruityCradleBehavior : OptionChestBehavior
	{
        internal PurchaseInteraction purchaseInteraction;
        public GameObject voidExecuteEffect => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CritGlassesVoid/CritGlassesVoidExecuteEffect.prefab").WaitForCompletion();
        void Start()
        {
            if (NetworkServer.active)
            {
                if (purchaseInteraction == null)
                    purchaseInteraction = GetComponent<PurchaseInteraction>();
                if (purchaseInteraction != null)
                    purchaseInteraction.onPurchase.AddListener(SoTrue);

                this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
                this.Roll();
            }

            if (TeleporterInteraction.instance)
                TeleporterInteraction.onTeleporterBeginChargingGlobal += VoidCancel;
        }

        private void VoidCancel(TeleporterInteraction obj)
        {
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= VoidCancel;
            //base.PlayAnimation("Body", OpeningLunar.OpeningStateHash, OpeningLunar.OpeningParamHash, OpeningLunar.duration);

            if (voidExecuteEffect != null)
            {
                EffectManager.SpawnEffect(voidExecuteEffect, new EffectData
                {
                    origin = transform.position,
                    scale = 3f
                }, true);
            }
            Destroy(gameObject);
        }

        public void SoTrue(Interactor interactor)
        {
            this.Open();
        }
	}
}
