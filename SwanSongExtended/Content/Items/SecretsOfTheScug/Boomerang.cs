using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SwanSongExtended.Items
{
    class Boomerang : ItemBase<Boomerang>
    {
        #region config
        public override string ConfigName => "Boomerang";

        public static GameObject boomerangPrefab;
        [AutoConfig("Max Boomerang Fly-Out Time", 0.3f)]
        public static float maxFlyOutTime = 0.3f; //0.6f
        [AutoConfig("Boomerang Scale Factor", 0.3f)]
        public static float boomerangScale = 0.3f; //1.0f
        [AutoConfig("Boomerang Speed", 60f)]
        public static float boomerangSpeed = 60f; //1.0f

        [AutoConfig("Damage Coefficient", 2f)]
        public static float damageCoefficient = 2f;
        [AutoConfig("Proc Coefficient", 0.8f)]
        public static float procCoefficient = 0.8f;
        [AutoConfig("Force", 150f)]
        public static float force = 150f;
        #endregion
        public static BuffDef boomerangBuff;
        #region abstract
        public override string ItemName => "Boomerang";

        public override string ItemLangTokenName => "SNAILY_BOOMERANG";

        public override string ItemPickupDesc => "";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage};

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSOTS;
        #endregion

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnSkillActivated += BoomerangOnSkill;
            boomerangBuff = Content.CreateAndAddBuff("BoomerangBuff", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), Color.cyan, true, false);
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    BoomerangBehavior boomerangBehavior = self.AddItemBehavior<BoomerangBehavior>(GetCount(self));
                }
            }
        }
        private void BoomerangOnSkill(On.RoR2.CharacterBody.orig_OnSkillActivated orig, RoR2.CharacterBody self, RoR2.GenericSkill skill)
        {
            orig (self, skill);          
            if (!(skill == self.skillLocator.primary))
            {
                return;
            }
            int boomerangCount = GetCount(self);
            if (boomerangCount <= 0)
            {
                return;
            }
            if (self.GetBuffCount(boomerangBuff) >= 1)
            {
                FireBoomerang(self);
            }
        }

        private void FireBoomerang(CharacterBody body)
        {
            if (body.GetBuffCount(boomerangBuff) > 0)
            {
                body.RemoveBuff(boomerangBuff);
            }           

            Vector3 forward = body.inputBank.aimDirection;
          

            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                damage = body.damage * damageCoefficient,
                crit = body.RollCrit(),
                damageColorIndex = DamageColorIndex.Item,
                position = body.corePosition/* + Vector3.up * 3*/,
                force = force,
                owner = body.gameObject,
                projectilePrefab = boomerangPrefab,
                rotation = Util.QuaternionSafeLookRotation(forward),
                speedOverride = boomerangSpeed //20
            });
        }

        public override void Init()
        {
            CreateProjectile();
            base.Init();
        }
        private void CreateProjectile()
        {
            boomerangPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Sawmerang").InstantiateClone("SnailyBoomerang", true);
            GameObject ghost = LegacyResourcesAPI.Load<GameObject>("prefabs/projectileghosts/WindbladeProjectileGhost").InstantiateClone("SnailyBoomerangGhost", false);//if this doesnt work and you have to do it the other way:RoR2/Base/Vulture/WindbladeProjectileGhost.prefab
            boomerangPrefab.transform.localScale = Vector3.one * boomerangScale;

            BoomerangProjectile bp = boomerangPrefab.GetComponent<BoomerangProjectile>();
            bp.travelSpeed = boomerangSpeed;
            bp.transitionDuration = 0.8f;
            bp.distanceMultiplier = maxFlyOutTime;
            bp.canHitWorld = false;

            ProjectileController pc = bp.GetComponent<ProjectileController>();
            pc.ghostPrefab = ghost;

            //ProjectileDamage pd = bp.GetComponent<ProjectileDamage>();
            //pd.damageType |= DamageType.BonusToLowHealth;

            ProjectileDotZone pdz = boomerangPrefab.GetComponent<ProjectileDotZone>();
            /*pdz.overlapProcCoefficient = 0.8f;
            pdz.damageCoefficient = 1f;
            pdz.resetFrequency = 1 / (maxFlyOutTime + bp.transitionDuration);
            pdz.fireFrequency = 20f;*/
            UnityEngine.Object.Destroy(pdz);

            ProjectileOverlapAttack poa = boomerangPrefab.GetComponent<ProjectileOverlapAttack>();
            poa.damageCoefficient = 1f;
            poa.overlapProcCoefficient = procCoefficient;

            Content.AddProjectilePrefab(boomerangPrefab);
        }
    }
    public class BoomerangBehavior : CharacterBody.ItemBehavior
    {
        public static float cooldownDuration = 4;
        float cooldownTimer = 0;
        private void FixedUpdate()
        {
            Debug.Log(cooldownTimer);
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }
            if (cooldownTimer <= 0)//make this not hardcoded
            {
                Debug.Log("boomb");
                RechargeBuff();
            }
        }
        private void RechargeBuff()
        {
            if(body.GetBuffCount(Boomerang.boomerangBuff) <= 3 + stack)
            {
                body.AddBuff(Boomerang.boomerangBuff);
            }
            cooldownTimer = cooldownDuration;
        }
    }
}
