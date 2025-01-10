using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using static SwanSongExtended.Modules.HitHooks;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using EntityStates;
using static R2API.RecalculateStatsAPI;
using HarmonyLib;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class VoidLeptonDaisy : ItemBase<VoidLeptonDaisy>
    {
        public override bool isEnabled => false;

        public static BuffDef lilyRageBuff;
        public static int duration = 20;

        public static float aspdBoostBase = 0.30f;
        public static float aspdBoostStack = 0.15f;
        float cdrBase = 1 - aspdBoostBase;
        float cdrStack = 1 - aspdBoostStack;
        //public static float mspdBoostBase = 0.25f;
        //public static float mspdBoostStack = 0.25f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDef;
        public override string ItemName => "Phrygian Lily";

        public override string ItemLangTokenName => "PHRYGIANLILY";

        public override string ItemPickupDesc => "Enter a rage after activating the teleporter. <style=cIsVoid>Corrupts all Lepton Daisies.</style>";

        public override string ItemFullDescription => $"<style=cIsDamage>Enter a rage</style> for {duration} seconds upon activating the teleporter. " +
            $"While enraged, increases attack speed by +{Tools.ConvertDecimal(aspdBoostBase)} " +
            $"<style=cStack>(+{Tools.ConvertDecimal(aspdBoostStack)} per stack)</style> " +
            $"and reduces skill cooldowns by {Tools.ConvertDecimal(aspdBoostBase)} " +
            $"<style=cStack>({Tools.ConvertDecimal(aspdBoostStack)} per stack)</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.HoldoutZoneRelated, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.Damage };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlPhrygianLily.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_PHRYGIANLILY.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            lilyRageBuff = Content.CreateAndAddBuff(
                "bdLilyRage",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), //replace me
                new Color(0.8f, 0f, 0f),
                false, false
                );
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.TeleporterInteraction.ChargingState.OnEnter += PhrygianLilyEnrage;
            On.RoR2.CharacterBody.RecalculateStats += LilyCDR;
            GetStatCoefficients += LilySpeed;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }

        private void PhrygianLilyEnrage(On.RoR2.TeleporterInteraction.ChargingState.orig_OnEnter orig, BaseState self)
        {
            orig(self);

            CharacterBody body = PlayerCharacterMasterController.instances[0].body;
            if(body != null)
            {
                int lilyCount = GetCount(body);
                if (lilyCount > 0)
                    body.AddTimedBuffAuthority(lilyRageBuff.buffIndex, duration);
            }
        }

        private void LilyCDR(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int lilyCount = GetCount(self);
            if (lilyCount > 0 && self.HasBuff(lilyRageBuff))
            {
                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    float cdrBoost = (cdrBase) * Mathf.Pow(cdrStack, lilyCount - 1);
                    Tools.ApplyCooldownScale(skillLocator.primary, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.utility, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.special, cdrBoost);
                }
            }
        }

        private void LilySpeed(CharacterBody sender, StatHookEventArgs args)
        {
            //Debug.Log("dsfjhgbds");
            int lilyCount = GetCount(sender);
            if (lilyCount > 0 && sender.HasBuff(lilyRageBuff))
            {
                float aspdBoost = aspdBoostBase + aspdBoostStack * (lilyCount - 1);
                args.attackSpeedMultAdd += aspdBoost;
            }
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.TPHealingNova, //consumes lepton daisy
                itemDef2 = VoidLeptonDaisy.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
