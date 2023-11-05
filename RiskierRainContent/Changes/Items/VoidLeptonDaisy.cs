using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using RiskierRain.CoreModules;
using static RiskierRain.CoreModules.StatHooks;
using R2API;
using RiskierRain.Items;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using EntityStates;
using static R2API.RecalculateStatsAPI;
using HarmonyLib;

namespace RiskierRain.Changes.Items
{
    class VoidLeptonDaisy : ItemBase<VoidLeptonDaisy>
    {
        public static BuffDef lilyRageBuff;
        public static int duration = 20;

        public static float aspdBoostBase = 0.30f;
        public static float aspdBoostStack = 0.15f;
        float cdrBase = 1 - aspdBoostBase;
        float cdrStack = 1 - aspdBoostStack;
        //public static float mspdBoostBase = 0.25f;
        //public static float mspdBoostStack = 0.25f;
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
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

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlPhrygianLily.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_PHRYGIANLILY.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
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

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        void CreateBuff()
        {
            lilyRageBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                lilyRageBuff.name = "phrygianRage";
                lilyRageBuff.buffColor = new Color(0.8f, 0f, 0f);
                lilyRageBuff.canStack = false;
                lilyRageBuff.isDebuff = false;
                lilyRageBuff.iconSprite = Assets.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            };
            Assets.buffDefs.Add(lilyRageBuff);
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
                    ApplyCooldownScale(skillLocator.primary, cdrBoost);
                    ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                    ApplyCooldownScale(skillLocator.utility, cdrBoost);
                    ApplyCooldownScale(skillLocator.special, cdrBoost);
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
