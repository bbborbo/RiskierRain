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
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class ShellShield : ItemBase<ShellShield>
    {
        public override bool isEnabled => true;
        #region config
        [AutoConfig("Percent Barrier Base", 0.4f)]
        public static float percentBase = 0.4f;
        [AutoConfig("Percent Barrier Stack", 0)]
        public static float percentStack = 0;
        [AutoConfig("Flat Barrier Base", 0)]
        public static int flatBase = 0;
        [AutoConfig("Flat Barrier Stack", 40)]
        public static int flatStack = 40;
        [AutoConfig("Barrier Decay Freeze Base", 1f)]
        public static float decayFreezeBase = 1f;
        [AutoConfig("Barrier Decay Freeze Stack", 1f)]
        public static float decayFreezeStack = 1f;

        public override string ConfigName => "Item: " + ItemName;
        #endregion
        #region abstract
        public override string ItemName => "Shell Shield";

        public override string ItemLangTokenName => "SHELLSHIELD";

        public override string ItemPickupDesc => "Standing still blocks one hit, granting barrier and regeneration.";

        public override string ItemFullDescription => $"While not moving, you are {UtilityColor("protected")} against one hit. " +
            $"Using your protection {UtilityColor("blocks the hit")}, then grants " +
            $"{HealingColor($"{ConvertDecimal(percentBase)} of maximum health {StackText($"+{flatStack} flat")} in barrier")}, " +
            $"and grants {HealingColor("Regenerative")} and {UtilityColor("freezes")} barrier decay for " +
            $"{UtilityColor($"{decayFreezeBase}")} {StackText($"+{decayFreezeStack}")} seconds. " +
            $"Recharges after {UtilityColor("7")} seconds.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/SecretsOfTheScug/Items/mdlShellShield.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/shellshield.png");
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
            shellShieldBuff = Content.CreateAndAddBuff("ShellShieldBuff", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                    Color.cyan,
                    false,
                    false);
            shellShieldCooldown = Content.CreateAndAddBuff("ShellShieldBuff", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                    Color.cyan,
                    false,
                    true);
            shellShieldBuff.isHidden = true;
            shellShieldCooldown.isHidden = true;
            base.Init();
        }
        private void ShellShieldOnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            CharacterBody body = self?.body;
            int itemCount = GetCount(body);
            if (itemCount <= 0 || body.notMovingStopwatch <= 0.1f || body.HasBuff(shellShieldCooldown))
            {
                orig(self, damageInfo);
                return;
            }
            damageInfo.damage = 0;//janky hack mate
            damageInfo.rejected = true;
            EffectData effectData = new EffectData
            {
                origin = damageInfo.position,
                rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
            };
            EffectManager.SpawnEffect(HealthComponent./*private*/AssetReferences.bearEffectPrefab, effectData, true);
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
            self.body.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, decayFreezeBase + (decayFreezeStack * itemCount));
            self.body.AddTimedBuffAuthority(shellShieldCooldown.buffIndex, 7);
        }
    }
}
