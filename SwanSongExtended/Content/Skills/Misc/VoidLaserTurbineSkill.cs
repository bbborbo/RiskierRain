using BepInEx.Configuration;
using R2API;
using SwanSongExtended.States;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Skills;
using EntityStates;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Skills
{
    class VoidLaserTurbineSkill : SkillBase<VoidLaserTurbineSkill>
    {
        public static GameObject tracerLaser;
        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;

        [AutoConfig("Damage Coefficient", 32f)]
        public static float damageCoefficient = 32f;
        [AutoConfig("Proc Coefficient", 1.0f)]
        public static float procCoefficient = 1.0f;
        [AutoConfig("Base Duration", 0.5f)]
        public static float baseDuration = 0.5f;
        [AutoConfig("Force", 2500f)]
        public static float force = 2500f;
        [AutoConfig("Self Force", 1500f)]
        public static float selfForce = 1500f;
        #region config
        public override string ConfigName => "Skills : Misc : HeavenPiercer";

        #endregion

        public static string _SkillName = "Heaven-Piercer";
        public override string SkillName => _SkillName;

        public override string SkillDescription => $"Fire a {VoidColor("devastating laser beam")}, piercing ALL enemies and terrain " +
            $"for {DamageValueText(VoidLaserBeam.damageCoefficient)}.";

        public override string SkillLangTokenName => "VOIDLASERTURBINESKILL";

        public override UnlockableDef UnlockDef => null;

        public override Sprite Icon => null;

        public override Type ActivationState => typeof(VoidLaserBeam);

        public override string CharacterName => "";

        public override SkillSlot SkillSlot => SkillSlot.Primary;
        public override float BaseCooldown => 0;
        public override InterruptPriority InterruptPriority => InterruptPriority.PrioritySkill;

        public override Type BaseSkillDef => typeof(SkillDef);


        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                rechargeStock: 0,
                dontAllowPastMaxStocks: true,
                mustKeyPress: true
            );

        public override void Hooks()
        {

        }
        public override void Init()
        {
            CreateTracer();
            base.Init();
        }
        private void CreateTracer()
        {
            tracerLaser = Resources.Load<GameObject>("prefabs/effects/tracers/TracerGolem").InstantiateClone("VoidTurbineLaser", false);
            Tracer buckshotTracer = tracerLaser.GetComponent<Tracer>();
            buckshotTracer.speed = 200f;
            buckshotTracer.length = 50f;
            buckshotTracer.beamDensity = 100f;
            VFXAttributes buckshotAttributes = tracerLaser.AddComponent<VFXAttributes>();
            buckshotAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            buckshotAttributes.vfxIntensity = VFXAttributes.VFXIntensity.High;

            Tools.GetParticle(tracerLaser, "SmokeBeam", new Color(0.15f, 0.05f, 0.3f), 5f);
            ParticleSystem.MainModule main = tracerLaser.GetComponentInChildren<ParticleSystem>().main;
            main.startSizeXMultiplier *= 5f;
            main.startSizeYMultiplier *= 5f;
            main.startSizeZMultiplier *= 0.1f;

            Content.CreateAndAddEffectDef(tracerLaser);
        }
    }
}