using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.CombatDirector;
using static R2API.RecalculateStatsAPI;
using static RiskierRainContent.CoreModules.EliteModule;
using UnityEngine.AddressableAssets;
using System.Linq;
using RoR2.Projectile;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.ObjectModel;

namespace RiskierRainContent.Equipment
{
    class SimulatedAspect : EliteEquipmentBase<SimulatedAspect>
    {
        public override string EliteEquipmentName => "Abyssal gaze"; //temp name

        public override string EliteAffixToken => "AFFIX_SIMULATED";

        public override string EliteEquipmentPickupDesc => "Become an aspect of the simulacrum";

        public override string EliteEquipmentFullDescription => "Some stats up idk, lol";

        public override string EliteEquipmentLore => "";

        public override string EliteModifier => "Simulated";
        public override float EliteHealthModifier => 0.7f; //voidtouched 1.5f

        public override float EliteDamageModifier => 2f; //voidtouched 0.7f, t1 1.5f/2f

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteLightning/texBuffAffixBlue.tif").WaitForCompletion();

        public override Color EliteBuffColor => new Color(0.4f, 0.0f, 0.4f, 1.0f);

        public override EliteTiers EliteTier { get; set; } = EliteTiers.Other;
        public override string EliteRampTextureName { get; set; } = "texRampLeeching";


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

        private int maxDuration = 30; // just enough time to get 2 simu attacks, probably
        private int minDuration = 7; // just enough time to get 2 simu attacks, probably

        private void SimulatedSpawn(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            CharacterBody victimBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;

            if (damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.VoidDeath))
            {
                if (!victimBody.isPlayerControlled)
                {
                    if (attackerBody)
                    {
                        if (Util.CheckRoll(50))//chance i tink
                        {
                            return;
                        }
                        int duration = (int) ((damageReport.combinedHealthBeforeDamage / victimBody.healthComponent.fullCombinedHealth) * maxDuration);
                        CharacterBody ghost = Util.TryToCreateGhost(victimBody, attackerBody, Math.Max(duration, minDuration));
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

                if(sender.inventory && sender.inventory.GetItemCount(RoR2Content.Items.HealthDecay) <= 0)
                {
                    args.baseRegenAdd -= 8f;
                }
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

            //ermm
            body.healthComponent.AddBarrier(body.healthComponent.fullCombinedHealth);
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
            
            portalBombCount = sizeMod + minPortalBombs;
            randomRadius = baseRandomRadius + sizeMod;
            attackStopWatch = 0;
            isFiring = true;
            bombsFired = 0;

            attackDuration = attackBaseDuration;
            fireInterval = attackBaseDuration / Mathf.Min(portalBombCount * body.attackSpeed, maxPortalBombs);
            chargeCount--;
            volleyStopWatch = 0;
        }
        public void FixedUpdate()
        {
            if (barrierBool)
            {
                //OneTimeBarrierGain();
            }
            if (isFiring)
            {
                TimerTick();
            }
            
        }
        private void TimerTick()
        {
            fireTimer += Time.fixedDeltaTime;
            if (fireTimer >= fireInterval)
            {
                NewPortalBombAttack();
                fireTimer -= fireInterval;
            }
            attackStopWatch += Time.fixedDeltaTime;
        }
        private void NewPortalBombAttack()
        {
            Vector3 vector = Vector3.zero;
            Ray aimRay = GetAimRay();
            aimRay.origin += UnityEngine.Random.insideUnitSphere * randomRadius;
            RaycastHit raycastHit;

            if (Physics.Raycast(aimRay, out raycastHit, (float)LayerIndex.world.mask))
            {
                vector = raycastHit.point;
            }
            if (vector == Vector3.zero)
            {
                return;
            }

            TeamIndex enemyTeam1 = TeamIndex.Player;
            TeamIndex enemyTeam2 = TeamIndex.Monster;
            
            Transform transform = FindTargetClosest(vector, enemyTeam1, enemyTeam2);

            Vector3 a = vector;
            if (transform)
            {
                a = transform.transform.position;
            }
            a += UnityEngine.Random.insideUnitSphere * randomRadius;

            if (Physics.Raycast(new Ray
            {
                origin = a + Vector3.up * randomRadius,
                direction = Vector3.down
            }, out raycastHit, 500f, LayerIndex.world.mask))
            {

                Vector3 point = raycastHit.point;
                Quaternion rotation;
                //Vector3 rot = new Vector3(90f, 0f, 0f);
                rotation = Quaternion.identity;//Quaternion.Euler(rot);
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = portalBombProjectileEffect;
                fireProjectileInfo.position = point;
                fireProjectileInfo.rotation = rotation;
                fireProjectileInfo.owner = base.gameObject;
                fireProjectileInfo.damage = body.damage * damageCoefficient;
                fireProjectileInfo.force = 0;
                fireProjectileInfo.crit = body.RollCrit();
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
            bombsFired++;
            if (attackStopWatch >= this.attackDuration)
            {
                isFiring = false;
            }
        }
        private Transform FindTargetClosest(Vector3 point, TeamIndex enemyTeam1, TeamIndex enemyTeam2)
        {
            ReadOnlyCollection<TeamComponent> teamMembers1 = TeamComponent.GetTeamMembers(enemyTeam1);
            ReadOnlyCollection<TeamComponent> teamMembers2 = TeamComponent.GetTeamMembers(enemyTeam2);
            float num = 99999f;
            Transform result = null;
            for (int i = 0; i < teamMembers1.Count; i++)
            {
                float num2 = Vector3.SqrMagnitude(teamMembers1[i].transform.position - point);
                if (num2 < num)
                {
                    num = num2;
                    result = teamMembers1[i].transform;
                }
            }
            for (int i = 0; i < teamMembers2.Count; i++)
            {
                float num2 = Vector3.SqrMagnitude(teamMembers2[i].transform.position - point);
                if (num2 < num)
                {
                    num = num2;
                    result = teamMembers2[i].transform;
                }
            }
            return result;
        }

        private void OneTimeBarrierGain()
        {
            body.healthComponent.AddBarrier(body.healthComponent.fullCombinedHealth);
            barrierBool = false;
        }

        protected Ray GetAimRay()
        {
            if (body.inputBank)
            {
                return new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            }
            return new Ray(base.transform.position, base.transform.forward);
        }

        public static float aimBaseDuration = EntityStates.NullifierMonster.AimPortalBomb.baseDuration;
        public static float arcMultiplier = EntityStates.NullifierMonster.AimPortalBomb.arcMultiplier;

        public static GameObject portalBombProjectileEffect = EntityStates.NullifierMonster.FirePortalBomb.portalBombProjectileEffect;
        public static GameObject muzzleflashEffectPrefab = EntityStates.NullifierMonster.FirePortalBomb.muzzleflashEffectPrefab;
        public static string muzzleString = EntityStates.NullifierMonster.FirePortalBomb.muzzleString;
        public int portalBombCount;
        public static float attackBaseDuration = 3;//EntityStates.NullifierMonster.FirePortalBomb.baseDuration;
        public static float maxDistance = EntityStates.NullifierMonster.FirePortalBomb.maxDistance * 4;
        public static float damageCoefficient = EntityStates.NullifierMonster.FirePortalBomb.damageCoefficient;
        public static float procCoefficient = EntityStates.NullifierMonster.FirePortalBomb.procCoefficient;
        public static float baseRandomRadius = 20;//EntityStates.NullifierMonster.FirePortalBomb.randomRadius;
        public static float force = EntityStates.NullifierMonster.FirePortalBomb.force;
        public static float minimumDistanceBetweenBombs = 5;//EntityStates.NullifierMonster.FirePortalBomb.minimumDistanceBetweenBombs;
        public Quaternion? startRotation;
        public Quaternion? endRotation;
        private float attackDuration;
        private int bombsFired;
        private float fireTimer;
        private float fireInterval;
        private int maxPortalBombs = 30;
        private int minPortalBombs = 5;

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
    


