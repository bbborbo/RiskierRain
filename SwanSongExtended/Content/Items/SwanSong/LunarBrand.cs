using BepInEx.Configuration;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.StatHooks;
using static SwanSongExtended.Tools;
using SwanSongExtended.Modules;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended.Items
{
    class LunarBrand : ItemBase<LunarBrand>
    {
        public static BuffDef CauterizeBuff;
        public static int duration = 5;
        public static int durationStack = 5;
        public static int cauterizeBlockChance = 15;

        public static float cauterizeDamageCoef = 4f;
        public static float cauterizeDamageStack = 2f;
        public static float cauterizeProcCoef = 0f;
        public static int burnThreshold = 5;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Starfire Brand";

        public override string ItemLangTokenName => "LUNARBRAND";

        public override string ItemPickupDesc => "Cauterize burning enemies, inflicting heavy damage " +
            "<style=cIsHealth>AND increasing their armor, rendering them invulnerable to Bleed.</style>";

        public override string ItemFullDescription => $"Gain <style=cIsDamage>{SwanSongPlugin.brandBurnChance}% ignite chance</style>. " + 
            $"Inflicting <style=cIsDamage>{burnThreshold}</style> stacks of burn Cauterizes enemies " +
            $"for <style=cIsDamage>{duration}</style> seconds <style=cStack>(+{durationStack} per stack)</style>, " +
            $"dealing <style=cIsDamage>{Tools.ConvertDecimal(cauterizeDamageCoef)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(cauterizeDamageStack)} per stack)</style> damage through armor. " +
            $"<style=cIsHealth>Cauterized enemies are invulnerable to Bleed and have +{cauterizeBlockChance} armor.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag [] { ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.Damage };
        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlLunarBrand.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_LUNARBRAND.png");


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            CauterizeBuff = Content.CreateAndAddBuff(
                "bdCauterize",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), //replace me
                new Color(0.2f, 0f, 0.1f),
                true, false
                );
            base.Init();
        }

        public override void Hooks()
        {
            GetMoreStatCoefficients += AddBurnChance;
            On.RoR2.GlobalEventManager.ProcessHitEnemy += BrandOnHit;
            On.RoR2.HealthComponent.TakeDamageProcess += CauterizeBuffBehavior;
        }

        private void CauterizeBuffBehavior(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            int buffCount = self.body.GetBuffCount(CauterizeBuff.buffIndex);
            if (buffCount <= 0)
            {
                orig(self, damageInfo);
                return;
            }

            if (Util.CheckRoll(cauterizeBlockChance * Math.Min(buffCount, 6), 0f, null))
            {
                EffectData effectData = new EffectData
                {
                    origin = damageInfo.position,
                    rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
                };
                EffectManager.SpawnEffect(HealthComponent./*private*/AssetReferences.bearEffectPrefab, effectData, true);
                //Util.PlaySound(StealthMode.enterStealthSound, self.gameObject);
                damageInfo.rejected = true;
            }
            orig(self, damageInfo);
        }

        private void AddBurnChance(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                args.burnChanceOnHit += SwanSongPlugin.brandBurnChance;
            }
        }



        private void BrandOnHit(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;
            CharacterBody attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
            int itemCount = GetCount(attackerBody);
            if (itemCount > 0)
            {
                int burnCount = victimBody.GetBuffCount(RoR2Content.Buffs.OnFire);
                int strongBurnCount = victimBody.GetBuffCount(DLC1Content.Buffs.StrongerBurn);
                int cauterizeCount = victimBody.GetBuffCount(CauterizeBuff);
                int threshold = burnThreshold;
                //if 1
                while (burnCount + strongBurnCount >= threshold * (cauterizeCount + 1))//cauterize when burn > 5, remove 5 burn too
                {
                    int i = 0;
                    while(i < threshold)
                    {
                        if (burnCount > 0)
                        {
                            victimBody.healthComponent.body.RemoveOldestTimedBuff(RoR2Content.Buffs.OnFire);
                            burnCount--;
                            i++;
                        }
                        else if (strongBurnCount > 0)
                        {
                            victimBody.healthComponent.body.RemoveOldestTimedBuff(DLC1Content.Buffs.StrongerBurn);
                            strongBurnCount--;
                            i++;
                        }
                        else
                            break;
                    }

                    Cauterize(attackerBody, damageInfo, victimBody);//do the thing
                    threshold += burnThreshold;
                }
            }
        }

        private void Cauterize(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victim)
        {
            DamageInfo cauterizeHit = new DamageInfo()
            {
                attacker = attackerBody.gameObject,
                crit = damageInfo.crit,
                damage = (cauterizeDamageCoef + cauterizeDamageStack) * attackerBody.damage,
                damageType = DamageType.BypassArmor | DamageType.BypassBlock,
                damageColorIndex = DamageColorIndex.Item,
                force = Vector3.zero,
                position = victim.transform.position,
                procChainMask = damageInfo.procChainMask,
                procCoefficient = cauterizeProcCoef
            };
            victim.healthComponent.TakeDamage(cauterizeHit); //deal damage
            DotController bleedDot = DotController.FindDotController(victim.gameObject);
            Tools.ClearDotStacksForType(bleedDot, DotController.DotIndex.Bleed);
            victim.AddTimedBuffAuthority(CauterizeBuff.buffIndex, duration + durationStack); //apply buff

            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova"), new EffectData
            {
                origin = victim.corePosition,
                scale = victim.bestFitRadius + 4
            }, true);
        }
    }
}
