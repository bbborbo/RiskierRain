using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HarmonyLib;
using EntityStates.Bandit2;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using UnityEngine.Networking;
using RoR2.ExpansionManagement;
using JumpRework;
using static MoreStats.StatHooks;
using static MoreStats.JumpAPI;
using SwanSongExtended.Modules;
using RoR2.Items;
using MoreStats;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class BottleFart : ItemBase<BottleFart>
    {
        public override int loadOrder => 1;
        public override bool GetPrerequisites()
        {
            return BottleCloud.GetBottleCloudConfig();
        }
        public override string ConfigName => "Items : Fart In A Jar";
        static GameObject fartZone;
        static GameObject novaEffectPrefab = null;// LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef fartReadyBuff;
        public static BuffDef fartNotReadyBuff;
        public static bool fartReadyBuffHidden = false;
        public static bool fartNotReadyBuffHidden = false;
        internal static float smokeBombRadius = 9f;
        internal static float smokeBombRadiusStack = 0f;
        static float fartBaseDamageCoefficient = 1f;
        static float fartStackDamageCoefficient = 1f;
        static float fartZoneProcCoefficient => (1 / fartZoneResetFrequency) / (3); //3 is the base duration of cripple proc, this makes it the minimum proc coefficient for constant cripple
        static float fartZoneDuration = 4f; //7
        static float fartZoneResetFrequency = 3f;
        static int fartCooldown = 4;

        public static float verticalBonusOnFartJump = 0.15f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Quarantined Contaminant";//"Sealed Pestilence";

        public override string ItemLangTokenName => "FARTBOTTLE";

        public override string ItemPickupDesc => "Double jumping near enemies cripples and damages them. " +
            "<style=cIsVoid>Corrupts all Cloud In A Bottles.</style>";

        public override string ItemFullDescription => $"Gain an air jump charge. While ready, " +
            $"air jumping within <style=cIsUtility>{smokeBombRadius}m</style> of an enemy " +
            $"produces a <style=cIsDamage>toxic gas</style>, dealing " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(fartBaseDamageCoefficient)}</style> base damage " +
            $"<style=cStack>(+{Tools.ConvertDecimal(fartStackDamageCoefficient)} per stack)</style> per second " +
            $"and Crippling enemies within. " +
            $"Cannot be reactivated for <style=cIsUtility>{fartCooldown}</style>s. " +
            $"<style=cIsVoid>Corrupts all Cloud In A Bottles.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => LoadDropPrefab("mdlBottleFart");

        public override Sprite ItemIcon => LoadItemIcon("texIconBottleFart");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            fartReadyBuff = Content.CreateAndAddBuff(
                "bdCloudReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.magenta, false, false);
            fartNotReadyBuff = Content.CreateAndAddBuff(
                "bdCloudNotReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.grey, true, true);
            fartReadyBuff.isHidden = fartReadyBuffHidden;
            fartNotReadyBuff.isHidden = fartNotReadyBuffHidden;
            CreateProjectile();
            base.Init();
        }
        public override void PostInit()
        {
            base.PostInit();
            AddVoidItemRelationship(BottleCloud.instance.ItemsDef);
        }
        public override void Hooks()
        {
            OnConditionalJumpUrgent += FartOnNearby;
        }

        private bool FartOnNearby(CharacterMotor sender, bool jumpIgnoredRequirements)
        {
            CharacterBody body = sender.body;
            if (!GetCloudReady(body))
                return false;

            float radius = GetFartRadius(body);
            List<HurtBox> hurtboxBuffer = BottleCloud.GetEnemiesWithinRadius(body.teamComponent.teamIndex, radius, body.corePosition);
            if (hurtboxBuffer.Count <= 0)
                return false;

            JumpAPI.SetJumpPowerForCurrentJump(vBonus: BottleCloud.verticalBonusOnCloudJump);

            SetCloudCooldown(body, fartCooldown);

            CreateFartCloud(body, hurtboxBuffer);

            return true;
        }

        private static float GetFartRadius(CharacterBody body)
        {
            return smokeBombRadius;
            int stack = body.inventory.GetItemCountEffective(BottleFart.instance.ItemsDef);

            return smokeBombRadius + smokeBombRadiusStack * (stack - 1);
        }
        private static bool GetCloudReady(CharacterBody body)
        {
            if (body.inventory == null || body.inventory.GetItemCountEffective(BottleFart.instance.ItemsDef) <= 0)
                return false;
            return body.HasBuff(fartReadyBuff);
        }

        private static void SetCloudCooldown(CharacterBody body, int duration)
        {
            if (!NetworkServer.active)
                return;

            if (body.HasBuff(fartReadyBuff))
                body.RemoveBuff(fartReadyBuff);
            if (body.HasBuff(fartNotReadyBuff))
                body.ClearTimedBuffs(fartNotReadyBuff.buffIndex);

            for (int i = 1; i <= duration; i++)
            {
                body.AddTimedBuff(fartNotReadyBuff.buffIndex, i);
            }
        }

        private void CreateProjectile()
        {
            GameObject mushroomGas = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/MiniMushroom/SporeGrenadeProjectileDotZone.prefab").WaitForCompletion();
            if (mushroomGas == null)
                return;

            fartZone = mushroomGas.InstantiateClone("FartJarGas", true);

            fartZone.transform.localScale = Vector3.one * smokeBombRadius / 6;

            ProjectileDotZone pdz = fartZone.GetComponent<ProjectileDotZone>();
            if (pdz)
            {
                pdz.resetFrequency = fartZoneResetFrequency;
                pdz.damageCoefficient = 1 / fartZoneResetFrequency;
                
                pdz.overlapProcCoefficient = fartZoneProcCoefficient; 
                pdz.lifetime = fartZoneDuration;
            }

            ProjectileDamage dmg = fartZone.GetComponent<ProjectileDamage>();
            if(dmg != null)
                dmg.damageType = DamageType.CrippleOnHit;
        }


        internal static void CreateFartCloud(CharacterBody attacker, List<HurtBox> victims)
        {
            if (fartZone == null)
                return;
            int stack = BottleFart.instance.GetCount(attacker, false);

            float fartDamage = attacker.damage * GetStackValue(fartBaseDamageCoefficient, fartStackDamageCoefficient, stack);
            bool fartCrit = Util.CheckRoll(attacker.crit, attacker.master);
            for (int i = 0; i < victims.Count; i++)
            {
                HurtBox hurtBox = victims[i];
                CharacterBody vBody = hurtBox.healthComponent?.body;
                if (vBody)
                {

                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = fartDamage;
                    damageInfo.procCoefficient = fartZoneProcCoefficient;
                    damageInfo.attacker = attacker.gameObject;
                    damageInfo.crit = fartCrit;
                    damageInfo.damageType = new DamageTypeCombo(DamageType.CrippleOnHit, DamageTypeExtended.Generic, DamageSource.NoneSpecified);

                    vBody.healthComponent.TakeDamage(damageInfo);
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, vBody.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, vBody.gameObject);
                }
            }
            victims.Clear();

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo();
            fireProjectileInfo.owner = attacker.gameObject;
            fireProjectileInfo.crit = fartCrit;
            fireProjectileInfo.position = attacker.transform.position - (Vector3.down * attacker.radius);
            fireProjectileInfo.projectilePrefab = fartZone;
            fireProjectileInfo.damage = fartDamage;

            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }
    }

    public class FartBottleBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => BottleFart.instance.ItemsDef;

        void OnEnable()
        {
            this.body.AddBuff(BottleFart.fartReadyBuff);
        }
        void OnDisable()
        {
            if (body.HasBuff(BottleFart.fartNotReadyBuff))
            {
                body.ClearTimedBuffs(BottleFart.fartNotReadyBuff);
            }
            if (body.HasBuff(BottleFart.fartReadyBuff))
            {
                body.RemoveBuff(BottleFart.fartReadyBuff);
            }
        }
        private void FixedUpdate()
        {
            bool isBuffed = this.body.HasBuff(BottleFart.fartReadyBuff);
            bool isDebuffed = this.body.HasBuff(BottleFart.fartNotReadyBuff);
            bool isNeither = !isBuffed && !isDebuffed;
            if (isNeither)
            {
                this.body.AddBuff(BottleFart.fartReadyBuff);
            }
            bool isBoth = isBuffed && isDebuffed;
            if (isBoth)
            {
                this.body.RemoveBuff(BottleFart.fartNotReadyBuff);
            }
        }
    }
}
