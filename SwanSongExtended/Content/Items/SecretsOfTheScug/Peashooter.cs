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
    class Peashooter : ItemBase<Peashooter>
    {
        #region config
        public override string ConfigName => "Peashooter";

        public static GameObject peashooterPrefab;
        [AutoConfig("Peashooter Lifetime", 5f)]
        public static float lifetime = 5f; //0.6f
        [AutoConfig("Peashooter Scale Factor", 0.3f)]
        public static float peashooterScale = 0.3f; //1.0f
        [AutoConfig("Peashooter Speed", 100f)]
        public static float peashooterSpeed = 100f; //1.0f

        [AutoConfig("Damage Coefficient", .2f)]
        public static float damageCoefficient = .2f;
        [AutoConfig("Proc Coefficient", 0.1f)]
        public static float procCoefficient = 0.1f;
        [AutoConfig("Force", 0f)]
        public static float force = 0f;
        #endregion
        #region abstract
        public override string ItemName => "Peashooter";

        public override string ItemLangTokenName => "SNAILY_PEASHOOTER";

        public override string ItemPickupDesc => "";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

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
        }

        public static void FirePeashooter(CharacterBody body)
        {
            if (body == null)
                return;
            Vector3 forward = body.inputBank.aimDirection;
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                damage = body.damage * damageCoefficient,
                crit = body.RollCrit(),
                damageColorIndex = DamageColorIndex.Item,
                position = body.corePosition/* + Vector3.up * 3*/,
                force = force,
                owner = body.gameObject,
                projectilePrefab = peashooterPrefab,
                rotation = Util.QuaternionSafeLookRotation(forward),
                speedOverride = peashooterSpeed, //20
                damageTypeOverride = DamageTypeCombo.GenericPrimary
            });
        }

        public override void Init()
        {
            CreateProjectile();
            base.Init();
        }
        private void CreateProjectile()
        {
            peashooterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarExploder/LunarExploderShardProjectile.prefab").WaitForCompletion().InstantiateClone("SnailyPeashooter", true);
            GameObject ghost = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarExploder/LunarExploderShardGhost.prefab").WaitForCompletion().InstantiateClone("SnailyPeashooterGhost", false);
            peashooterPrefab.transform.localScale = Vector3.one * peashooterScale;

            ProjectileSimple ps = peashooterPrefab.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = peashooterSpeed;
            ps.lifetime = lifetime;

            ProjectileController pc = ps.GetComponent<ProjectileController>();
            pc.ghostPrefab = ghost;

            Content.AddProjectilePrefab(peashooterPrefab);
        }
    }
}
