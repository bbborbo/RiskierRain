using FruityElites.Modules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace FruityElites.EliteReworks
{
    public class GlacialReworks : EliteReworkBase<GlacialReworks>
    {
        public static GameObject frozenExplosionPrefab;
        [AutoConfig("On-Hit : Chill AoE Radius", "Expressed in meters. Vanilla is 0", 5f)]
        public static float glacialFrostRadius = 5f;
        [AutoConfig("On-Hit : Chill AoE Duration", "Expressed in seconds. Vanilla is 1.5", 2f)]
        public static float glacialFrostDuration = 2f;
        public override string eliteName => "Glacial";

        public override void Init()
        {
            base.Init();
            EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Chef.ChefIceBoxExplosionVFX_prefab, CreateEffect);
        }

        private void CreateEffect(GameObject obj)
        {
            frozenExplosionPrefab = obj.InstantiateClone("FruityGlacialImpactEffect", false);
            frozenExplosionPrefab.transform.localScale *= glacialFrostRadius / 3f;
            //if(frozenExplosionPrefab.TryGetComponent(out ShakeEmitter se))
            //{
            //    UnityEngine.Object.Destroy(se);
            //}
            //Light light = frozenExplosionPrefab.GetComponentInChildren<Light>();
            //if(light != null)
            //{
            //    light.gameObject.SetActive(false);
            //}
            Modules.Content.CreateAndAddEffectDef(frozenExplosionPrefab);
        }

        public override void Hooks()
        {
            base.Hooks();
            On.RoR2.GlobalEventManager.OnHitAllProcess += GlacialChillAoe;
        }

        private void GlacialChillAoe(On.RoR2.GlobalEventManager.orig_OnHitAllProcess orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (glacialFrostRadius <= 0)
                return;
            if (!NetworkServer.active)
                return;
            if (damageInfo.attacker == null)
                return;
            float pco = damageInfo.procCoefficient;
            if (pco <= 0)
                return;
            if(damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.HasBuff(RoR2Content.Buffs.AffixWhite))
            {
                EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>(frozenExplosionPrefab).WaitForCompletion(), 
                    new EffectData
                {
                    origin = damageInfo.position,
                    scale = glacialFrostRadius,
                    rotation = UnityEngine.Random.rotation
                }, true);

                HurtBox[] hurtBoxes = new SphereSearch
                {
                    radius = glacialFrostRadius,
                    mask = LayerIndex.entityPrecise.mask,
                    origin = damageInfo.position,
                    queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
                }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex)).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();
                foreach(HurtBox hurtBox in hurtBoxes)
                {
                    hurtBox.healthComponent.body.AddTimedBuff(RoR2Content.Buffs.Slow80, glacialFrostDuration * pco);
                }
            }
        }
    }
}
