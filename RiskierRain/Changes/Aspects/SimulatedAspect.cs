using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.CombatDirector;
using static R2API.RecalculateStatsAPI;
using static RiskierRain.CoreModules.EliteModule;
using UnityEngine.AddressableAssets;
using System.Linq;
using RoR2.Projectile;

namespace RiskierRain.Equipment
{
    class SimulatedAspect : EliteEquipmentBase
    {
        public override string EliteEquipmentName => "Abyssal gaze"; //temp name

        public override string EliteAffixToken => "AFFIX_SIMULATED";

        public override string EliteEquipmentPickupDesc => "Become a simulation. lol";

        public override string EliteEquipmentFullDescription => "Some stats up idk, lol";

        public override string EliteEquipmentLore => "";

        public override string EliteModifier => "Simulated";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteLightning/texBuffAffixBlue.png").WaitForCompletion();

        public override Color EliteBuffColor => new Color(0.4f, 0.0f, 0.4f, 1.0f);

        public override EliteTiers EliteTier { get; set; } = EliteTiers.Tier1;


        public override float Cooldown { get; } = 10f;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddSimulatedBehavior;
            GetStatCoefficients += SimulatedStatBuff;
            On.RoR2.CharacterBody.RecalculateStats += SimulatedCooldownBuff;
        }

        private void AddSimulatedBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            self.AddItemBehavior<AffixSimulatedBehavior>(IsElite(self) ? 1 : 0);
        }

        private void SimulatedStatBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (IsElite(sender, EliteBuffDef))
            {
                SetTeamToVoid(sender);
                args.moveSpeedMultAdd += 0.25f;
                args.baseAttackSpeedAdd += 1f;
            }
        }

        private void SimulatedCooldownBuff(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (IsElite(self, EliteBuffDef))
            {
                float scale = 0.85f;
                if (self.skillLocator.primary)
                {
                    self.skillLocator.primary.cooldownScale *= scale;
                }
                if (self.skillLocator.secondary)
                {
                    self.skillLocator.secondary.cooldownScale *= scale;
                }
                if (self.skillLocator.utility)
                {
                    self.skillLocator.utility.cooldownScale *= scale;
                }
                if (self.skillLocator.special)
                {
                    self.skillLocator.special.cooldownScale *= scale;
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CanAppearInEliteTiers = VanillaTier1();

            CreateEliteEquipment();
            CreateLang();
            CreateElite();
            Hooks();
        }

        public void SetTeamToVoid(CharacterBody body)
        {
            if (!body.isPlayerControlled)
            {
                body.teamComponent.teamIndex = TeamIndex.Void;
            }
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            AffixSimulatedBehavior simulatedBehavior = slot.characterBody.GetComponent<AffixSimulatedBehavior>();
            if (simulatedBehavior != null)
            {
                simulatedBehavior.AffixSimulatedAttack();
            }
            return true; 
        }
    }

    class AffixSimulatedBehavior : CharacterBody.ItemBehavior
    {
        public bool isAiming;
        public bool isFiring;
        public float attackStopWatch;
        public int sizeMod;
        public float randomRadius;

        public void AffixSimulatedAttack()
        {

            sizeMod = (int)Mathf.Ceil(body.radius); //projectile count scales with size
            portalBombCount = sizeMod * sizeMod * 2;
            randomRadius = baseRandomRadius * sizeMod / 2;

            isAiming = true;
            attackStopWatch = 0;
            //stolen code from nullifier entity states, god help us
            //if (body.hasAuthority)
            {
                BullseyeSearch bullseyeSearch = new BullseyeSearch();
                bullseyeSearch.viewer = body;
                bullseyeSearch.searchOrigin = body.corePosition;
                bullseyeSearch.searchDirection = body.corePosition;
                bullseyeSearch.maxDistanceFilter = maxDistance;
                bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(body.teamComponent.teamIndex);
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
                bullseyeSearch.RefreshCandidates();
                this.target = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
                if (this.target)
                {
                    //this.pointA = this.RaycastToFloor(this.target.transform.position);
                    this.pointA = this.target.transform.position;
                }
            }
        }

        public void FixedUpdate()
        {
            //if (body.hasAuthority)
            {
                if (isAiming)
                {

                    if (this.target)
                    {

                        //this.pointB = this.RaycastToFloor(this.target.transform.position);
                        this.pointB = this.target.transform.position;

                        if (this.pointA != null && this.pointB != null)
                        {

                            Ray aimRay = GetAimRay();
                            Vector3 forward = this.pointA.Value - aimRay.origin;
                            Vector3 forward2 = this.pointB.Value - aimRay.origin;
                            Quaternion a = Quaternion.LookRotation(forward);
                            Quaternion quaternion = Quaternion.LookRotation(forward2);
                            Quaternion value = quaternion;
                            Quaternion value2 = Quaternion.SlerpUnclamped(a, quaternion, 1f + arcMultiplier);

                            startRotation = new Quaternion?(value);
                            endRotation = new Quaternion?(value2);
                            FirePortalBomb();
                            //entityState = new FirePortalBomb
                            //{
                            //    startRotation = new Quaternion?(value),
                            //    endRotation = new Quaternion?(value2)
                            //};
                        }
                    }
                }
                if (isFiring)
                {
                    this.fireTimer -= Time.fixedDeltaTime;
                    attackStopWatch += Time.fixedDeltaTime;
                    if (this.fireTimer <= 0f)
                    {
                        this.fireTimer += this.fireInterval;
                        if (this.startRotation != null && this.endRotation != null)
                        {
                            float num = 1f / ((float)portalBombCount - 1f);
                            float t = (float)this.bombsFired * num;
                            Ray aimRay = GetAimRay();
                            Quaternion rotation = Quaternion.Slerp(this.startRotation.Value, this.endRotation.Value, t);
                            aimRay.direction = rotation * Vector3.forward;
                            float bonusPitch = UnityEngine.Random.Range(-randomRadius, randomRadius / 4f) / 2F;
                            float bonusYaw = UnityEngine.Random.Range(-randomRadius, randomRadius);
                            this.FireBomb(aimRay, bonusPitch, bonusYaw);
                            EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, gameObject, muzzleString, true);
                        }
                        this.bombsFired++;
                    }
                    if (attackStopWatch >= this.attackDuration)
                    {
                        isFiring = false;
                    }
                }
            }
        }
        
        private void FirePortalBomb()
        {
            isAiming = false;
            isFiring = true;
            this.attackDuration = attackBaseDuration / body.attackSpeed;
            StartAimMode(GetAimRay(), 4f, false);
            //if (body.hasAuthority)
            {
                this.fireInterval = this.attackDuration / (float)portalBombCount;
                this.fireTimer = 0f;
            }
        }

        private void FireBomb(Ray fireRay, float pitch, float yaw)
        {
            //RaycastHit raycastHit;
            //if (Physics.Raycast(fireRay, out raycastHit, maxDistance, LayerIndex.world.mask))
            {

                Vector3 vector = Util.ApplySpread((Vector3)pointA, 0f, 0f, 1f, 1f, yaw, pitch);//raycastHit.point;
                Vector3 vector2 = vector - this.lastBombPosition;
                if (this.bombsFired > 0 && vector2.sqrMagnitude < minimumDistanceBetweenBombs * minimumDistanceBetweenBombs)
                {
                    vector += vector2.normalized * minimumDistanceBetweenBombs;
                }
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = portalBombProjectileEffect;
                fireProjectileInfo.position = vector;
                fireProjectileInfo.rotation = Quaternion.identity;
                fireProjectileInfo.owner = gameObject;
                fireProjectileInfo.damage = body.damage * damageCoefficient;
                fireProjectileInfo.force = force;
                fireProjectileInfo.crit = body.RollCrit();
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                this.lastBombPosition = vector;
            }
        }
        
        protected Ray GetAimRay()
        {
            if (body.inputBank)
            {
                return new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            }
            return new Ray(base.transform.position, base.transform.forward);
        }

        protected void StartAimMode(Ray aimRay, float duration = 2f, bool snap = false)
        {
            if (body.characterDirection && aimRay.direction != Vector3.zero)
            {
                if (snap)
                {
                    body.characterDirection.forward = aimRay.direction;
                }
                else
                {
                    body.characterDirection.moveVector = aimRay.direction;
                }
            }
            if (body)
            {
                body.SetAimTimer(duration);
            }
            if (body.modelLocator)
            {
                Transform modelTransform = body.modelLocator.modelTransform;
                if (modelTransform)
                {
                    AimAnimator component = modelTransform.GetComponent<AimAnimator>();
                    if (component && snap)
                    {
                        component.AimImmediate();
                    }
                }
            }
        }

        private HurtBox target;
        public static float aimBaseDuration = EntityStates.NullifierMonster.AimPortalBomb.baseDuration;
        public static float arcMultiplier = EntityStates.NullifierMonster.AimPortalBomb.arcMultiplier;
        private float aimDuration;
        private Vector3? pointA;
        private Vector3? pointB;

        public static GameObject portalBombProjectileEffect = EntityStates.NullifierMonster.FirePortalBomb.portalBombProjectileEffect;
        public static GameObject muzzleflashEffectPrefab = EntityStates.NullifierMonster.FirePortalBomb.muzzleflashEffectPrefab;
        public static string muzzleString = EntityStates.NullifierMonster.FirePortalBomb.muzzleString;
        public int portalBombCount;
        public static float attackBaseDuration = 4;//EntityStates.NullifierMonster.FirePortalBomb.baseDuration;
        public static float maxDistance = EntityStates.NullifierMonster.FirePortalBomb.maxDistance;
        public static float damageCoefficient = EntityStates.NullifierMonster.FirePortalBomb.damageCoefficient;
        public static float procCoefficient = EntityStates.NullifierMonster.FirePortalBomb.procCoefficient;
        public static float baseRandomRadius = 5;//EntityStates.NullifierMonster.FirePortalBomb.randomRadius;
        public static float force = EntityStates.NullifierMonster.FirePortalBomb.force;
        public static float minimumDistanceBetweenBombs = 5;//EntityStates.NullifierMonster.FirePortalBomb.minimumDistanceBetweenBombs;
        public Quaternion? startRotation;
        public Quaternion? endRotation;
        private float attackDuration;
        private int bombsFired;
        private float fireTimer;
        private float fireInterval;
        private Vector3 lastBombPosition;

    }
}
    


