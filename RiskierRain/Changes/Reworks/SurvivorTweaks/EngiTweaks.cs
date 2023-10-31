using EntityStates.Engi.EngiBubbleShield;
using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.SurvivorTweaks
{
    class EngiTweaks : SurvivorTweakModule
    {
        public static float mineArmingDuration = 2f;//3f
        public static GameObject bubbleShieldPrefab;
        public static float bubbleShieldRadius = 30;//20
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

            //utility
            LanguageAPI.Add("ENGI_UTILITY_DESCRIPTION", "Place an <style=cIsUtility>impenetrable shield</style> that blocks all incoming damage, and <style=cIsUtility>slows enemies</style> inside.");
            bubbleShieldPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiBubbleShield.prefab").WaitForCompletion().InstantiateClone("NewEngiBubbleShield", true);
            ChildLocator cl = bubbleShieldPrefab.GetComponent<ChildLocator>();
            GameObject bubble = cl.FindChild(Deployed.childLocatorString).gameObject;
            bubble.transform.localScale = Vector3.one * bubbleShieldRadius;
            BuffWard buffWard = bubble.AddComponent<BuffWard>();
            buffWard.buffDef = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdSlow50.asset").WaitForCompletion();
            buffWard.buffDuration = 0.1f;
            buffWard.interval = 0.1f;
            buffWard.radius = bubbleShieldRadius / 2;
            buffWard.invertTeamFilter = true;
            On.EntityStates.Engi.EngiWeapon.FireMines.OnEnter += ReplaceBubbleShieldPrefab;
            On.EntityStates.Engi.EngiBubbleShield.Deployed.FixedUpdate += BubbleBuffwardTeam;
        }

        private void BubbleBuffwardTeam(On.EntityStates.Engi.EngiBubbleShield.Deployed.orig_FixedUpdate orig, Deployed self)
        {
            bool deployed = self.hasDeployed;
            orig(self);
            if(!deployed && self.hasDeployed)
            {
                BuffWard buffWard = self.gameObject.GetComponentInChildren<BuffWard>();
                if(buffWard != null)
                {
                    buffWard.teamFilter = self.outer.GetComponent<TeamFilter>();
                }
            }
        }

        private void ReplaceBubbleShieldPrefab(On.EntityStates.Engi.EngiWeapon.FireMines.orig_OnEnter orig, EntityStates.Engi.EngiWeapon.FireMines self)
        {
            if(self is FireBubbleShield)
            {
                self.projectilePrefab = bubbleShieldPrefab;
            }
            orig(self);
        }

        private void ChangeMineArmTime(On.EntityStates.Engi.Mine.MineArmingWeak.orig_FixedUpdate orig, MineArmingWeak self)
        {
            MineArmingWeak.duration = mineArmingDuration;
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
