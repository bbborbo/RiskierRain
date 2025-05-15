using EntityStates;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.Language.Styling;
using SwanSongExtended.States.Loader;
using RoR2.Skills;
using UnityEngine.AddressableAssets;
using R2API;

namespace SwanSongExtended.Skills
{
    class DynamicPunchSkill : SkillBase<DynamicPunchSkill>
    {
        public override bool isEnabled => false; 
        #region config
        public override string ConfigName => "Skills : Loader : Dynamic Punch";

        

        [AutoConfig("Damage Coefficient", 3f)]
        public static float damageCoefficient = 3f;
        [AutoConfig("Proc Coefficient", 1f)]
        public static float procCoefficient = 1f;
        [AutoConfig("Base Duration", 1.35f)]
        public static float baseDuration = 1.35f;
        [AutoConfig("Force", 500f)]
        public static float force = 500f;
        #endregion
        #region override
        public override string SkillName => "Dynamic Punch";

        public override string SkillDescription => $"Charge a flurry of punches. Releasing early will instead throw a single punch, {UtilityColor("stunning and knocking enemies back")} for {DamageValueText(damageCoefficient)}";

        public override string SkillLangTokenName => "LOADERDYNAMICPUNCH";

        public override UnlockableDef UnlockDef => null;

        public override Sprite Icon => null;

        public override Type ActivationState => typeof(ChargeDynamicPunch);

        public override Type BaseSkillDef => typeof(SkillDef);

        public override string CharacterName => "LoaderBody";

        public override SkillSlot SkillSlot => SkillSlot.Primary;

        public override float BaseCooldown => 1f;

        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;

        public override SimpleSkillData SkillData => new SimpleSkillData();

        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;
        #endregion
        public override void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += LoaderArmorBreakStats;
        }


        private void LoaderArmorBreakStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(loaderArmorBreak))
            {
                args.armorAdd -= 20;//yeagh
            }
        }

        public override void Init()
        {
            base.Init();
            Content.AddEntityState(typeof(DynamicPunchJab));
            Content.AddEntityState(typeof(DynamicPunchRush));
            loaderArmorBreak = Content.CreateAndAddBuff("LoaderArmorBreak", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ArmorReductionOnHit/texBuffPulverizeIcon.tif").WaitForCompletion(),
                    Color.grey,
                    true,
                    true);
        }
        #region buff
        public static BuffDef loaderArmorBreak;
        #endregion
    }
}
