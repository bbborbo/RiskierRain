using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Skills;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Enemies.VoidDreamers
{
    class DreamersATFieldSkill : SkillBase
    {
        public static GameObject atField;
        public static GameObject invisField;
        public override string SkillName => "Extend Field / Defend";

        public override string SkillDescription => "";

        public override string SkillLangTokenName => "DREAMERSSHIELDSKILL";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(DreamersATFieldEnter);

        public override string CharacterName => "COMMANDOBODY";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Special;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 6,
                interruptPriority: EntityStates.InterruptPriority.Frozen
            );

        public override void Hooks()
        {
            On.EntityStates.Engi.EngiBubbleShield.Undeployed.OnEnter += ATFieldHook;
            On.EntityStates.Engi.EngiBubbleShield.Deployed.OnEnter += ATFieldDeploy;
        }

        private void ATFieldDeploy(On.EntityStates.Engi.EngiBubbleShield.Deployed.orig_OnEnter orig, EntityStates.Engi.EngiBubbleShield.Deployed self)
        {
            orig(self);
            Debug.Log("at field deployed arrrrhghhghg");
        }

        private void ATFieldHook(On.EntityStates.Engi.EngiBubbleShield.Undeployed.orig_OnEnter orig, EntityStates.Engi.EngiBubbleShield.Undeployed self)
        {
            orig(self);
            if (self.gameObject.GetComponent<ATFieldComponent>() == null)
            {
                Debug.Log("no at field");
                return;
            }
            Debug.Log("yes at field");
            ProjectileStickOnImpact psoi = self.gameObject.GetComponent<ProjectileStickOnImpact>();
            if (psoi == null)
            {
                Debug.Log("no psoi");
            }
            self.outer.SetNextState(new EntityStates.Engi.EngiBubbleShield.Deployed());
        }

        public override void Init(ConfigFile config)
        {
            Hooks();
            CreateLang();
            CreateSkill();
            CreateProjectile();
        }

        private void CreateProjectile()
        {
            //RoR2/Base/Engi/EngiBubbleShield.prefab
            atField = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiBubbleShield.prefab").WaitForCompletion().InstantiateClone("ATFieldShield", true);
            //invisField = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteHaunted/AffixHauntedWard.prefab").WaitForCompletion().InstantiateClone("ATFieldInvis", true);
            //BuffWard invisWard = invisField.GetComponent<BuffWard>();

            ProjectileStickOnImpact psoi = atField.GetComponent<ProjectileStickOnImpact>();
            psoi.ignoreWorld = true;
            //TeamFilter tf = atField.GetComponent<TeamFilter>();
            Rigidbody rb = atField.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.detectCollisions = false;

            ProjectileSimple ps = atField.GetComponent<ProjectileSimple>();
            ps.velocity = 0;
            ps.desiredForwardSpeed = 0;
            ps.updateAfterFiring = true;

            ApplyTorqueOnStart atos = atField.GetComponent<ApplyTorqueOnStart>();
            UnityEngine.Object.Destroy(atos);

            //BuffWard buffward = atField.AddComponent<BuffWard>();
            //buffward.shape = 0;
            //buffward.buffDef = RoR2Content.Buffs.AffixHauntedRecipient;
            //buffward.radius = 20;//weh
            //buffward.interval = 0.2f;
            //buffward.floorWard = false;
            //buffward.expires = false;
            //buffward.expireDuration = 0;
            //buffward.invertTeamFilter = false;
            //buffward.requireGrounded = false;
            //buffward.animateRadius = invisWard.animateRadius;
            //buffward.radiusCoefficientCurve = invisWard.radiusCoefficientCurve;
            //buffward.rangeIndicator = invisWard.rangeIndicator;
            //buffward.rangeIndicatorScaleVelocity = invisWard.rangeIndicatorScaleVelocity;
            //ProjectileSimple ps2 = invisField.AddComponent<ProjectileSimple>();
            //ps2.velocity = 0;
            //ps2.desiredForwardSpeed = 0;
            //ps2.updateAfterFiring = true;


            atField.AddComponent<ATFieldComponent>();
            //invisField.AddComponent<ATFieldComponent>();

            CoreModules.Assets.projectilePrefabs.Add(atField);
            //Assets.projectilePrefabs.Add(invisField);
        }
    }

    class ATFieldComponent : MonoBehaviour
    {
        SphereSearch sphereSearch;
        const float auraRadius = 10;
        public void Awake()
        {
            sphereSearch = new SphereSearch
            {
                radius = auraRadius,
                queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                mask = LayerIndex.entityPrecise.mask
            };
        }
        public void FixedUpdate()
        {
            DoInvisBuff();
        }
        void DoInvisBuff()
        {
            sphereSearch.origin = gameObject.transform.position;
            this.sphereSearch.RefreshCandidates();
            this.sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
            TeamMask mask = default(TeamMask);
            mask.AddTeam(TeamIndex.Player);//improve later
            sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
            HarmonyLib.CollectionExtensions.Do<HurtBox>(sphereSearch.GetHurtBoxes(), delegate (HurtBox hurtBox)
            {
                UnityEngine.Object x;
                if (hurtBox == null)
                {
                    x = null;
                }
                else
                {
                    HealthComponent healthComponent = hurtBox.healthComponent;
                    x = ((healthComponent != null) ? healthComponent.body : null);
                }
                    hurtBox.healthComponent.body.AddTimedBuff(RoR2Content.Buffs.AffixHauntedRecipient, 1f);                
            });
        }
    }
}
