using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ModularEclipse;
using RoR2.ExpansionManagement;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using EntityStates.MiniMushroom;
using System.Security.Permissions;
using System.Security;
using System.Linq;
using RoR2.Projectile;
using On.EntityStates.VoidSurvivor.Weapon;
using EntityStates.Captain.Weapon;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ModularEclipsePlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI))]
    public partial class MissileReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }

        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "MissileRework";
        public const string version = "1.0.0";
        #endregion

        ArtifactDef MissileArtifact = ScriptableObject.CreateInstance<ArtifactDef>();
        ItemDef icbmItemDef;

        public const float missileSpread = 45;
        public const float projectileSpread = 25;

        public void Awake()
        {
            CreateArtifact();
            ModularEclipsePlugin.SetArtifactDefaultWhitelist(MissileArtifact, true);

            DoMissileArtifactEffects();

            DisableICBM();
        }

        private void DoMissileArtifactEffects()
        {
            On.RoR2.Inventory.GetItemCount_ItemIndex += OverrideItemCount;
            On.EntityStates.GenericProjectileBaseState.FireProjectile += MissileArtifact_FireProjectile;

            //viend m2
            On.EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase.FireProjectiles += MissileArtifact_ViendSecondary;
            //captain tazer
            On.EntityStates.Captain.Weapon.FireTazer.Fire += MissileArtifact_CaptainTazer;

            //mushrum spore grenade
            On.EntityStates.MiniMushroom.SporeGrenade.FireGrenade += MissileArtifact_MushrumSporeGrenade;
        }

        private void MissileArtifact_CaptainTazer(On.EntityStates.Captain.Weapon.FireTazer.orig_Fire orig, EntityStates.Captain.Weapon.FireTazer self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = FireTazer.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * FireTazer.damageCoefficient;
                fireProjectileInfo.force = FireTazer.force;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_ViendSecondary(On.EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase.orig_FireProjectiles orig, EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, self.spread, 1f, 1f, 0f, 0f);
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * self.damageCoefficient;
                fireProjectileInfo.force = self.force;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_MushrumSporeGrenade(On.EntityStates.MiniMushroom.SporeGrenade.orig_FireGrenade orig, EntityStates.MiniMushroom.SporeGrenade self, string targetMuzzle)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                orig(self, targetMuzzle);
                return;
            }
            Ray aimRay = self.GetAimRay();
            Ray ray = new Ray(aimRay.origin, Vector3.up);
            Transform transform = self.FindModelChild(targetMuzzle);
            if (transform)
            {
                ray.origin = transform.position;
            }
            BullseyeSearch bullseyeSearch = new BullseyeSearch();
            bullseyeSearch.searchOrigin = aimRay.origin;
            bullseyeSearch.searchDirection = aimRay.direction;
            bullseyeSearch.filterByLoS = false;
            bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
            if (self.teamComponent)
            {
                bullseyeSearch.teamMaskFilter.RemoveTeam(self.teamComponent.teamIndex);
            }
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
            bullseyeSearch.RefreshCandidates();
            HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
            bool flag = false;
            Vector3 a = Vector3.zero;
            RaycastHit raycastHit;
            if (hurtBox)
            {
                a = hurtBox.transform.position;
                flag = true;
            }
            else if (Physics.Raycast(aimRay, out raycastHit, 1000f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
            {
                a = raycastHit.point;
                flag = true;
            }
            float magnitude = SporeGrenade.projectileVelocity;
            if (flag)
            {
                Vector3 vector = a - ray.origin;
                Vector2 a2 = new Vector2(vector.x, vector.z);
                float magnitude2 = a2.magnitude;
                Vector2 vector2 = a2 / magnitude2;
                if (magnitude2 < SporeGrenade.minimumDistance)
                {
                    magnitude2 = SporeGrenade.minimumDistance;
                }
                if (magnitude2 > SporeGrenade.maximumDistance)
                {
                    magnitude2 = SporeGrenade.maximumDistance;
                }
                float y = Trajectory.CalculateInitialYSpeed(SporeGrenade.timeToTarget, vector.y);
                float num = magnitude2 / SporeGrenade.timeToTarget;
                Vector3 direction = new Vector3(vector2.x * num, y, vector2.y * num);
                magnitude = direction.magnitude;
                ray.direction = direction;
            }
            Quaternion rotation = Util.QuaternionSafeLookRotation(ray.direction + UnityEngine.Random.insideUnitSphere * 0.05f);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = SporeGrenade.projectilePrefab,
                position = ray.origin,
                rotation = rotation,
                owner = self.gameObject,
                damage = self.damageStat * SporeGrenade.damageCoefficient,
                crit = Util.CheckRoll(self.critStat, self.characterBody.master),
                force = 0,
                speedOverride = magnitude
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

            Vector3 axis = self.inputBank ? self.inputBank.aimDirection : self.characterBody.transform.position;
            FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
            fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * (ray.direction + UnityEngine.Random.insideUnitSphere * 0.05f));
            ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

            FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
            fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * (ray.direction + UnityEngine.Random.insideUnitSphere * 0.05f));
            ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
        }

        private void MissileArtifact_FireProjectile(On.EntityStates.GenericProjectileBaseState.orig_FireProjectile orig, EntityStates.GenericProjectileBaseState self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                if (self.isAuthority)
                {
                    //alpha construct, blind pest
                    if (self is EntityStates.MinorConstruct.Weapon.FireConstructBeam || self is EntityStates.FlyingVermin.Weapon.Spit)
                    {
                        Ray aimRay = self.GetAimRay();
                        aimRay = self.ModifyProjectileAimRay(aimRay);
                        aimRay.direction = Util.ApplySpread(aimRay.direction, self.minSpread, self.maxSpread, 1f, 1f, 0f, self.projectilePitchBonus);

                        FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                        {
                            projectilePrefab = self.projectilePrefab,
                            position = aimRay.origin,
                            rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                            owner = self.gameObject,
                            damage = self.damageStat * self.damageCoefficient,
                            crit = Util.CheckRoll(self.critStat, self.characterBody.master),
                            force = self.force
                        };
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                        Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                        Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                        FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                        fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

                        FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                        fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
                        return;
                    }
                }
            }
            orig(self);
        }

        private void DisableICBM()
        {
            icbmItemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/MoreMissile/MoreMissile.asset").WaitForCompletion();
            if (icbmItemDef != null)
            {
                icbmItemDef.tier = ItemTier.NoTier;
                //icbmItemDef.deprecatedTier = ItemTier.NoTier;
            }
        }

        private void CreateArtifact()
        {
            MissileArtifact.cachedName = "BorboWarfare";
            MissileArtifact.nameToken = "ARTIFACT_MISSILE_NAME";
            MissileArtifact.descriptionToken = "ARTIFACT_MISSILE_DESC";
            MissileArtifact.smallIconDeselectedSprite = LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
            MissileArtifact.smallIconSelectedSprite = LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
            MissileArtifact.unlockableDef = null;
            MissileArtifact.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

            LanguageAPI.Add(MissileArtifact.nameToken, "Artifact of Warfare");
            LanguageAPI.Add(MissileArtifact.descriptionToken, "Triple ALL missile effects.");
            ContentAddition.AddArtifactDef(MissileArtifact);
        }

        private int OverrideItemCount(On.RoR2.Inventory.orig_GetItemCount_ItemIndex orig, Inventory self, ItemIndex itemIndex)
        {
            if (itemIndex == DLC1Content.Items.MoreMissile.itemIndex)
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact.artifactIndex))
                    return 1;
                return 0;
            }
            return orig(self, itemIndex);
        }
    }
}