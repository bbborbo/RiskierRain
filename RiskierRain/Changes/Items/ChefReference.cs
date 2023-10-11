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
        int maxBurnBonusBase = 1;
        int maxBurnBonusStack = 4;
        float bonusDamagePerBurn = 0.04f;

        public override string ItemName => "Chef \u2019Stache";

        public override string ItemLangTokenName => "CHEFITEM";

        public override string ItemPickupDesc => "Ignites on hit. Enemies take more damage from you while they burn.";

        public override string ItemFullDescription => 
            $"<style=cIsDamage>{RiskierRainPlugin.ignitionTankBurnChance}%</style> chance to ignite on hit. " +
            $"Deal <style=cIsDamage>{Tools.ConvertDecimal(bonusDamagePerBurn)}</style> more damage to enemies <style=cIsDamage>per instance of burn,</style> " +
            $"for up to a maximum of <style=cIsUtility>{maxBurnBonusBase + maxBurnBonusStack} debuffs</style> <style=cStack>(+{maxBurnBonusStack} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };
        public override BalanceCategory Category => BalanceCategory.StateOfDamage;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlChefStache.prefab");

        public override Sprite ItemIcon => RiskierRainPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CHEFITEM.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            BurnStatCoefficient += AddBurnChance;
            On.RoR2.HealthComponent.TakeDamage += TakeMoreDamageWhileBurning;
        }

        private void AddBurnChance(CharacterBody sender, BurnEventArgs args)
        {
            if(GetCount(sender) > 0)
            {
                args.burnChance += RiskierRainPlugin.stacheBurnChance;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
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
