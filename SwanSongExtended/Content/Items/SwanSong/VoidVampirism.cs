using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class VoidVampirism : ItemBase<VoidVampirism>
    {
        int vampireBleedChance = 10;

        int maxBleedBonusBase = 9;
        int maxBleedBonusStack = 5;
        float bonusDamagePerBleed = 0.04f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override string ItemName => "Bloodsucking Coralite";

        public override string ItemLangTokenName => "HEALFROMBLEEDINGENEMIES";

        public override string ItemPickupDesc => "Deal more damage against bleeding enemies. Corrupts all Chef Staches.";

        public override string ItemFullDescription => $"Gain <style=cIsHealth>{vampireBleedChance}% bleed chance</style>. " +
            $"Bleeding enemies take <style=cIsDamage>+{bonusDamagePerBleed * 100}% more damage</style> from your attacks " +
            $"<style=cIsHealth>per stack of bleed</style>, up to a maximum of " +
            $"{maxBleedBonusBase} <style=cStack>(+{maxBleedBonusStack} per stack)</style> times. " +
            $"<style=cIsVoid>Corrupts all Chef Staches.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Damage };
        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/coralite.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_HEALFROMBLEEDINGENEMIES.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += VampireBleedChance;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.HealthComponent.TakeDamageProcess += TakeMoreDamageWhileBurning;
        }

        private void TakeMoreDamageWhileBurning(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (damageInfo.attacker != null)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody != null)
                {
                    CharacterBody victimBody = self.body;

                    int currentBuffCount = victimBody.GetBuffCount(RoR2Content.Buffs.Bleeding);
                    int maxBuffCount = maxBleedBonusBase + maxBleedBonusStack * GetCount(attackerBody);

                    damageInfo.damage *= 1 + bonusDamagePerBleed * Mathf.Min(currentBuffCount, maxBuffCount);
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
