using BepInEx.Configuration;
using EntityStates.BrotherMonster;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.EliteModule;
using static RainrotSharedUtils.Shelters.ShelterUtilsModule;
using static R2API.RecalculateStatsAPI;
using RoR2.CharacterAI;
using RoR2.Navigation;
using SwanSongExtended.Components;
using RoR2.Artifacts;
using static R2API.DamageAPI;
using System.Collections.ObjectModel;

namespace SwanSongExtended.Elites
{
    class SurgingAspect : StormEliteEquipmentBase<SurgingAspect>
    {
        #region config
        public override string ConfigName => "Elites : Storm : " + EliteModifier;
        #endregion

        public static ModdedDamageType riptideDamageType;
        public static BuffDef riptideDebuff;
        public static int riptideArmorPenalty = 20;
        public static float riptideMovementPenalty = 0.8f;
        public static float riptideDuration = 1f;

        public static GameObject waveProjectilePrefab;
        public static GameObject cannonballProjectilePrefab;
        public static float waveProjectileSpeed = 30f; //60f
        public static float waveProjectileDuration = 1.5f; //3f
        public static float waveProjectileCount = 5f; //12f
        public static float waveProjectileBaseDamage = 8f;
        public static float waveProjectileDamageLevel = 0.3f;
        public static float waveProjectileProcCoefficient = 2.0f;
        public static float waveProjectileForce = 150f;
        public static int cannonballBouncesMin = 1;
        /// <summary>
        /// uses hull classification instead of body size
        /// </summary>
        public static int cannonballBouncesPerSize = 1;
        public static float cannonballInitialVelocity = 50f;

        public static GameObject teleportEffect;
        public static GameObject teleportTracer;
        public static float cannonballGravityCoefficient = 0.2f;
        public static float teleportDistanceFromCurrent = 60;
        public static float teleportDistanceFromTargetMin = 10;
        public static float teleportDistanceFromTarget = 30;
        public static float teleportDistanceFromTargetPerSize = 0;
        public static float teleportDelay = 0.7f;
        public static float teleportWaveDelay = 0.3f;
        public static float teleportEffectDuration = 1.0f;
        public static float teleportStaggerDuration = 4f;

        public static int surgingEmpoweredArmor = 200;
        public static float surgingEmpoweredMoveSpeed = 0.8f;
        public static float surgingEmpoweredAtkSpeed = 0.3f;

        public override float EliteDamageModifier => 0;
        public override float EliteHealthModifier => 0;

        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;

        //VERY important
        public override EliteTiers EliteTier { get; set; } = EliteTiers.StormT1;

        public override string EliteAffixToken => "AFFIX_FLOOD";

        public override string EliteModifier => "Surging";

        public override string EliteEquipmentName => "A Dormant Force";

        public override string EliteEquipmentPickupDesc => "Become an aspect of flood.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteFire/texBuffAffixRed.tif").WaitForCompletion();
        public override Color EliteBuffColor => Color.cyan;

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matLeeching.mat");
        public override string EliteRampTextureName { get; set; } = "texRampSurging";
        //public override CombatDirector.EliteTierDef[] CanAppearInEliteTiers => new CombatDirector.EliteTierDef[1] { RiskierRainContent.StormT1 };

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 18f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();

            riptideDamageType = ReserveDamageType();

            riptideDebuff = Modules.Content.CreateAndAddBuff(
                "bdFloodEliteRiptide",
                null,//Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.texBuffSlow50Icon_tif).WaitForCompletion(),
                Color.blue,
                canStack: false,
                isDebuff: true,
                isHidden: false
                );
            SwanSongPlugin.LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.bdSlow50_asset, (bd) =>
            {
                riptideDebuff.iconSprite = bd.iconSprite;
            });

            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bomb.SpiteBomb_prefab, CreateCannonballProjectile);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Junk_Parent.ParentTeleportEffect_prefab, CreateTeleportEffect);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidSurvivor.VoidSurvivorBeamTracer_prefab, CreateTeleportTracer);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Brother.BrotherSunderWave_prefab, CreateWaveProjectile);
        }

        private void CreateWaveProjectile(GameObject baseWaveProjectile)
        {
            Vector3 size = new Vector3(20f, 2.0f, 1.0f);//30f, 4.5f, 1.0f
            waveProjectilePrefab = baseWaveProjectile.InstantiateClone("FloodWaveProjectile", true);

            if(waveProjectilePrefab.TryGetComponent(out ProjectileDamage pd))
            {
                pd.damageType = new DamageTypeCombo();
                pd.damageType.AddModdedDamageType(riptideDamageType);
            }

            if(waveProjectilePrefab.TryGetComponent(out ProjectileController pc))
            {
                SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Brother.BrotherSunderWaveGhost_prefab, (baseGhost) =>
                {
                    GameObject waveGhost = baseGhost.InstantiateClone("FloodWaveProjectileGhost");

                    Transform sizeParent = waveGhost.transform.GetChild(0);

                    //lunar spikes/corruption
                    sizeParent.GetChild(0).gameObject.SetActive(false);
                    //dust
                    sizeParent.GetChild(1).gameObject.SetActive(false);
                    //debris, off by default
                    sizeParent.GetChild(2).gameObject.SetActive(false);
                    //water
                    sizeParent.GetChild(3).gameObject.SetActive(true);

                    Transform hitbox = waveGhost.transform.GetChild(1);
                    hitbox.localScale = size;

                    pc.ghostPrefab = waveGhost;

                    //no need to register ghost prefab ig
                });
            }

            Transform hitbox = waveProjectilePrefab.transform.GetChild(0);
            hitbox.localScale = size; 

            if(waveProjectilePrefab.TryGetComponent(out ProjectileCharacterController projectileCharacterController))
            {
                projectileCharacterController.velocity = waveProjectileSpeed; //60
                projectileCharacterController.lifetime = waveProjectileDuration; //3
            }

            if(waveProjectilePrefab.TryGetComponent(out ProjectileOverlapAttack overlap))
            {
                overlap.overlapProcCoefficient = waveProjectileProcCoefficient;
                overlap.forceVector = Vector3.up * waveProjectileForce;
            }

            Modules.Content.AddProjectilePrefab(waveProjectilePrefab);
        }

        private void CreateTeleportTracer(GameObject baseTracerEffect)
        {
            teleportTracer = baseTracerEffect.InstantiateClone("FloodTpTracerEffect", false);
            teleportTracer.transform.GetChild(0).gameObject.SetActive(false);
            teleportTracer.transform.GetChild(1).gameObject.SetActive(false);

            if(teleportTracer.TryGetComponent(out LineRenderer lineRenderer))
            {
                lineRenderer.widthMultiplier = 0.35f;
                lineRenderer.numCapVertices = 10;

                Material mat = UnityEngine.Object.Instantiate(lineRenderer.material);
                SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampLunarWardDecal_png, (tex) =>
                {
                    mat.SetTexture("_RemapTex", tex);
                    lineRenderer.material = mat;
                });
            }
            if(teleportTracer.TryGetComponent(out AnimateShaderAlpha asa))
            {
                asa.timeMax = teleportEffectDuration;
            }
            Modules.Content.CreateAndAddEffectDef(teleportTracer);
        }

        private void CreateTeleportEffect(GameObject baseTpEffect)
        {
            teleportEffect = baseTpEffect.InstantiateClone("FloodTpEffect", false);

            Transform particles = teleportEffect.transform.GetChild(0);

            SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampLunarWispFire_png, (tex) =>
            {
                if (particles.GetChild(0).TryGetComponent(out ParticleSystemRenderer ringParticle))
                {
                    Material mat = UnityEngine.Object.Instantiate(ringParticle.sharedMaterial);
                    mat.SetTexture("_RemapTex", tex);
                    ringParticle.sharedMaterial = mat;
                }
                if (particles.GetChild(3).TryGetComponent(out ParticleSystemRenderer energyInitialParticle))
                {
                    Material mat = UnityEngine.Object.Instantiate(energyInitialParticle.sharedMaterial);
                    mat.SetTexture("_RemapTex", tex);
                    ringParticle.sharedMaterial = mat;
                }
            });

            particles.GetChild(1).gameObject.SetActive(false);
            particles.GetChild(4).gameObject.SetActive(false);

            Modules.Content.CreateAndAddEffectDef(teleportEffect);
        }

        private void CreateCannonballProjectile(GameObject spiteBomb)
        {
            cannonballProjectilePrefab = spiteBomb.InstantiateClone("FloodCannonballProjectile", true);

            if(cannonballProjectilePrefab.TryGetComponent(out SpiteBombController bombController))
            {
                CannonballController cannonball = cannonballProjectilePrefab.AddComponent<CannonballController>();
                HG.ArrayUtils.CloneTo(bombController.bounceSoundStrings, ref cannonball.bounceSoundStrings);
                cannonball.initialVelocityY = cannonballInitialVelocity;
                cannonball.minimumBounceVelocity = cannonballInitialVelocity;
                cannonball.radius = bombController.radius;

                cannonball.meshVisuals = /*bombController.meshVisuals;*/ new GameObject[3]
                {
                    cannonball.transform.GetChild(0).gameObject,
                    cannonball.transform.GetChild(1).gameObject,
                    cannonball.transform.GetChild(2).gameObject
                };

                UnityEngine.Object.Destroy(bombController);
            }

            //if(cannonballProjectilePrefab.TryGetComponent(out Rigidbody rb))
            //{
            //    rb.isKinematic = false;
            //}

            Modules.Content.AddNetworkedObjectPrefab(cannonballProjectilePrefab);
        }

        public override void Hooks()
        {
            GetStatCoefficients += SurgingStats;
            On.RoR2.CharacterBody.AddOrRemoveEliteItemBehavior += AddAffixBehavior;
            MoreStats.OnHit.GetHitBehavior += RiptideOnHit;
            GlobalEventManager.onCharacterDeathGlobal += FireCannonball;
        }

        private void FireCannonball(DamageReport damageReport)
        {
            if (NetworkServer.active)
            {
                if (damageReport.victimBody.HasBuff(EliteBuffDef))
                {
                    FireCannonballProjectile(damageReport.victimBody, damageReport.victimTeamIndex);
                }
            }
        }

        private void FireCannonballProjectile(CharacterBody victimBody, TeamIndex team)
        {
            if (victimBody.healthComponent.globalDeathEventChanceCoefficient < 1)
                return;

            Vector3 spawnPosition = victimBody.corePosition;
            Ray ray = new Ray(spawnPosition + new Vector3(0f, BombArtifactManager.maxBombStepUpDistance, 0f), Vector3.down);
            float maxDistance = BombArtifactManager.maxBombStepUpDistance + BombArtifactManager.maxBombFallDistance;
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                float groundY = raycastHit.point.y;

                if (spawnPosition.y < groundY + 4f)
                {
                    spawnPosition.y = groundY + 4f;
                }
                Vector3 bouncePosition = ray.origin;
                bouncePosition.y = groundY;

                int level = 0;
                if (Run.instance != null)
                    level = Run.instance.ambientLevelFloor;

                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(cannonballProjectilePrefab, spawnPosition, UnityEngine.Random.rotation);
                CannonballController cannonball = gameObject.GetComponent<CannonballController>();
                cannonball.maxBounces = cannonballBouncesMin + (int)victimBody.radius * (cannonballBouncesPerSize - 1);
                cannonball.startPosition = spawnPosition;
                cannonball.rb.MovePosition(spawnPosition);
                DelayBlast delayBlast = cannonball.delayBlast;
                TeamFilter teamFilter = gameObject.GetComponent<TeamFilter>();
                cannonball.bouncePosition = bouncePosition;
                cannonball.initialVelocityY = cannonballInitialVelocity;
                delayBlast.position = spawnPosition;
                delayBlast.baseDamage = waveProjectileBaseDamage * Tools.GetAmbientLevelScalar(waveProjectileDamageLevel);
                delayBlast.baseForce = 2300f;
                delayBlast.attacker = victimBody.gameObject;
                delayBlast.radius = BombArtifactManager.bombBlastRadius;
                delayBlast.crit = false;
                delayBlast.procCoefficient = 0.75f;
                delayBlast.maxTimer = BombArtifactManager.bombFuseTimeout / cannonballGravityCoefficient;
                delayBlast.timerStagger = 0f;
                delayBlast.falloffModel = BlastAttack.FalloffModel.Linear;
                delayBlast.teamFilter = teamFilter;
                teamFilter.teamIndex = team;
                NetworkServer.Spawn(gameObject);
            }
        }

        private void SurgingStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(riptideDebuff))
            {
                args.armorAdd -= riptideArmorPenalty;
                args.moveSpeedReductionMultAdd += riptideMovementPenalty;
            }
            if (sender.HasBuff(EliteBuffDef))
            {
                if (!IsBodySuperSheltered(sender, sender.bestFitRadius))
                {
                    args.armorAdd += surgingEmpoweredArmor;
                    args.attackSpeedMultAdd += surgingEmpoweredAtkSpeed;
                    args.moveSpeedMultAdd += surgingEmpoweredMoveSpeed;
                }
            }
        }

        private void RiptideOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (!NetworkServer.active)
                return;
            if (attackerBody.HasBuff(EliteBuffDef) || damageInfo.HasModdedDamageType(riptideDamageType))
            {
                victimBody.AddTimedBuff(riptideDebuff, riptideDuration * damageInfo.procCoefficient);
            }
        }

        private void AddAffixBehavior(On.RoR2.CharacterBody.orig_AddOrRemoveEliteItemBehavior orig, CharacterBody self, BuffDef buffDef, bool add)
        {
            if(buffDef == this.EliteBuffDef)
            {
                self.AddItemBehavior<AffixFloodBehavior>(add ? 1 : 0);
                return;
            }
            orig(self, buffDef, add);
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            if(slot.characterBody && slot.characterBody.TryGetComponent(out AffixFloodBehavior floodBehavior))
            {
                Ray aimRay = slot.GetAimRay();
                Quaternion.LookRotation(aimRay.direction);
                Vector3 origin = slot.characterBody.transform.position;
                slot.characterBody.transform.TransformDirection(Vector3.forward);
                Vector3 groundPosition = Vector3.zero;
                Vector3 groundNormal = Vector3.zero;
                RaycastHit raycastHit;
                if (Physics.Raycast(origin, aimRay.direction, out raycastHit, teleportDistanceFromCurrent, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    if (floodBehavior.TryPickNextTpLocation(raycastHit.point, out Vector3 loc))
                    {
                        floodBehavior.TryTeleport(loc);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void FireRingAuthority(Vector3 position, Vector3 forward, GameObject attacker, float baseDamage, bool isCrit)
        {
            float num = 360f / (float)SurgingAspect.waveProjectileCount;
            Vector3 point = Vector3.ProjectOnPlane(forward, Vector3.up);
            for (int i = 0; i < ExitSkyLeap.waveProjectileCount; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(num * (float)i, Vector3.up) * point;

                ProjectileManager.instance.FireProjectileWithoutDamageType(
                    SurgingAspect.waveProjectilePrefab,
                    position, Util.QuaternionSafeLookRotation(dir),
                    attacker,
                    waveProjectileBaseDamage * Tools.GetAmbientLevelScalar(waveProjectileDamageLevel),
                    0,
                    isCrit,
                    DamageColorIndex.Default, null, -1f) ;
            }
        }
    }

    public class AffixFloodBehavior : BaseStormEliteBehavior
    {
        private static List<AffixFloodBehavior> instancesList = new List<AffixFloodBehavior>();
        public static ReadOnlyCollection<AffixFloodBehavior> readOnlyInstancesList = new ReadOnlyCollection<AffixFloodBehavior>(AffixFloodBehavior.instancesList);

        public void TryTeleport(Vector3 loc)
        {
            SetTeleportLocation(loc);
            StepPreTeleport();
        }
        public bool TryPickNextTpLocation(Transform targetTransform, out Vector3 endPosition)
        {
            endPosition = Vector3.zero;
            return this.TryPickNextTpLocation(targetTransform.position, out endPosition);
        }

        public bool TryPickNextTpLocation(Vector3 nearPosition, out Vector3 teleportPosition)
        {
            if (!DirectorCore.instance || !SceneInfo.instance)
            {
                teleportPosition = default(Vector3);
                return false;
            }

            NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
            List<NodeGraph.NodeIndex> list = 
                nodeGraph.FindNodesInRange(nearPosition, 
                isPlayer ? SurgingAspect.teleportDistanceFromTargetMin : 0, 
                isPlayer ? SurgingAspect.teleportDistanceFromTarget : SurgingAspect.teleportDistanceFromTargetMin, 
                (HullMask)body.hullClassification);

            if (list.Count >= 1)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    NodeGraph.NodeIndex nodeIndex = list[i];
                    Vector3 vector;
                    if (!DirectorCore.instance.CheckNodeOccupied(nodeGraph, nodeIndex) 
                        && nodeGraph.GetNodePosition(nodeIndex, out vector) 
                        && (this.collisionCheckDistance < 0f || this.CheckTeleportPositionValid(vector)))
                    {
                        teleportPosition = vector;
                        return true;
                    }
                }
            }
            teleportPosition = default(Vector3);
            return false;
        }
        private bool CheckTeleportPositionValid(Vector3 telePosition)
        {
            return Physics.OverlapSphere(telePosition, this.collisionCheckDistance, LayerIndex.CommonMasks.allCharacterCollisions, QueryTriggerInteraction.Ignore).Length == 0;
        }

        internal float cooldownTimer = 0;
        internal Action nextStep;
        bool isPlayer;
        BaseAI baseAI;
        bool hasAuthority;
        float collisionCheckDistance => body.radius;

        internal float onStartDelayForPlayers = 0.25f;
        internal float onStartDelayForNPCs = 5f;
        internal float targetSearchCooldown = 2f;
        internal bool useRandomTargeting;

        internal bool foundLocation = false;
        internal Vector3 teleportLocation = Vector3.zero;

        internal static float teleportStaggerTimestamp = 0;

        void Start()
        {
            if (this.body)
            {
                isPlayer = body.isPlayerControlled;
                if (!isPlayer)
                {
                    CharacterMaster master = this.body.master;
                    baseAI = ((master != null) ? master.GetComponent<BaseAI>() : null);
                    this.cooldownTimer = onStartDelayForNPCs;
                }
                else
                {
                    cooldownTimer = onStartDelayForPlayers;
                }

                useRandomTargeting =
                    this.isPlayer ?
                    this.body.inventory == null || this.body.inventory.currentEquipmentIndex != SurgingAspect.instance.EliteEquipmentDef.equipmentIndex
                    : baseAI == null;

                hasAuthority = Util.HasEffectiveAuthority(body.networkIdentity);
            }
        }

        void OnEnable()
        {
            instancesList.Add(this);

            this.cooldownTimer = 10f;
            this.nextStep = new Action(StepIdentifyNextLocation);

            if(body != null)
            {
                hasAuthority = Util.HasEffectiveAuthority(body.networkIdentity);
                if (!hasAuthority)
                {
                    body.OnNetworkItemBehaviorUpdate += OnNetworkItemUpdate;
                }
            }
        }

        void OnDisable()
        {
            if (instancesList.Contains(this))
            {
                instancesList.Remove(this);
            }

            if (!hasAuthority && body != null)
            {
                body.OnNetworkItemBehaviorUpdate -= OnNetworkItemUpdate;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hasAuthority || body == null)
                return;

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0)
                return;
            nextStep();
        }

        void OnNetworkItemUpdate(CharacterBody.NetworkItemBehaviorData data)
        {
            if (data.buffIndex != SurgingAspect.instance.EliteBuffDef.buffIndex)
                return;
            if (hasAuthority)
                return;
            PreFireVFX();
        }

        void StepIdentifyNextLocation()
        {
            if (isPlayer || baseAI == null || body.healthComponent.globalDeathEventChanceCoefficient < 1)
            {
                QuickCooldown(float.PositiveInfinity);
                return;
            }

            if (body.HasBuff(Storms.StormsCore.StormEliteWeak) || body.healthComponent.isInFrozenState)
            {
                this.QuickCooldown(targetSearchCooldown);
                foundLocation = false;
                return;
            }

            if (foundLocation)
            {
                float delta = Time.time - teleportStaggerTimestamp;
                if(isPlayer || delta >= SurgingAspect.teleportStaggerDuration)
                {
                    nextStep = new Action(StepPreTeleport);
                    QuickCooldown(0);
                    return;
                }
                else if (!isPlayer)
                {
                    QuickCooldown(delta + UnityEngine.Random.Range(0, 1));
                    return;
                }
            }

            baseAI.ForceAcquireNearestEnemyIfNoCurrentEnemy();
            if (baseAI.currentEnemy.gameObject == null)
            {
                this.QuickCooldown(targetSearchCooldown);
                return;
            }

            if (TryPickNextTpLocation(baseAI.currentEnemy.characterBody.footPosition, out Vector3 loc) && !IsPositionSheltered(loc))
            {
                SetTeleportLocation(loc);
                this.QuickCooldown(0.3f);
                return;
            }
            this.QuickCooldown(targetSearchCooldown);
        }

        public void SetTeleportLocation(Vector3 position)
        {
            foundLocation = true;
            teleportLocation = position;
        }

        void StepPreTeleport()
        {
            CharacterBody.NetworkItemBehaviorData itemBehaviorData = new CharacterBody.NetworkItemBehaviorData(SurgingAspect.instance.EliteBuffDef.buffIndex, 1f);
            this.body.TransmitItemBehavior(itemBehaviorData, false);

            PreFireVFX();

            if (!isPlayer)
                teleportStaggerTimestamp = Time.time;

            QuickCooldown(SurgingAspect.teleportDelay);
            nextStep = new Action(StepTeleportToLocation);
        }

        //called by StepPreTeleport and OnNetworkItemUpdate
        void PreFireVFX()
        {
            Vector3 currentPosition = body.corePosition;
            EffectManager.SpawnEffect(SurgingAspect.teleportEffect, new EffectData
            {
                scale = 1.5f,
                origin = currentPosition
            }, true);
            EffectManager.SpawnEffect(SurgingAspect.teleportEffect, new EffectData
            {
                scale = 1.5f,
                origin = teleportLocation
            }, true);
            EffectManager.SpawnEffect(SurgingAspect.teleportTracer, new EffectData
            {
                start = currentPosition,
                origin = teleportLocation
            }, true);
        }

        void StepTeleportToLocation()
        {
            if (body.healthComponent.isInFrozenState)
            {
                this.QuickCooldown(targetSearchCooldown);
                nextStep = new Action(StepIdentifyNextLocation);
                foundLocation = false;
                return;
            }

            Vector3 currentPosition = body.corePosition;
            EffectManager.SpawnEffect(SurgingAspect.teleportEffect, new EffectData
            {
                scale = 0.66f,
                origin = currentPosition
            }, true);

            TeleportHelper.TeleportBody(body, teleportLocation, true);

            EffectManager.SpawnEffect(SurgingAspect.teleportEffect, new EffectData
            {
                scale = 0.66f,
                origin = teleportLocation
            }, true);

            if(body.healthComponent.TryGetComponent(out SetStateOnHurt ssoh))
            {
                ssoh.OverrideStun(2f);
            }
            QuickCooldown(SurgingAspect.teleportWaveDelay);
            nextStep = new Action(StepFireWaveProjectile);
        }

        void StepFireWaveProjectile()
        {
            SurgingAspect.FireRingAuthority(teleportLocation, body.inputBank.aimDirection, body.gameObject, body.damage, Util.CheckRoll(body.crit, body.master));

            QuickCooldown(SurgingAspect.instance.Cooldown);
            this.foundLocation = false;
            nextStep = new Action(StepIdentifyNextLocation);
        }

        void QuickCooldown(float duration)
        {
            cooldownTimer = duration;
        }
    }
}
