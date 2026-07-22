using RoR2;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RainrotSharedUtils.Shelters.ShelterUtilsModule;

namespace SwanSongExtended.Components
{
    public class BaseStormEliteBehavior : CharacterBody.ItemBehavior
    {
        bool bodyIsInSuperShelter = false;
        public static float minWeakTime = 10f;
        private float weakTimeCountdown = 0;
        public virtual void FixedUpdate()
        {
            if (body == null || !NetworkServer.active)
                return;

            weakTimeCountdown -= Time.fixedDeltaTime;
            if (weakTimeCountdown > 0)
                return;

            bodyIsInSuperShelter = IsBodySuperSheltered(body, body.bestFitRadius);
            if (bodyIsInSuperShelter != body.HasBuff(StormsCore.StormEliteWeak))
            {
                if (bodyIsInSuperShelter)
                {
                    body.AddBuff(StormsCore.StormEliteWeak);
                    weakTimeCountdown = minWeakTime;
                    //play vfx
                    EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ArmorReductionOnHit.PulverizedEffect_prefab).WaitForCompletion()
                        , new EffectData
                    {
                        scale = body.radius,
                        origin = body.corePosition
                    }, true);
                }
                else
                {
                    body.RemoveBuff(StormsCore.StormEliteWeak);
                }
            }
        }

    }
}
