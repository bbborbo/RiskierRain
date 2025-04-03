using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static MoreStats.StatHooks;

namespace SwanSongExtended.Items
{
    class ShellShield : ItemBase<ShellShield>
    {
        #region config
        [AutoConfig("Percent Barrier Base", 0.2f)]
        public static float percentBase = 0.2f;
        [AutoConfig("Percent Barrier Stack", 0)]
        public static float percentStack = 0;
        [AutoConfig("Flat Barrier Base", 0)]
        public static int flatBase = 0;
        [AutoConfig("Flat Barrier Stack", 20)]
        public static int flatStack = 20;
        [AutoConfig("Barrier Decay Freeze Base", 0)]
        public static float decayFreezeBase = 0;
        [AutoConfig("Barrier Decay Freeze Stack", 0.5f)]
        public static float decayFreezeStack = 0.5f;

        public override string ConfigName => "Item: " + ItemName;
        #endregion
        #region abstract
        public override string ItemName => "Shell Shield";

        public override string ItemLangTokenName => "SHELLSHIELD";

        public override string ItemPickupDesc => "Standing still blocks one hit and grants barrier.";

        public override string ItemFullDescription => "Standing still blocks one hit and grants barrier.";

        public override string ItemLore => "Standing still blocks one hit and grants barrier.";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility};

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSOTS;
        #endregion
        public static BuffDef shellShieldBuff;
        public static BuffDef shellShieldCooldown;
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ShellShieldOnTakeDamage;
            GetMoreStatCoefficients += ShellShieldBarrierFreeze;
        }

        private void ShellShieldBarrierFreeze(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.HasBuff(shellShieldBuff))
            {
                args.barrierFreezeCount += 1;
            }
        }

        public override void Init()
        {
            base.Init();
            shellShieldBuff = Content.CreateAndAddBuff("ShellShieldBuff", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                    Color.cyan,
                    false,
                    false);
            shellShieldCooldown = Content.CreateAndAddBuff("ShellShieldBuff", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                    Color.cyan,
                    false,
                    true);
            shellShieldCooldown.isHidden = true;
        }
        private void ShellShieldOnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {

            CharacterBody body = self?.body;
            int itemCount = GetCount(body);
            if (itemCount <= 0 || body.notMovingStopwatch <= 0.1f || shellShieldCooldown)
            {
                orig(self, damageInfo);
                return;
            }
            damageInfo.damage = 0;//janky hack mate
            //damageInfo.rejected = true;
            orig(self, damageInfo);
            ShellShieldBarrier(self, itemCount);
        }

        private void ShellShieldBarrier(HealthComponent self, int itemCount)
        {
            itemCount -= 1;
            float barrierToAdd = self.fullCombinedHealth * (percentBase + percentStack * itemCount);
            barrierToAdd += flatBase + flatStack * itemCount;
            self.AddBarrierAuthority(barrierToAdd);

            self.body.AddTimedBuffAuthority(shellShieldBuff.buffIndex, decayFreezeBase + (decayFreezeStack * itemCount));
            self.body.AddTimedBuffAuthority(shellShieldCooldown.buffIndex, 7);
        }
    }
}
