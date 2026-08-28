using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static MoreStats.StatHooks;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Elites
{
    public class GlassAspect : EliteEquipmentBase<GlassAspect>
    {
        #region config

        public static int healthGateCountBase = 1;
        public static int healthGateCountPerSize = 1;

        public static float shatterProtectionDuration = 1;
        public static float shatterDuration = 0.75f;
        public static float shatterDurationMultiplierPerSize = 0.3f;
        public static float shatterMoveSpeed = 2f;
        public static float shatterAttackSpeed = 1f;
        public static float shatterCooldownDuration = 0.5f;

        public static float healthTotalMult = 0.2f;
        #endregion
        public static BuffDef shatteredBuff;
        public override string EliteEquipmentName => "Fractured Belief";

        public override string EliteAffixToken => "MIRRORED";

        public override string EliteEquipmentPickupDesc => "Become an aspect of vanity.";

        public override string EliteEquipmentFullDescription => EliteEquipmentPickupDesc;

        public override string EliteEquipmentLore => "";

        public override string EliteModifier => "Mirrored";

        public override float EliteHealthModifier => 0f;

        public override float EliteDamageModifier => 0f;

        public override EliteModule.EliteTiers EliteTier => EliteModule.EliteTiers.Lunar;

        public override string EliteRampTextureName => "texRampGlass";

        public override GameObject EliteEquipmentModel => LoadDropPrefab();// LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LoadItemIcon();// LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteLightning/texBuffAffixBlue.tif").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            shatteredBuff = Modules.Content.CreateAndAddBuff("bdMirrorEliteShattered",
                null,
                buffColor: new Color32(100, 156, 220, 155),
                canStack: false,
                isDebuff: false,
                isHidden: false
                );
            SwanSongPlugin.LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ArmorReductionOnHit.bdPulverized_asset, (ctx) =>
            {
                shatteredBuff.iconSprite = ctx.iconSprite;
            });
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            GetStatCoefficients += GlassAspect_GetStatCoefficients;
            GetMoreStatCoefficients += GlassAspect_GetMoreStatCoefficients;
            OnBodyHealthGateTriggeredGlobal += GlassAspect_OnBodyHealthGateTriggeredGlobal;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (IsElite(self))
                self.isGlass = true;
        }

        private void GlassAspect_GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (!IsElite(sender))
                return;
            args.baseCurseAdd += 1f;
            args.healthTotalMult *= healthTotalMult;
            int shatterCount = sender.GetBuffCount(shatteredBuff);
            if(shatterCount > 0)
            {
                args.moveSpeedMultAdd += shatterMoveSpeed * shatterCount;
                args.attackSpeedMultAdd += shatterAttackSpeed * shatterCount;
                args.allSkills.cooldownMultiplier *= Mathf.Pow(shatterCooldownDuration, shatterCount);
            }
        }

        private void GlassAspect_OnBodyHealthGateTriggeredGlobal(CharacterBody sender)
        {
            if (!IsElite(sender))
                return;

            float multiplier = 1 + shatterDurationMultiplierPerSize * sender.radius;
            sender.AddTimedBuff(RoR2Content.Buffs.Intangible, shatterProtectionDuration);
            sender.AddTimedBuff(RoR2Content.Buffs.Immune, shatterProtectionDuration);
            sender.AddTimedBuff(shatteredBuff, shatterDuration);
        }

        private void GlassAspect_GetMoreStatCoefficients(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (!IsElite(sender))
                return;
            int gateCt = healthGateCountBase + healthGateCountPerSize * Mathf.CeilToInt(sender.radius);
            int glassCt = sender.inventory.GetItemCountEffective(RoR2Content.Items.LunarDagger);
            if (glassCt > 0)
                gateCt += glassCt * 1;
            args.ModifyHealthGateCount(gateCt, true);
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }
}
