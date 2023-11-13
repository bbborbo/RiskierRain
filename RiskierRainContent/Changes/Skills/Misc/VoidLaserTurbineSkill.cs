using BepInEx.Configuration;
using R2API;
using RiskierRainContent.States;
using RiskierRainContent.CoreModules;
using RiskierRainContent.EntityState.Huntress;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Skills
{
    class VoidLaserTurbineSkill : SkillBase<VoidLaserTurbineSkill>
    {
        public static GameObject tracerLaser;
        public static string _SkillName = "Heaven-Piercer";
        public override string SkillName => _SkillName;

        public override string SkillDescription => $"Fire a devastating laser beam, piercing ALL enemies and terrain " +
            $"for {Tools.ConvertDecimal(VoidLaserBeam.damageCoefficient)} damage.";

        public override string SkillLangTokenName => "VOIDLASERTURBINESKILL";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(VoidLaserBeam);

        public override string CharacterName => "";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                rechargeStock: 0,
                baseRechargeInterval: 0,
                interruptPriority: EntityStates.InterruptPriority.PrioritySkill,
                dontAllowPastMaxStocks: true,
                mustKeyPress: true
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateSkill();
            CreateTracer();
            Hooks();
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

            Assets.CreateEffect(tracerLaser);
        }
    }
}
