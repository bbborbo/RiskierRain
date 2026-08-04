using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Components;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SwanSongExtended.Modules.EliteModule;
using static R2API.RecalculateStatsAPI;
using static RainrotSharedUtils.Shelters.ShelterUtilsModule;
using System.Collections.ObjectModel;
using RoR2.CharacterAI;
using SwanSongExtended.Storms;
using RoR2.Navigation;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using EntityStates.AI;
using SwanSongExtended.States.AI;
using EntityStates.AI.Walker;
using MonoMod.RuntimeDetour;
using System.Reflection;
using UnityEngine.Networking;
using RoR2.Audio;
using System.Linq;

namespace SwanSongExtended.Elites
{
    class WhirlwindAspect : T1EliteEquipmentBase<WhirlwindAspect>
    {
        #region
        public override string ConfigName => "Elites : Storm : " + EliteModifier;

        public static int   howlingEmpoweredArmor => SurgingAspect.surgingEmpoweredArmor;
        public static float howlingEmpoweredMoveSpeed = 0.8f;
        public static float howlingEmpoweredAtkSpeed = 0.3f;

        public static float squallDamagePerSecond = 7f;
        public static float squallAimDamping = 0.9f;
        public static float squallAimMaxSpeed = 80f;
        public static float squallBeamRadius = 3f;
        public static float squallBeamTickFrequency = 4f;

        public static GameObject squallBeamVfxPrefab;
        public static GameObject tetherVfxPrefab;
        #endregion

        public static GameObject howlingRallyBodyAttachment;

        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;

        //VERY important
        public override EliteTiers EliteTier { get; set; } = EliteTiers.StormT1;

        public override string EliteAffixToken => "AFFIX_SQUALL";

        public override string EliteModifier => "Howling"; //churning, gyrating, swirling, winding

        public override string EliteEquipmentName => "Twisted Stare";

        public override string EliteEquipmentPickupDesc => "Become an aspect of squall.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteHaunted/texBuffAffixHaunted.tif").WaitForCompletion();
        public override Color EliteBuffColor => Color.gray;

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matLeeching.mat");
        public override string EliteRampTextureName { get; set; } = "texRampHowling";
        //public override CombatDirector.EliteTierDef[] CanAppearInEliteTiers => new CombatDirector.EliteTierDef[1] { RiskierRainContent.StormT1 };

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_EliteEarth.AffixEarthBodyAttachment_prefab, CreateBodyAttachment);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamVFX_prefab, CreateBeamVfx);

            Modules.Content.AddEntityState(typeof(Converge));
            base.Init();
        }

        private void CreateBeamVfx(GameObject voidlingBeamVfx)
        {
            squallBeamVfxPrefab = voidlingBeamVfx.InstantiateClone("SquallBeamVfx", false);

            squallBeamVfxPrefab.transform.localScale = new Vector3(squallBeamRadius, squallBeamRadius, 30f);

            Transform meshAdditive = squallBeamVfxPrefab.transform.GetChild(0);
            if (meshAdditive)
            {
                if (meshAdditive.TryGetComponent(out MeshRenderer mr1))
                {
                    Material mat = UnityEngine.Object.Instantiate(mr1.material);
                    SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_FalseSonBoss.texFSBLunarSpikeRampGrey_png, (tex) =>
                    {
                        mat.SetTexture("_RemapTex", tex);
                        mat.SetColor("_TintColor", new Color32(215, 159, 100, 192));
                    });
                    mr1.material = mat;
                }

                Transform meshTransparent = meshAdditive.GetChild(0);
                if (meshTransparent.TryGetComponent(out MeshRenderer mr2))
                {
                    Material mat = UnityEngine.Object.Instantiate(mr2.material);
                    SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_FalseSonBoss.texFSBLunarSpikeRampGrey_png, (tex) =>
                    {
                        mat.SetTexture("_RemapTex", tex);
                        mat.SetColor("_TintColor", new Color32(64, 64, 64, 255));
                    });
                    mr2.material = mat;
                }
            }

            Transform lightMiddle = squallBeamVfxPrefab.transform.GetChild(3);
            if(lightMiddle && lightMiddle.TryGetComponent(out Light light1))
            {
                light1.color = new Color32(156, 156, 156, 255);
                light1.range = 30;
                light1.intensity = 30;
            }

            Transform lightEnd = squallBeamVfxPrefab.transform.GetChild(4);
            if(lightEnd && lightEnd.TryGetComponent(out Light light2))
            {
                light2.color = new Color32(156, 156, 156, 255);
                light2.range = 30;
                light2.intensity = 30;
            }

            Transform muzzleRayParticles = squallBeamVfxPrefab.transform.GetChild(6);
            if(muzzleRayParticles && muzzleRayParticles.TryGetComponent(out ParticleSystemRenderer psr3))
            {
                Material mat = UnityEngine.Object.Instantiate(psr3.material);
                SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampBanditSmokescreen_png, (tex) =>
                {
                    mat.SetTexture("_RemapTex", tex);
                    mat.SetColor("_TintColor", new Color32(213, 180, 136, 255));
                });
                psr3.material = mat;
            }
        }

        private void CreateBodyAttachment(GameObject earthEliteAttachment)
        {
            howlingRallyBodyAttachment = earthEliteAttachment.InstantiateClone("AffixSquallBodyAttachment", true);

            HowlingRallyController controller = howlingRallyBodyAttachment.AddComponent<HowlingRallyController>();

            if (howlingRallyBodyAttachment.TryGetComponent(out HealNearbyController healNearbyController))
            {
                controller.tetherVfxOrigin = healNearbyController.tetherVfxOrigin;
                controller.activeVfx = healNearbyController.activeVfx;
                UnityEngine.Object.Destroy(healNearbyController);
            }

            Transform activeVfx = howlingRallyBodyAttachment.transform.GetChild(0);

            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Elites_EliteBead.EliteBeadTether_prefab, (earthTetherVfx) =>
            {
                tetherVfxPrefab = earthTetherVfx.InstantiateClone("AffixSquallTetherVfx");
                controller.activeVfx = tetherVfxPrefab;
            });

            Modules.Content.AddNetworkedObjectPrefab(howlingRallyBodyAttachment);
        }

        public override void Hooks()
        {
            GetStatCoefficients += HowlingStats;
            On.RoR2.CharacterBody.AddOrRemoveEliteItemBehavior += AddAffixBehavior;
            On.RoR2.HealthComponent.TakeDamage += CycloneBlock;

            On.RoR2.CharacterAI.BaseAI.EvaluateSkillDrivers += LeaderSquallOverride;
            On.EntityStates.AI.Walker.Combat.FixedUpdate += HowlingConvergeCombat2;
            On.EntityStates.AI.Walker.Wander.FixedUpdate += HowlingConvergeWander2;
            On.EntityStates.AI.Walker.LookBusy.FixedUpdate += HowlingConvergeLookBusy2;
        }



        #region ai overriding
        private BaseAI.SkillDriverEvaluation LeaderSquallOverride(On.RoR2.CharacterAI.BaseAI.orig_EvaluateSkillDrivers orig, BaseAI self)
        {
            if (self.body.HasBuff(StormsCore.CycloneLeader))
            {
                if (CycloneController.instance != null
                    && CycloneController.instance.cycloneState >= CycloneController.CycloneState.PreparingSquall
                    && CycloneController.instance.HowlSquallDriver != null
                    && self.body.HasBuff(StormsCore.CycloneLeader))
                {
                    self.UpdateTargets();
                    self.customTarget.gameObject = CharacterMaster.instancesList
                        .Where(x => x.teamIndex == TeamIndex.Player)
                        .OrderByDescending(x => (x.GetBodyObject().transform.position - self.body.corePosition))
                        .FirstOrDefault().GetBodyObject(); ;

                    return new BaseAI.SkillDriverEvaluation
                    {
                        dominantSkillDriver = CycloneController.instance.HowlSquallDriver,
                        target = self.currentEnemy,
                        aimTarget = self.customTarget,
                        separationSqrMagnitude = float.PositiveInfinity
                    };
                }
            }
            return orig(self);
        }
        private void HowlingConvergeLookBusy2(On.EntityStates.AI.Walker.LookBusy.orig_FixedUpdate orig, EntityStates.AI.Walker.LookBusy self)
        {
            if (IsElite(self.body) && CycloneController.GetShouldConverge())
            {
                BaseAIState nextState = new Converge();
                self.outer.SetNextState(nextState);
                return;
            }
            orig(self);
        }

        private void HowlingConvergeCombat2(On.EntityStates.AI.Walker.Combat.orig_FixedUpdate orig, EntityStates.AI.Walker.Combat self)
        {
            if (IsElite(self.body) && CycloneController.GetShouldConverge())
            {
                BaseAIState nextState = new Converge();
                self.outer.SetNextState(nextState);
                return;
            }
            orig(self);
        }

        private void HowlingConvergeWander2(On.EntityStates.AI.Walker.Wander.orig_FixedUpdate orig, EntityStates.AI.Walker.Wander self)
        {
            //bool shouldConverge = !self.body.HasBuff(StormsCore.CycloneProtection) && self.body.HasBuff(EliteBuffDef) && CycloneController.GetShouldConverge();
            //TryConverge();
            //void TryConverge()
            //{
            //    if (!shouldConverge)
            //        return;
            //    Vector3 targetPosition = self.body.isFlying ? CycloneController.convergePositionAir : CycloneController.convergePositionGround;
            //
            //    BroadNavigationSystem.Agent broadNavigationAgent = self.ai.broadNavigationAgent;
            //    self.ai.SetGoalPosition(targetPosition);
            //    broadNavigationAgent.InvalidatePath();
            //    (self.ai.broadNavigationSystem as NodeGraphNavigationSystem).UpdateAgent(broadNavigationAgent.handle);
            //}
            if (IsElite(self.body) && CycloneController.GetShouldConverge())
            {
                BaseAIState nextState = new Converge();
                self.outer.SetNextState(nextState);
                return;
            }
            orig(self);
        }

        private void HowlingConvergeWander(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<BroadNavigationSystem.Agent>("set_goalPosition")
                );
            if (!b2)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(HowlingConvergeCombat), 2);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Vector3, EntityStates.AI.Walker.Wander, Vector3>>((goalPosIn, self) =>
            {
                if (!self.body.HasBuff(StormsCore.CycloneProtection) && self.body.HasBuff(EliteBuffDef) && CycloneController.GetShouldConverge())
                {
                    return self.body.isFlying ? CycloneController.convergePositionAir : CycloneController.convergePositionGround;
                }
                return goalPosIn;
            });
        }

        private void HowlingConvergeCombat(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<BaseAI>(nameof(BaseAI.SetGoalPosition))
                );
            if (!b1)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(HowlingConvergeCombat), 1);
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<EntityStates.AI.Walker.Combat>>((self) =>
            {
                if(!self.body.HasBuff(StormsCore.CycloneProtection) && self.body.HasBuff(EliteBuffDef) && CycloneController.GetShouldConverge())
                {
                    self.ai.SetGoalPosition(self.body.isFlying ? CycloneController.convergePositionAir : CycloneController.convergePositionGround);
                }
            });

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<BroadNavigationSystem.Agent>("set_goalPosition")
                );
            if (!b2)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(HowlingConvergeCombat), 2);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Vector3, EntityStates.AI.Walker.Combat, Vector3>>((goalPosIn, self) =>
            {
                if (!self.body.HasBuff(StormsCore.CycloneProtection) && self.body.HasBuff(EliteBuffDef) && CycloneController.GetShouldConverge())
                {
                    return self.body.isFlying ? CycloneController.convergePositionAir : CycloneController.convergePositionGround;
                }
                return goalPosIn;
            });
        }
        #endregion

        private void CycloneBlock(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            CharacterBody victimBody = self.body;
            if (damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody))
            {
                if (victimBody.HasBuff(StormsCore.CycloneProtection) && !attackerBody.HasBuff(StormsCore.CycloneProtection) && !IsBodySuperSheltered(victimBody) && victimBody.teamComponent.teamIndex != TeamIndex.Player)
                {
                    EffectManager.SpawnEffect(HealthComponent.AssetReferences.damageRejectedPrefab, new EffectData { origin = damageInfo.position }, true);
                    damageInfo.rejected = true;
                }
            }
            orig(self, damageInfo);
        }

        private void HowlingStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(EliteBuffDef))
            {
                bool isLeader = sender.HasBuff(StormsCore.CycloneLeader);
                if (!IsBodySuperSheltered(sender, sender.bestFitRadius))
                {
                    args.armorAdd +=            howlingEmpoweredArmor;
                    args.attackSpeedMultAdd +=  howlingEmpoweredAtkSpeed;
                    args.moveSpeedMultAdd +=    howlingEmpoweredMoveSpeed;
                }
                if (isLeader)
                {
                    args.armorAdd += howlingEmpoweredArmor;
                    args.attackSpeedMultAdd += howlingEmpoweredAtkSpeed;
                    args.moveSpeedMultAdd += howlingEmpoweredMoveSpeed;
                }
            }
        }
        private void AddAffixBehavior(On.RoR2.CharacterBody.orig_AddOrRemoveEliteItemBehavior orig, CharacterBody self, BuffDef buffDef, bool add)
        {
            if (buffDef == this.EliteBuffDef)
            {
                self.AddItemBehavior<AffixSquallBehavior>(add ? 1 : 0);
                return;
            }
            orig(self, buffDef, add);
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }

    public class AffixSquallBehavior : BaseStormEliteBehavior
    {
        public static bool GetShouldConverge()
        {
            return CycloneController.GetShouldConverge();
        }
        private static List<AffixSquallBehavior> instancesList = new List<AffixSquallBehavior>();
        public static ReadOnlyCollection<AffixSquallBehavior> readOnlyInstancesList = new ReadOnlyCollection<AffixSquallBehavior>(AffixSquallBehavior.instancesList);
        private GameObject affixSquallAttachment;


        bool isPlayer;
        bool isPlayerTeam;
        BaseAI baseAI;
        bool hasAuthority;
        private GameObject beamVfxInstance;
        private LoopSoundManager.SoundLoopPtr loopPtr;
        public bool isFiring
        {
            get
            {
                return _isFiring;
            }
            set
            {
                if (_isFiring != value)
                    UpdateIsFiring(value);
                _isFiring = value;
            }
        }

        private void UpdateIsFiring(bool newValue)
        {
            if(newValue == true)
            {
                this.beamVfxInstance = UnityEngine.Object.Instantiate<GameObject>(WhirlwindAspect.squallBeamVfxPrefab);
                this.beamVfxInstance.transform.SetParent(body.aimOriginTransform, true);
                this.UpdateBeamTransform();
                RoR2Application.onLateUpdate += this.UpdateBeamTransform;

                this.loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, EntityStates.VoidRaidCrab.SpinBeamAttack.loopSound);
                //Util.PlaySound(EntityStates.VoidRaidCrab.SpinBeamAttack.enterSoundString, base.gameObject);
            }
            else
            {
                RoR2Application.onLateUpdate -= this.UpdateBeamTransform;
                VfxKillBehavior.KillVfxObject(this.beamVfxInstance);
                this.beamVfxInstance = null;

                LoopSoundManager.StopSoundLoopLocal(this.loopPtr);
            }
        }

        bool _isFiring = false;

        void OnEnable()
        {
            instancesList.Add(this);
        }

        private void UpdateBeamTransform()
        {
            Ray beamRay = this.GetBeamRay();
            this.beamVfxInstance.transform.SetPositionAndRotation(beamRay.origin, Quaternion.LookRotation(beamRay.direction));
        }

        void OnDisable()
        {
            if (instancesList.Contains(this))
            {
                instancesList.Remove(this);
            }
            _isFiring = false;
        }

        void Start()
        {
            if (this.body)
            {
                isPlayer = body.isPlayerControlled;
                isPlayerTeam = body.teamComponent.teamIndex == TeamIndex.Player;
                if (!isPlayer)
                {
                    CharacterMaster master = this.body.master;
                    baseAI = ((master != null) ? master.GetComponent<BaseAI>() : null);
                }
                else
                {

                }

                hasAuthority = Util.HasEffectiveAuthority(body.networkIdentity);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!NetworkServer.active)
            {
                return;
            }

            DoBodyAttachment();

            DoBeamAttackServer();
        }

        float beamTickTimer = 0f;
        private void DoBeamAttackServer()
        {
            if (!isFiring)
            {
                beamTickTimer = 1f / EntityStates.VoidRaidCrab.SpinBeamAttack.beamTickFrequency;
                return;
            }

            beamTickTimer -= Time.fixedDeltaTime;
            if (beamTickTimer > 0)
                return;
            beamTickTimer += 1f / EntityStates.VoidRaidCrab.SpinBeamAttack.beamTickFrequency;

            Ray beamRay = GetBeamRay();
            new BulletAttack
            {
                muzzleName = "Head",
                origin = beamRay.origin,
                aimVector = beamRay.direction,
                minSpread = 0f,
                maxSpread = 0f,
                maxDistance = 1000f,
                hitMask = LayerIndex.CommonMasks.bullet,
                stopperMask = 0,
                bulletCount = 1U,
                radius = WhirlwindAspect.squallBeamRadius,
                smartCollision = false,
                queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                procCoefficient = 1f,
                procChainMask = default(ProcChainMask),
                owner = base.gameObject,
                weapon = base.gameObject,
                damage = WhirlwindAspect.squallDamagePerSecond * Tools.GetAmbientLevelScalar(0.2f) / WhirlwindAspect.squallBeamTickFrequency,
                damageColorIndex = DamageColorIndex.Default,
                damageType = DamageType.Generic,
                falloffModel = BulletAttack.FalloffModel.None,
                force = 0f,
                hitEffectPrefab = null,// EntityStates.VoidRaidCrab.SpinBeamAttack.beamImpactEffectPrefab,
                tracerEffectPrefab = null,
                isCrit = false,
                HitEffectNormal = false
            }.Fire();
        }

        protected Ray GetBeamRay()
        {
            if (body.inputBank)
            {
                return new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            }
            return new Ray(body.transform.position, body.transform.forward);
        }

        private void DoBodyAttachment()
        {
            bool flag = this.stack > 0;
            if (affixSquallAttachment != flag)
            {
                if (flag)
                {
                    this.affixSquallAttachment = UnityEngine.Object.Instantiate<GameObject>(WhirlwindAspect.howlingRallyBodyAttachment);
                    this.affixSquallAttachment.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(this.body.gameObject, null);
                    return;
                }
                UnityEngine.Object.Destroy(this.affixSquallAttachment);
                this.affixSquallAttachment = null;
            }
        }
    }
}
