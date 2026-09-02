using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Orbs;
using UnityEngine.AddressableAssets;
using System.Linq;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

using RoR2.Items;
using SwanSongExtended.Modules;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    public class AoeOnCrit : ItemBase<AoeOnCrit>
    {
        public static ModdedProcType AoeOnCritProc;
        public static GameObject laserOrbEffect;
        public static int bouncesBase = 1;
        public static int bouncesStack = 1;
        public static int freeCrit = 5;
        public static float firstBounceDamageBase = 0.6f;
        public static float firstBounceDamageStack = 0.2f;
        public static float lastBounceDamageCoefficient = 0.4f;
        public static float procCoefficientPerBounce = 0.35f;
        public static float bounceRange = 60f;
        public override string ItemName => "Hypo-Threader";

        public override string ItemLangTokenName => "AOEONCRIT";

        public override string ItemPickupDesc => "\'Critical Strikes\' fire a laser chain.";

        public override string ItemFullDescription => $"Gain {DamageColor(freeCrit + "% critical chance")}. " +
            $"{DamageColor("Critical strikes")} fire a {DamageColor("bouncing laser")} for " +
            $"{DamageColor(firstBounceDamageBase.AsPercent())} {StackText("+" + firstBounceDamageStack.AsPercent())} TOTAL damage " +
            $"on to up to {DamageColor($"{bouncesBase} {StackText("+" + bouncesStack)} enemies")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.Technology };

        public override GameObject ItemModel => LoadDropPrefab("mdlAoeOnCrit");

        public override Sprite ItemIcon => LoadItemIcon("texIconAoeOnCrit");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }
        public override void Init()
        {
            AoeOnCritProc = ProcTypeAPI.ReserveProcType();
            //SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_DroneWeapons.ChainGunOrbEffect_prefab, (effect) =>
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Drone_Tech.NanoPistolOrbEffect_prefab, CreateOrbEffect);
            base.Init();
        }

        private void CreateOrbEffect(GameObject effect)
        {
            laserOrbEffect = effect.InstantiateClone("AoeOnCritLaserOrb", false);

            laserOrbEffect.transform.localScale *= 2;
            if (laserOrbEffect.TryGetComponent(out OrbEffect orbEffect))
            {
                SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Drone_Tech.NanoPistolRicochetImpactEffect_prefab, (impactEffect) =>
                {
                    GameObject laserImpactEffect = impactEffect.InstantiateClone("AoeOnCritLaserImpact", false);

                    Transform ringBurst0 = laserImpactEffect.transform.GetChild(0);
                    MakeRed(ref ringBurst0, new Color32(74, 0, 0, 255));
                    Transform ringBurst1 = laserImpactEffect.transform.GetChild(1);
                    MakeRed(ref ringBurst1, new Color32(74, 0, 0, 255));
                    Transform shockwave = laserImpactEffect.transform.GetChild(2);
                    MakeRed(ref shockwave, new Color32(74, 0, 0, 255));
                    Transform impactBits = laserImpactEffect.transform.GetChild(3);
                    MakeRed(ref impactBits, new Color32(74, 0, 0, 255));
                    Transform impactPixels = laserImpactEffect.transform.GetChild(4);
                    MakeRed(ref impactPixels, new Color32(74, 0, 0, 255));
                    Transform impactSmall = laserImpactEffect.transform.GetChild(5);
                    MakeRed(ref impactSmall, new Color32(74, 0, 0, 255));
                    //Transform pointLight = laserImpactEffect.transform.GetChild(6);

                    Content.CreateAndAddEffectDef(laserImpactEffect);

                    void MakeRed(ref Transform t, Color32 color)
                    {
                        if(t != null && t.TryGetComponent(out ParticleSystemRenderer psr))
                        {
                            Material newMat = UnityEngine.Object.Instantiate(psr.material);
                            newMat.SetColor("_TintColor", color);
                            psr.material = newMat;
                        }
                    }
                    orbEffect.endEffect = laserImpactEffect;
                });
                //orbEffect.
            }

            Transform trailParent = laserOrbEffect.transform.GetChild(0);
            if (trailParent)
            {
                if (trailParent.TryGetComponent(out ParticleSystemRenderer psr1))
                {
                    //tr.time = 2.0f;
                    //
                    ////tr.startWidth = 1.0f;
                    ////tr.endWidth = 0.7f;
                    //tr.startColor = new Color(1, 0.6f, 0.5f);
                    //tr.endColor = new Color(0.6f, 0.0f, 0.0f);
                }

                //if (trailParent.TryGetComponent(out AnimateShaderAlpha asa))
                //{
                //    asa.timeMax = 1.0f;
                //}

                Transform trailLight = trailParent.GetChild(0);
                if(trailLight.gameObject.TryGetComponent(out TrailRenderer trail1))
                {
                    Material mat = UnityEngine.Object.Instantiate(trail1.material);
                    mat.SetColor("_TintColor", new Color32(176, 2, 0, 255));
                    trail1.material = mat;
                }
                Transform trailDark = trailParent.GetChild(1);
                if (trailDark.gameObject.TryGetComponent(out TrailRenderer trail2))
                {
                    Material mat = UnityEngine.Object.Instantiate(trail2.material);
                    mat.SetColor("_TintColor", new Color32(38, 2, 0, 255));
                    mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampDiamondLaser_png).WaitForCompletion());
                    //mat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_SpeedBoostPickup.texNegateAttackTrail_png).WaitForCompletion());
                    //.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_Railgunner.texRailgunnerBeamMask_png).WaitForCompletion());
                    mat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Gravekeeper.texChainTrailMask_png).WaitForCompletion());
                    trail2.material = mat;
                }
            }

            Content.CreateAndAddEffectDef(laserOrbEffect);
        }

        public override void Hooks()
        {
            GetStatCoefficients += AoeOnCritBaseCrit;
        }

        private void AoeOnCritBaseCrit(CharacterBody sender, StatHookEventArgs args)
        {
            int count = GetCount(sender);
            if(count > 0)
            {
                args.critAdd += 5;
            }
        }
    }

    public class AoeOnCritBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => AoeOnCrit.instance.ItemsDef;
        private static GameObject orbEffectObject => AoeOnCrit.laserOrbEffect;
        private static ModdedProcType procType => AoeOnCrit.AoeOnCritProc;

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            if (damageReport.damageInfo.procChainMask.HasModdedProc(procType))
                return;
            if (!damageReport.damageInfo.crit)
                return;
            SphereSearch sphereSearch = new SphereSearch
            {
                mask = LayerIndex.entityPrecise.mask,
                origin = damageReport.damageInfo.position,
                queryTriggerInteraction = QueryTriggerInteraction.Collide,
                radius = AoeOnCrit.bounceRange
            };

            TeamMask teamMask = TeamMask.GetEnemyTeams(TeamIndex.Player);
            List<HurtBox> hurtBoxesList = new List<HurtBox>();

            sphereSearch
                .RefreshCandidates()
                .FilterCandidatesByHurtBoxTeam(teamMask)
                .FilterCandidatesByDistinctHurtBoxEntities()
                .GetHurtBoxes(hurtBoxesList);
            if (hurtBoxesList.Count <= 0)
                return;
            hurtBoxesList = hurtBoxesList
                .Where((hurtBox) => hurtBox.healthComponent.body != damageReport.victimBody)
                .OrderBy((hurtBox) => (hurtBox.transform.position - damageReport.damageInfo.position).sqrMagnitude)
                .ToList();
            if (hurtBoxesList.Count <= 0)
                return;
            int index = 0;// UnityEngine.Random.RandomRangeInt(0, hurtBoxesList.Count);
            HurtBox target = hurtBoxesList[index];

            float damageCoefficient = AoeOnCrit.firstBounceDamageBase + AoeOnCrit.firstBounceDamageStack * (stack - 1);
            int bounces = AoeOnCrit.bouncesBase + AoeOnCrit.bouncesStack * (stack - 1);
            float lastBounceDamageMultiplier = AoeOnCrit.lastBounceDamageCoefficient / damageCoefficient;
            // |  ||
            // |, |_
            float loss = bounces > 1f ? Mathf.Pow(lastBounceDamageMultiplier, 1f / ((float)bounces - 1f)) : 0f;

            ChainGunOrb chainGunOrb = new ChainGunOrb(orbEffectObject);
            chainGunOrb.damageValue = damageReport.damageInfo.damage * damageCoefficient;
            chainGunOrb.isCrit = true;
            chainGunOrb.teamIndex = TeamComponent.GetObjectTeam(this.body.gameObject);
            chainGunOrb.attacker = this.body.gameObject;
            chainGunOrb.procCoefficient = AoeOnCrit.procCoefficientPerBounce;
            chainGunOrb.procChainMask = damageReport.damageInfo.procChainMask;
            chainGunOrb.procChainMask.AddModdedProc(procType);
            chainGunOrb.origin = damageReport.damageInfo.position;
            chainGunOrb.target = target;
            chainGunOrb.speed = 600f;
            chainGunOrb.bouncesRemaining = bounces - 1;
            chainGunOrb.bounceRange = AoeOnCrit.bounceRange;
            chainGunOrb.damageCoefficientPerBounce = loss;// AoeOnCrit.lastBounceDamageMultiplier;
            chainGunOrb.bouncedObjects = new List<HealthComponent>() { damageReport.victimBody.healthComponent };
            chainGunOrb.targetsToFindPerBounce = 1;
            chainGunOrb.canBounceOnSameTarget = false;
            chainGunOrb.damageColorIndex = DamageColorIndex.Item;
            OrbManager.instance.AddOrb(chainGunOrb);
        }
    }
}
