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
    class RainbowWave : ItemBase<RainbowWave>
    {

        #region config
        public override string ConfigName => "Rainbow Wave";

        public static GameObject rainbowWavePrefab;
        [AutoConfig("Max Rainbow Wave Fly-Out Time", 10f)]
        public static float maxFlyOutTime = 010f; //0.6f
        [AutoConfig("Rainbow Wave Scale Factor", 0.3f)]
        public static float rainbowWaveScale = 5f; //1.0f
        [AutoConfig("Rainbow Wave Speed", 100f)]
        public static float rainbowWaveSpeed = 100f; //1.0f

        [AutoConfig("Damage Coefficient", 8f)]
        public static float damageCoefficient = 8f;
        [AutoConfig("Proc Coefficient", 1f)]
        public static float procCoefficient = 1f;
        [AutoConfig("Force", 150f)]
        public static float force = 150f;
        #endregion
        public static BuffDef rainbowBuff;
        #region abstract
        public override string ItemName => "Rainbow Wave";

        public override string ItemLangTokenName => "SNAILY_RAINBOW_WAVE";

        public override string ItemPickupDesc => "";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

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
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    RainbowWaveBehavior rainbowWaveBehavior = self.AddItemBehavior<RainbowWaveBehavior>(GetCount(self));
                }
            }
        }

        public static void FireRainbowWave(CharacterBody body)
        {
            if (body == null || body.GetBuffCount(rainbowBuff) <= 0)
                return;
            body.RemoveBuff(rainbowBuff);

            Vector3 forward = body.inputBank.aimDirection;
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                damage = body.damage * damageCoefficient,
                crit = body.RollCrit(),
                damageColorIndex = DamageColorIndex.Item,
                position = body.corePosition/* + Vector3.up * 3*/,
                force = force,
                owner = body.gameObject,
                projectilePrefab = rainbowWavePrefab,
                rotation = Util.QuaternionSafeLookRotation(forward),
                speedOverride = rainbowWaveSpeed //20
            });
        }

        public override void Init()
        {
            rainbowBuff = Content.CreateAndAddBuff(
                    "RainbowWaveBuff",
                    Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                    Color.red,
                    true,
                    false
                );
            CreateProjectile();
            base.Init();
        }
        private void CreateProjectile()
        {
            rainbowWavePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/FMJRamping.prefab").WaitForCompletion().InstantiateClone("SnailyRainbowWave", true);
            GameObject ghost = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/FMJRampingGhost.prefab").WaitForCompletion().InstantiateClone("SnailyRainbowWaveGhost", false);//if this doesnt work and you have to do it the other way:RoR2/Base/Vulture/WindbladeProjectileGhost.prefab
            rainbowWavePrefab.transform.localScale = Vector3.one * rainbowWaveScale + new Vector3 (0, 1, 1) * 10f;//testig :3
            ghost.transform.localScale = Vector3.one * rainbowWaveScale + new Vector3(0, 1, 1) * 10f;//testig :3

            ProjectileSimple ps = rainbowWavePrefab.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = rainbowWaveSpeed;
            ps.lifetime = maxFlyOutTime;

            ProjectileController pc = ps.GetComponent<ProjectileController>();
            pc.ghostPrefab = ghost;

            //ProjectileDamage pd = bp.GetComponent<ProjectileDamage>();
            //pd.damageType |= DamageType.BonusToLowHealth;

            ProjectileDotZone pdz = rainbowWavePrefab.GetComponent<ProjectileDotZone>();
            /*pdz.overlapProcCoefficient = 0.8f;
            pdz.damageCoefficient = 1f;
            pdz.resetFrequency = 1 / (maxFlyOutTime + bp.transitionDuration);
            pdz.fireFrequency = 20f;*/
            UnityEngine.Object.Destroy(pdz);

            ProjectileOverlapAttack poa = rainbowWavePrefab.GetComponent<ProjectileOverlapAttack>();
            poa.damageCoefficient = 1f;
            poa.overlapProcCoefficient = procCoefficient;

            Content.AddProjectilePrefab(rainbowWavePrefab);
        }
    }
    public class RainbowWaveBehavior : CharacterBody.ItemBehavior
    {
        public static float cooldownDuration = 12;//for testing. increase later
        float cooldownTimer = 0;
        private void FixedUpdate()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }
            if (cooldownTimer <= 0)//make this not hardcoded
            {
                RechargeBuff();
            }
        }
        private void RechargeBuff()
        {
            if (body.GetBuffCount(RainbowWave.rainbowBuff) < stack)
            {
                body.AddBuff(RainbowWave.rainbowBuff);
            }
            cooldownTimer = cooldownDuration;
        }
    }
}
