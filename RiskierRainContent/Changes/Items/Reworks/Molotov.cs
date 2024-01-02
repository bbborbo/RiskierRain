using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRainContent.CoreModules.StatHooks;

namespace RiskierRainContent.Items
{
    class Molotov : ItemBase<Molotov>
    {
        public static GameObject molotovProjectile;
        public static GameObject molotovDotZone;

        public static float procChance = 9;
        public static float baseDamageCoefficient = 1.6f;
        public static float stackDamageCoefficient = 1.6f;
        public static float impactProcCoefficient = 1;
        public static float dotDamageCoefficient = 0.25f; //0.4f
        public static float dotFrequency = 2f; //3f
        public static float dotProcCoefficient = 0.5f;
        public static float blastRadius = 9;//7f

        public override ExpansionDef RequiredExpansion => SotvExpansionDef();

        public override string ItemName => "Molotov (1-Pack)";

        public override string ItemLangTokenName => "BORBOMOLOTOV";

        public override string ItemPickupDesc => "Chance on hit to lob a flaming molotov.";

        public override string ItemFullDescription => $"<style=cIsDamage>{procChance}%</style> chance on hit to " +
            $"lob a <style=cIsDamage>molotov cocktail</style> that <style=cIsDamage>ignites</style> enemies " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(baseDamageCoefficient)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(stackDamageCoefficient)} per stack)</style> TOTAL damage and leaves a " +
            $"<style=cIsDamage>burning</style> area for <style=cIsDamage>{Tools.ConvertDecimal(baseDamageCoefficient * dotDamageCoefficient * dotFrequency)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(stackDamageCoefficient * dotDamageCoefficient * dotFrequency)} per stack)</style> TOTAL damage per second.";

        public override string ItemLore => "Order: Ethanol Bottle (32 oz.), 6 Pack\r\nTracking Number: 81******\r\nEstimated Delivery: 05/8/2058\r\nShipping Method: Priority\r\nShipping Address: Teromere Manor, Privet Road, Mars\nShipping Details: Let our friends inside know that we're coming over for drinks on the 16th of June. It'll be a hell of a party, they should probably hit the road before things get too out of hand.";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/PickupMolotov.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/Molotov/texMolotovIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public static void GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.Missile);
        }

        public override void Hooks()
        {
            On.RoR2.BodyCatalog.Init += GetDisplayRules;
            GetHitBehavior += MolotovOnHit;
        }

        private void MolotovOnHit(CharacterBody attackerBody, DamageInfo damageInfo, GameObject victim)
        {
            int itemCount = GetCount(attackerBody);
            if(itemCount > 0 && !damageInfo.procChainMask.HasProc(ProcType.RepeatHeal))
            {
                if(Util.CheckRoll(procChance * damageInfo.procCoefficient, attackerBody.master))
                {
                    damageInfo.procChainMask.AddProc(ProcType.RepeatHeal);
                    float damageCoefficient = baseDamageCoefficient + stackDamageCoefficient * (itemCount - 1);

                    Vector3 forward = attackerBody.inputBank.aimDirection;
                    forward.y = 0;

                    Vector3 fireDirection = forward + Vector3.up * 0.8f;
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        damage = attackerBody.damage * damageCoefficient,
                        crit = damageInfo.crit,
                        damageColorIndex = DamageColorIndex.Item,
                        position = attackerBody.corePosition,
                        procChainMask = damageInfo.procChainMask,
                        force = 0,
                        owner = damageInfo.attacker,
                        projectilePrefab = molotovProjectile,
                        rotation = Util.QuaternionSafeLookRotation(fireDirection),
                        speedOverride = 16 //20
                    });
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
            CreateProjectile();
        }

        private void CreateProjectile()
        {
            GameObject molotov = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovSingleProjectile.prefab").WaitForCompletion();
            GameObject dotZone = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovProjectileDotZone.prefab").WaitForCompletion();
            molotovProjectile = molotov.InstantiateClone("BorboMolotovProjectile", true);
            molotovDotZone = dotZone.InstantiateClone("BorboMolotovDotZone", true);

            ProjectileController projectile = molotovProjectile.GetComponent<ProjectileController>();
            if (projectile)
            {
                projectile.procCoefficient = 1;
            }

            ProjectileImpactExplosion pie = molotovProjectile.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.blastRadius = blastRadius;
                pie.blastProcCoefficient = impactProcCoefficient;
                pie.blastDamageCoefficient = 1;

                pie.childrenProjectilePrefab = molotovDotZone;
            }

            molotovDotZone.transform.localScale = Vector3.one * blastRadius / 6;

            ProjectileDotZone pdz = molotovDotZone.GetComponent<ProjectileDotZone>();
            if (pdz)
            {
                pdz.overlapProcCoefficient = dotProcCoefficient;
                pdz.damageCoefficient = dotDamageCoefficient;
                pdz.resetFrequency = dotFrequency;
                pdz.fireFrequency = dotFrequency * 2;
            }

            Assets.projectilePrefabs.Add(molotovProjectile);
            Assets.projectilePrefabs.Add(molotovDotZone);
        }
    }
}
