using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using SwanSongExtended.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static MoreStats.OnHit;

namespace SwanSongExtended.Items
{
    class Molotov : ItemBase<Molotov>
    {
        #region config
        public override string ConfigName => "Reworks : Molotovs";

        [AutoConfig("Disable Molotov Equipment", false)]
        public static bool disableMolotovEquipment = false;

        [AutoConfig("Molotov Equipment Damage Coefficient", 2.5f)]
        public static float molotovEquipmentDamage = 2.5f;
        [AutoConfig("Molotov Equipment Damage Multiplier Per Second", 0.4f)]
        public static float molotovEquipmentDotDamage = 0.4f;
        [AutoConfig("Molotov Equipment Damage Frequency", 0.4f)]
        public static float molotovEquipmentDotFrequency = 3;

        [AutoConfig("Molotov Item Proc Chance", 9)]
        public static float procChance = 9;
        [AutoConfig("Molotov Item Base Damage Coefficient", 0.6f)]
        public static float baseDamageCoefficient = 0.6f;
        [AutoConfig("Molotov Item Stacking Damage Coefficient", 0.6f)]
        public static float stackDamageCoefficient = 0.6f;
        [AutoConfig("Molotov Item Impact Proc Coefficient", 0.5f)]
        public static float impactProcCoefficient = 0.5f;
        [AutoConfig("Molotov Item Damage Multiplier Per Second", 0.25f)]
        public static float dotDamageCoefficient = 0.25f; //0.4f
        [AutoConfig("Molotov Item Damage Frequency", 2f)]
        public static float dotFrequency = 2f; //3f
        [AutoConfig("Molotov Item Proc Coefficient Per Tick", 0.33f)]
        public static float dotProcCoefficient = 0.33f;
        [AutoConfig("Molotov Item Blast Radius", 12)]
        public static float blastRadius = 12;//7f
        [AutoConfig("Molotov Item Napalm Duration", 5)]
        public static float napalmDuration = 5;//7f
        #endregion

        public static GameObject molotovProjectile;
        public static GameObject molotovDotZone;
        public override AssetBundle assetBundle => null;
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

        public override void Init()
        {
            base.Init();
            CreateProjectile();

            Debug.LogWarning("Still need to replace the molotov equipment icon");
            EquipmentDef equipDef = Addressables.LoadAssetAsync<EquipmentDef>("RoR2/DLC1/Molotov/Molotov.asset").WaitForCompletion();
            if (disableMolotovEquipment)
            {
                equipDef.canDrop = false;
                equipDef.enigmaCompatible = false;
                equipDef.canBeRandomlyTriggered = false;
            }
            else
            {
                LanguageAPI.Add("EQUIPMENT_MOLOTOV_DESC",
                    $"Throw <style=cIsDamage>6</style> molotov cocktails that <style=cIsDamage>ignite</style> enemies for " +
                    $"<style=cIsDamage>{Tools.ConvertDecimal(molotovEquipmentDamage)} base damage</style>. " +
                    $"Each molotov leaves a burning area for " +
                    $"<style=cIsDamage>{Tools.ConvertDecimal(molotovEquipmentDamage * molotovEquipmentDotDamage * molotovEquipmentDotFrequency)} damage per second</style>.");
            }
        }
        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Equipment.Molotov);
            GetHitBehavior += MolotovOnHit;
        }

        private void MolotovOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victim)
        {
            int itemCount = GetCount(attackerBody);
            if(itemCount > 0 && (damageInfo.damageType.IsDamageSourceSkillBased || damageInfo.damageType.damageSource == DamageSource.Equipment))
            {
                if(Util.CheckRoll(procChance * damageInfo.procCoefficient, attackerBody.master))
                {
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

            Content.AddProjectilePrefab(molotovProjectile);
            Content.AddProjectilePrefab(molotovDotZone);
        }
    }
}
