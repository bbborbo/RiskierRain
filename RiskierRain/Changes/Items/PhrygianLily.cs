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
    class PhrygianLily : ItemBase<PhrygianLily>
    {
        public ExpansionDef SOTVExpansionDef;//move this to a better place
        public static BuffDef lilyRageBuff;
        public static int duration = 30;

        public static float aspdBoostBase = 0.40f;
        public static float aspdBoostStack = 0.20f;
        //public static float mspdBoostBase = 0.25f;
        //public static float mspdBoostStack = 0.25f;
        public override string ItemName => "Phrygian Lily";

        public override string ItemLangTokenName => "PHRYGIANLILY";

        public override string ItemPickupDesc => "Enter a rage for when activating the teleporter.";

        public override string ItemFullDescription => $"<style=cIsDamage>Enter a rage</style> for 30 seconds upon activating the teleporter. Rage gives +40% attack speed <style=cStack>(+20%)</style> and -40% <style=cStack> (-20% per stack)</style>cooldown reduction.";

        public override string ItemLore =>
@"Order: Lay-Z Mushroom Travel Buddy
Tracking Number: 58***********
Estimated Delivery: 09/23/2056
Shipping Method:  Priority/Biological
Shipping Address:444 Slate Drive, Mars
Shipping Details:

Thank you for your purchase!

Directions:
Turn nozzle to ‘open’. Spores will disperse into the air, causing time to warp and pass slower, thus shortening your wait as the rest of reality will be experiencing time faster. It may take some time for the warping effects to occur; leave the spore bottle in one area to maximize spore count. If traveling in a small enclosed space, the spores may eventually fill the entire area. Ventilate regularly to prevent oversaturation.
To end time warp effect, close nozzle and leave or ventilate the affected area. Always close nozzle before leaving affected area; partial bodily exposure to time warpage may have unwanted effects.
Note- you may experience time normally for several seconds or minutes after the warping begins; this is normal. Simply wait for time to slow for you too (the slow-moving objects will resume their normal speed and unaffected objects will appear to speed up) and then happy waiting!

Warnings:
Objects appear to move slower, but carry the same force as they would normally. Do not interact with normally fast moving or forceful objects in an unsafe manner.
The affected area experienced lowered temperature; you may want to wear warm clothes or turn your heater up. Thermometers do not accurately measure temperature in affected area; assume practical temperatures to be up to 50 degrees lower than measured.
(bold text)Do not open bottle. Handle with extreme care. Ventilate regularly.(/bold)

FUN-GUYS Inc. is not liable for any illness, injury, death, extended or permanent change in time perception, spacial warping, mania or lethargy, hallucination, paranoia, acute panic attack, or otherwise dissatisfactory results. All purchases are final.";

        public override ItemTier Tier => ItemTier.VoidTier2;
        public ExpansionDef requiredExpansion => SOTVExpansionDef;//ExpansionCatalog._expansionDefs.DLC1; //what the fuck
        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

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

            int lilyCount = GetCount(body);
            if (lilyCount > 0)
                body.AddTimedBuffAuthority(lilyRageBuff.buffIndex, duration);
        }

        public override void Init(ConfigFile config)
        {
            SOTVExpansionDef = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
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
                lilyRageBuff.iconSprite = RiskierRainPlugin.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            };
            Assets.buffDefs.Add(lilyRageBuff);
        }

        private void LilyCDR(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int lilyCount = GetCount(self);
            if (lilyCount > 0 && self.HasBuff(lilyRageBuff))
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = 1 - aspdBoostBase;
                if (lilyCount > 1)
                    cdrBoost *= Mathf.Pow(1 - aspdBoostStack, lilyCount - 1);

                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
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
                itemDef2 = PhrygianLily.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
