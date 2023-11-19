using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class VoidVampirism : ItemBase<VoidVampirism>
    {
        int maxHeal = 3;
        int bleedPerHeal = 5;
        int vampireBleedChance = 10;

        int maxBurnBonusBase = 1;
        int maxBurnBonusStack = 4;
        float bonusDamagePerBurn = 0.04f;
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;

        public override string ItemName => "Bloodsucking Coralite";

        public override string ItemLangTokenName => "HEALFROMBLEEDINGENEMIES";

        public override string ItemPickupDesc => "Deal more damage against bleeding enemies. Corrupts all Chef Staches.";

        public override string ItemFullDescription => $"{vampireBleedChance}% chance to inflict bleed on hit. " +
            $"Bleeding enemies take Y% more damage from your attacks, up to a maximum of Z times. Corrupts all Chef Staches.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Damage };
        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/coralite.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_HEALFROMBLEEDINGENEMIES.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += VampireHit;
            On.RoR2.CharacterBody.RecalculateStats += VampireBleedChance;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.HealthComponent.TakeDamage += TakeMoreDamageWhileBurning;
        }

        private void TakeMoreDamageWhileBurning(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (damageInfo.attacker != null)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody != null)
                {
                    CharacterBody victimBody = self.body;

                    int currentBuffCount = RiskierRainContent.GetBurnCount(victimBody);
                    int maxBuffCount = maxBurnBonusBase + maxBurnBonusStack * GetCount(attackerBody);

                    damageInfo.damage *= 1 + bonusDamagePerBurn * Mathf.Min(currentBuffCount, maxBuffCount);
                }
            }

            orig(self, damageInfo);
        }

        private void VampireBleedChance(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (GetCount(self) > 0)
            {
                self.bleedChance += vampireBleedChance;
            }
        }

        private void VampireHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            GameObject attacker = damageInfo.attacker;
            if(attacker != null)
            {
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                CharacterBody attackerBody = attacker.GetComponent<CharacterBody>();
                if (victimBody != null && attackerBody != null)
                {
                    int bleedCount = victimBody.GetBuffCount(RoR2Content.Buffs.Bleeding);
                    if (bleedCount > 0)
                    {
                        int itemCount = GetCount(attackerBody);
                        if (itemCount > 0 && damageInfo.procCoefficient > 0)
                        {
                            int healAmount = Mathf.Clamp((bleedCount / bleedPerHeal) + 1, 0, maxHeal * itemCount);
                            attackerBody.healthComponent.Heal(healAmount, new ProcChainMask());
                        }
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = ChefReference.instance.ItemsDef, //consumes leeching seed
                itemDef2 = VoidVampirism.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
