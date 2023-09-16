using EntityStates.Engi.Mine;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.SurvivorTweaks
{
    class EngiTweaks : SurvivorTweakModule
    {
        public override string survivorName => "Engineer";

        public override string bodyName => "ENGIBODY";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            //primary
            primary.variants[0].skillDef.cancelSprintingOnActivation = false;
            LanguageAPI.Add("ENGI_PRIMARY_DESCRIPTION", "<style=cIsUtility>Agile.</style> Charge up to <style=cIsDamage>8</style> grenades that deal <style=cIsDamage>100% damage</style> each.");

            //secondary
            IL.EntityStates.Engi.Mine.Detonate.Explode += DetonationRadiusBoost;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += ChangeMineArmTime;
        }

        private void ChangeMineArmTime(On.EntityStates.Engi.Mine.MineArmingWeak.orig_FixedUpdate orig, MineArmingWeak self)
        {
            MineArmingWeak.duration = 2;
            orig(self);
        }

        private void DetonationRadiusBoost(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchStfld<BlastAttack>(nameof(BlastAttack.radius))
                );

            c.EmitDelegate<Func<float, float>>((startRadius) =>
            {
                float endRadius = startRadius + 2;
                return endRadius;
            });
        }
    }
}
