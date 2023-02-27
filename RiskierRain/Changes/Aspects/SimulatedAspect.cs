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
using MonoMod.Cil;
using Mono.Cecil.Cil;

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

        public override EliteTiers EliteTier { get; set; } = EliteTiers.Other;


        public override float Cooldown { get; } = 6f;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddSimulatedBehavior;
            GetStatCoefficients += SimulatedStatBuff;
            On.RoR2.CharacterBody.RecalculateStats += SimulatedCooldownBuff;
            On.RoR2.GlobalEventManager.OnCharacterDeath += SimulatedSpawn;
            IL.RoR2.CharacterBody.UpdateHurtBoxesEnabled += SimulatedHurtbox;
            IL.RoR2.CharacterModel.UpdateOverlays += SimulatedOverlay;
        }

        private void SimulatedOverlay(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterModel>("set_isGhost")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, CharacterModel, bool>>((isGhost, self) =>
            {
                if (isGhost)
                    return true;

                CharacterBody body = self.body;
                if (self.body && body.HasBuff(EliteBuffDef))
                    return true;

                return false;
            });
        }

        private void SimulatedHurtbox(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            //il hook by borbo thanks bestie  
            // <3 - borbo

            c.GotoNext(MoveType.After,
                x => x.MatchStloc(0));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<CharacterBody, bool>>((body) => 
            {
                if (body.HasBuff(RoR2Content.Buffs.Intangible))
                    return true;

                if (body.HasBuff(EliteBuffDef))
                    return false;

                Inventory inv = body.inventory;
                if (inv && inv.GetItemCount(RoR2Content.Items.Ghost) > 0)
                    return true;

                return false;
            });
            c.Emit(OpCodes.Stloc_0);
        }

       

        private void SimulatedSpawn(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            CharacterBody victimBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;

            if (damageReport.damageInfo.damageType.HasFlag(DamageType.VoidDeath))
            {
                if (!victimBody.isPlayerControlled)
                {
                    if (attackerBody)
                    {
                        int duration = (int) ((damageReport.combinedHealthBeforeDamage / victimBody.healthComponent.fullCombinedHealth)* 50);
                        CharacterBody ghost = Util.TryToCreateGhost(victimBody, attackerBody, duration);
                        CharacterMaster ghostMaster = ghost.master;
                        if (ghostMaster != null)
                        {
                            if (ghostMaster.inventory == null)
                            {
                                Debug.Log("ghost inv null");
                                    
                            }
                            else
                            {
                                ghost.inventory.SetEquipmentIndex(EliteEquipmentDef.equipmentIndex);
                                ghost.inventory.GiveItem(RoR2Content.Items.UseAmbientLevel);
                            }
                        }
                        else
                        {
                            Debug.Log("ghostMaster null");
                        }
                    }
                }
            }
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
                sender.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath;
                sender.bodyFlags |= CharacterBody.BodyFlags.Void;
                Debug.Log(sender.bodyFlags);
            }
        }

        internal static void RemoveAllOfItem(Inventory inv, ItemDef itemDef)
        {
            int damageBoostCount = inv.GetItemCount(itemDef);
            inv.RemoveItem(itemDef, damageBoostCount);
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
            //CanAppearInEliteTiers = VanillaTier1();

            CreateEliteEquipment();
            CreateLang();
            CreateElite();
            Hooks();
        }

        public void SetTeamToVoid(CharacterBody body)
        {
            if (!body.isPlayerControlled)
            {
                TeamComponent teamComponent = body.teamComponent;
                //if (teamComponent.teamIndex != TeamIndex.Player)
                    teamComponent.teamIndex = TeamIndex.Void;
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
        public float volleyDelay = 1;
        public float volleyStopWatch;
        public int sizeMod;
        public float randomRadius;
        public int chargeCount = 0;

        public void Start()
        {
            Inventory inv = body.inventory;
            if (inv)
            {
                SimulatedAspect.RemoveAllOfItem(inv, RoR2Content.Items.BoostDamage);
            }

            CharacterModel[] models = body.GetComponentsInChildren<CharacterModel>();
            foreach(CharacterModel model in models)
            {
                model.UpdateOverlays();
            }
            body.UpdateHurtBoxesEnabled();
        }
        public void AffixSimulatedAttack()
        {
            sizeMod = (int)Mathf.Ceil(body.radius); //projectile count scales with size
            chargeCount = Mathf.Clamp(sizeMod, 5, 30);
            //if (body.hasAuthority)
            {
                PortalBombVolley();
            }
        }
        public void PortalBombVolley()
        {
            
            portalBombCount = sizeMod;
            randomRadius = baseRandomRadius + sizeMod;
            attackStopWatch = 0;
            isAiming = true;
            bombsFired = 0;

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
                this.pointA = this.target.transform.position;
            }
            chargeCount--;
            volleyStopWatch = 0;
        }
        public void FixedUpdate()
        {
            if (barrierBool)
            {
                OneTimeBarrierGain();
            }
            //if (body.hasAuthority)
            {
                if (isAiming)
                {

                    if (this.target)
                    {

                        this.pointB = this.RaycastToFloor(this.target.transform.position);
                        this.pointB = this.RaycastToFloor(this.target.transform.position);

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
                            //Quaternion rotation = Quaternion.Slerp(this.startRotation.Value, this.endRotation.Value, t);
                            //aimRay.direction = rotation * Vector3.forward;
                            float bonusPitch = UnityEngine.Random.Range(0, randomRadius);
                            float bonusYaw = UnityEngine.Random.Range(-randomRadius, randomRadius);
                            this.FireBomb(aimRay, bonusPitch, bonusYaw, t);
                            EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, gameObject, muzzleString, true);
                        }
                        this.bombsFired++;
                    }
                    if (attackStopWatch >= this.attackDuration)
                    {
                        isFiring = false;
                    }
                }
                if (!isFiring && !isAiming && chargeCount > 0)
                {
                    //volleyStopWatch += Time.fixedDeltaTime;
                    //if (volleyStopWatch >= volleyDelay)
                    {
                        Debug.Log(chargeCount);
                        PortalBombVolley();
                    }
                }
            }
        }

        private void OneTimeBarrierGain()
        {
            body.healthComponent.AddBarrier(body.healthComponent.fullCombinedHealth);
            barrierBool = false;
        }

        private void FirePortalBomb()
        {
            isAiming = false;
            isFiring = true;
            this.attackDuration = attackBaseDuration / body.attackSpeed;
            StartAimMode(GetAimRay(), 2, false);
            //if (body.hasAuthority)
            {
                this.fireInterval = this.attackDuration / (float)portalBombCount;
                this.fireTimer = 0f;
            }
        }

        private void FireBomb(Ray fireRay, float pitch, float yaw, float bombsFired)//CHECK OUT FIRERAY IN DNSPY THANKS BESTIE
        {
            RaycastHit raycastHit;
            Vector3 aimDirection;
            if (Physics.Raycast(fireRay, out raycastHit, maxDistance))
            {
                aimDirection = raycastHit.point;
                
            }
            else
            {
                Debug.Log("raycast no hit :/");
                aimDirection = base.transform.position;
            }
            Vector3 vector = Util.ApplySpread(aimDirection, 0f + bombsFired * 2, 3f + bombsFired * 2, 1f, 0.1f, 0, pitch);

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = portalBombProjectileEffect,
                position = vector,
                rotation = Quaternion.identity,
                owner = gameObject,
                damage = body.damage * damageCoefficient,
                force = force,
                crit = body.RollCrit(),
            };
            
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            //this.lastBombPosition = vector;
        }

        protected Ray GetAimRay()
        {
            if (body.inputBank)
            {
                return new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            }
            return new Ray(base.transform.position, base.transform.forward);
        }
        private Vector3? RaycastToFloor(Vector3 position)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(new Ray(position, Vector3.down), out raycastHit, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                return new Vector3?(raycastHit.point);
            }
            return null;
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
        public static float attackBaseDuration = 1;//EntityStates.NullifierMonster.FirePortalBomb.baseDuration;
        public static float maxDistance = EntityStates.NullifierMonster.FirePortalBomb.maxDistance * 4;
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

        public bool barrierBool = true;

    }
    public class SimulatedBallsManager : CharacterBody.ItemBehavior
    {
        public CharacterBody body;
        public GameObject prefab = EntityStates.NullifierMonster.FirePortalBomb.portalBombProjectileEffect;
        private float timer = 0;
        public float interval = 5f;
        public int ballCount;

        private void Start()
        {
            body = GetComponent<CharacterBody>();

                ballCount = 5;

        }

        private void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;
            if (timer >= interval)
            {
                //var playerList = CharacterBody.readOnlyInstancesList.Where(x => x.isPlayerControlled).ToArray();
                //for (int i = 0; i < playerList.Length; i++)
                FindTarget();
                if (target != null)
                {
                    
                    for (int j = 0; j < ballCount; j++)
                    {
                        var fpi = new FireProjectileInfo
                        {
                            projectilePrefab = prefab,
                            damage = Run.instance ? 5f + Mathf.Sqrt(Run.instance.ambientLevel * 200f) : 0f,
                            rotation = Quaternion.identity,
                            owner = gameObject,
                            crit = body.RollCrit(),
                            position = Util.ApplySpread(target.transform.position, 6f * j, 7f * j, 1f, 0.08f)
                        };
                        ProjectileManager.instance.FireProjectile(fpi);
                        timer = 0f;
                    }
                }
            }
        }

        //my code, god help us
        private void FindTarget()
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
        }

        private HurtBox target;
        public static float maxDistance = EntityStates.NullifierMonster.FirePortalBomb.maxDistance * 2;

    }
}
    


