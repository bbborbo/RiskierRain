using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Components
{
    internal class InteractableCurseController : InteractableBuffController
    {
        public GameObject voidExecuteEffect => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CritGlassesVoid/CritGlassesVoidExecuteEffect.prefab").WaitForCompletion();
        internal override void Start()
        {
            base.Start();
            buffDef = Assets.voidCradleCurse;
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.onTeleporterBeginChargingGlobal += VoidExplode;
            }
        }

        private void VoidExplode(TeleporterInteraction interaction)
        {
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= VoidExplode;
            EffectManager.SpawnEffect(voidExecuteEffect, new EffectData
            {
                origin = base.transform.position,
                scale = 3f
            }, true);
            GameObject.Destroy(base.gameObject);
        }
    }
}
