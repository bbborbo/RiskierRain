using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRainContent.CoreModules.StatHooks;

namespace RiskierRainContent.Items
{
    class Molotov : ItemBase<Molotov>
    {
        public static float molotovEquipmentDamage = 2.5f;
        public static float molotovEquipmentDotDamage = 0.4f;
        public static float molotovEquipmentDotFrequency = 3;
        public static GameObject molotovProjectile;
        public static GameObject molotovDotZone;

        public static float procChance = 9;
        public static float baseDamageCoefficient = 0.6f;
        public static float stackDamageCoefficient = 0.6f;
        public static float impactProcCoefficient = 0.5f;
        public static float dotDamageCoefficient = 0.25f; //0.4f
        public static float dotFrequency = 2f; //3f
        public static float dotProcCoefficient = 0.33f;
        public static float blastRadius = 12;//7f
        public static float napalmDuration = 5;//7f

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
        public IEnumerator GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.Missile);
            yield break;
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

                    Vector3 fireDirection = forward + Vector3.up * 0.5f;
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
                        speedOverride = 20 //20
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

            Debug.LogWarning("Still need to replace the molotov equipment icon");
            LanguageAPI.Add("EQUIPMENT_MOLOTOV_DESC", 
                $"Throw <style=cIsDamage>6</style> molotov cocktails that <style=cIsDamage>ignite</style> enemies for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(molotovEquipmentDamage)} base damage</style>. " +
                $"Each molotov leaves a burning area for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(molotovEquipmentDamage * molotovEquipmentDotDamage * molotovEquipmentDotFrequency)} damage per second</style>.");
        }

        private void CreateProjectile()
        {
            GameObject molotov = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovSingleProjectile.prefab").WaitForCompletion();
            GameObject dotZone = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovProjectileDotZone.prefab").WaitForCompletion();

            #region all molotov
            ProjectileController projectile = molotov.GetComponent<ProjectileController>();
            if (projectile)
            {
                projectile.procCoefficient = 1;
            }

            ProjectileImpactExplosion pie = molotov.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.blastRadius = blastRadius;
                pie.blastDamageCoefficient = molotovEquipmentDamage;
                pie.childrenProjectilePrefab = dotZone;
            }

            dotZone.transform.localScale = Vector3.one * blastRadius / 6;

            ProjectileDotZone pdz = dotZone.GetComponent<ProjectileDotZone>();
            if (pdz)
            {
                pdz.damageCoefficient = molotovEquipmentDotDamage;
                pdz.resetFrequency = molotovEquipmentDotFrequency;
                pdz.fireFrequency = molotovEquipmentDotFrequency * 2;
                pdz.lifetime = napalmDuration;
            }
            #endregion

            molotovProjectile = molotov.InstantiateClone("BorboMolotovProjectile", true);
            molotovDotZone = dotZone.InstantiateClone("BorboMolotovDotZone", true);

            ProjectileImpactExplosion pie2 = molotovProjectile.GetComponent<ProjectileImpactExplosion>();
            if (pie2)
            {
                pie2.blastDamageCoefficient = 1;
                pie2.blastProcCoefficient = impactProcCoefficient;
                pie2.childrenProjectilePrefab = molotovDotZone;
            }

            ProjectileDotZone pdz2 = molotovDotZone.GetComponent<ProjectileDotZone>();
            if (pdz2)
            {
                pdz2.overlapProcCoefficient = dotProcCoefficient;
                pdz2.damageCoefficient = dotDamageCoefficient;
                pdz2.resetFrequency = dotFrequency;
                pdz2.fireFrequency = dotFrequency * 2;
            }

            CoreModules.Assets.projectilePrefabs.Add(molotovProjectile);
            CoreModules.Assets.projectilePrefabs.Add(molotovDotZone);
        }
    }
}
