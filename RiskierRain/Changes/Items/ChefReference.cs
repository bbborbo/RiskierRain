using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.BurnStatHook;

namespace RiskierRain.Items
{
    class ChefReference : ItemBase<ChefReference>
    {
        float burnProcChanceBase = 7;
        float burnProcChanceStack = 8;
        int maxBurnBonusBase = 3;
        int maxBurnBonusStack = 4;
        float bonusDamagePerBurn = 0.03f;
        float burnDuration = 8; //4

        public override string ItemName => "Chef \u2019Stache";

        public override string ItemLangTokenName => "CHEFITEM";

        public override string ItemPickupDesc => "Ignites on hit. Enemies take more damage from you while they burn.";

        public override string ItemFullDescription => $"<style=cIsDamage>{burnProcChanceBase + burnProcChanceStack}%</style> <style=cStack>(+{burnProcChanceStack}% per stack)</style> " +
            $"chance to <style=cIsDamage>ignite</style> enemies on hit for <style=cIsDamage>{(burnDuration/2)*100}% damage</style>. " +
            $"Deal <style=cIsDamage>{Tools.ConvertDecimal(bonusDamagePerBurn)}</style> more damage to enemies <style=cIsDamage>per instance of burn,</style> " +
            $"for up to a maximum of <style=cIsUtility>{maxBurnBonusBase + maxBurnBonusStack} debuffs</style> <style=cStack>(+{maxBurnBonusStack} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Damage };
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfDamage;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            //On.RoR2.GlobalEventManager.OnHitEnemy += BurnOnHit;
            BurnStatCoefficient += AddBurnChance;
            On.RoR2.HealthComponent.TakeDamage += TakeMoreDamageWhileBurning;
        }

        private void AddBurnChance(CharacterBody sender, BurnEventArgs args)
        {
            if(GetCount(sender) > 0)
            {
                Debug.Log("g");
                args.burnChance += RiskierRainPlugin.stacheBurnChance;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void BurnOnHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);


            if (damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;
                if (attackerBody && victimBody)
                {
                    int moustacheItemCount = GetCount(attackerBody);
                    if(moustacheItemCount > 0)
                    {
                        float burnProcChance = burnProcChanceBase + burnProcChanceStack * moustacheItemCount;
                        float procCoeff = Mathf.Sqrt(damageInfo.procCoefficient);
                        int burnProcs = Mathf.FloorToInt((burnProcChance * procCoeff) / 100);

                        for (int i = 0; i < burnProcs; i++)
                        {
                            DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Burn, burnDuration * procCoeff, 1f);
                        }
                        if (Util.CheckRoll((burnProcChance * procCoeff) - burnProcs * 100, attackerBody.master))
                        {
                            DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Burn, burnDuration * procCoeff, 1f);
                        }
                    }
                }
            }
        }

        private void TakeMoreDamageWhileBurning(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if(damageInfo.attacker != null)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody != null)
                {
                    CharacterBody victimBody = self.body;

                    int currentBuffCount = RiskierRainPlugin.GetBurnCount(victimBody);
                    int maxBuffCount = maxBurnBonusBase + maxBurnBonusStack * GetCount(attackerBody);

                    damageInfo.damage *= 1 + bonusDamagePerBurn * Mathf.Min(currentBuffCount, maxBuffCount);
                }
            }

            orig(self, damageInfo);
        }
    }
}
