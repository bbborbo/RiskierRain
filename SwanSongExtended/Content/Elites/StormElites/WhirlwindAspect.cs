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
using static MoreStats.StatHooks;
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
using SwanSongExtended.Modules;
using RoR2.Projectile;

namespace SwanSongExtended.Elites
{
    class WhirlwindAspect : T1EliteEquipmentBase<WhirlwindAspect>
    {
        #region
        public override string ConfigName => "Elites : Storm : " + EliteModifier;

        public static int   howlingEmpoweredArmor => SurgingAspect.surgingEmpoweredArmor;
        public static float howlingEmpoweredMoveSpeed = 0.8f;
        public static float howlingEmpoweredAtkSpeed = 0.3f;

        public static float playerSquallDuration = StormsCore.squallFireDurationMin + StormsCore.squallFireDurationBonusPerOverspill;
        public static float squallDamagePerSecond = 40f;
        public static float squallDamagePerLevel = 0.4f;//0.2f
        /// <summary>
        /// expressed in seconds?
        /// </summary>
        public static float squallAimDamping = 0.6f;
        public static float squallPreFireTime = 2.0f;
        public static float squallAimMaxSpeed = 40f;
        public static float squallBeamRadius = 1.75f;
        public static float squallPreBeamRadius = 0.75f;
        public static float squallBeamTickFrequency = 8f;

        public static float missileDamageBase = 6f;
        public static float missileDamagePerLevel = 0.3f;
        public static int missileCtBase = 3;
        public static int missileCtPerSize = 1;

        public static GameObject squallBeamVfxPrefab;
        public static GameObject squallPreBeamVfxPrefab;
        public static GameObject tetherVfxPrefab;
        public static GameObject howlWindMissilePrefab;
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
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.MissileProjectile_prefab, CreateHowlMissile);

            Modules.Content.AddEntityState(typeof(Converge));
            base.Init();
        }

        private void CreateHowlMissile(GameObject missilePrefab)
        {
            howlWindMissilePrefab = missilePrefab.InstantiateClone("HowlWindMissile", true);

            if(howlWindMissilePrefab.TryGetComponent(out ProjectileController projectile))
            {
                SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Lemurian.FireballGhost_prefab, (fireballGhost) =>
                {
                    //no need to register ghost to content pack
                    GameObject ghost = fireballGhost.InstantiateClone("HowlWindMissileGhost", false);
                    projectile.ghostPrefab = ghost;

                    //if(ghost.TryGetComponent(out ParticleSystem particleSystem))
                    //{
                    //    ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
                    //    colorOverLifetime.color.
                    //}
                });
            }

            if(howlWindMissilePrefab.TryGetComponent(out ProjectileDamage pd))
            {
                pd.damageType.damageSource = DamageSource.Equipment;
            }

            Content.AddProjectilePrefab(howlWindMissilePrefab);
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
                        mat.SetColor("_TintColor", new Color32(185, 169, 90, 162));
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
                        mat.SetColor("_TintColor", new Color32(54, 54, 54, 255));
                    });
                    mr2.material = mat;
                }
            }

            Transform glows = squallBeamVfxPrefab.transform.GetChild(1);
            if(glows && glows.gameObject.TryGetComponent(out ParticleSystem psr0))
            {
                glows.gameObject.SetActive(false);
            }

            Transform billboards = squallBeamVfxPrefab.transform.GetChild(2);
            if(billboards && billboards.gameObject.TryGetComponent(out ParticleSystemRenderer psr))
            {
                Material mat = UnityEngine.Object.Instantiate(psr.material);
                SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampMissileTrail_png, (tex) =>
                {
                    mat.SetTexture("_RemapTex", tex);
                    mat.SetColor("_TintColor", new Color32(234, 228, 171, 255));
                });
                psr.material = mat;
            }

            Transform lightMiddle = squallBeamVfxPrefab.transform.GetChild(3);
            if (lightMiddle && lightMiddle.TryGetComponent(out Light light1))
            {
                light1.color = new Color32(156, 156, 156, 255);
                light1.range = 30;
                light1.intensity = 30;
            }

            Transform lightEnd = squallBeamVfxPrefab.transform.GetChild(4);
            if (lightEnd && lightEnd.TryGetComponent(out Light light2))
            {
                light2.color = new Color32(156, 156, 156, 255);
                light2.range = 30;
                light2.intensity = 30;
            }

            Transform swirlyParticles = squallBeamVfxPrefab.transform.GetChild(5);
            if (swirlyParticles && swirlyParticles.gameObject.TryGetComponent(out ParticleSystemRenderer psr2))
            {
                Material mat = UnityEngine.Object.Instantiate(psr2.sharedMaterials[1]);
                SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampBanditSmokescreen_png, (tex) =>
                {
                    mat.SetTexture("_RemapTex", tex);
                    mat.SetColor("_TintColor", new Color32(199, 180, 79, 255));
                });
                psr2.sharedMaterials = new Material[] { null, mat };
            }

            Transform muzzleRayParticles = squallBeamVfxPrefab.transform.GetChild(6);
            if (muzzleRayParticles && muzzleRayParticles.TryGetComponent(out ParticleSystemRenderer psr3))
            {
                Material mat = UnityEngine.Object.Instantiate(psr3.material);
                SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampBanditSmokescreen_png, (tex) =>
                {
                    mat.SetTexture("_RemapTex", tex);
                    mat.SetColor("_TintColor", new Color32(213, 180, 136, 255));
                });
                psr3.material = mat;
            }

            squallPreBeamVfxPrefab = voidlingBeamVfx.InstantiateClone("SquallPreBeamVfx", false);

            squallPreBeamVfxPrefab.transform.localScale = new Vector3(squallPreBeamRadius, squallPreBeamRadius, 30f);

            TryDestroyChild(squallPreBeamVfxPrefab, 1);
            TryDestroyChild(squallPreBeamVfxPrefab, 2);
            TryDestroyChild(squallPreBeamVfxPrefab, 3);
            TryDestroyChild(squallPreBeamVfxPrefab, 4);
            TryDestroyChild(squallPreBeamVfxPrefab, 5);
            TryDestroyChild(squallPreBeamVfxPrefab, 6);
            Transform meshAdditive1 = squallPreBeamVfxPrefab.transform.GetChild(0);
            if (meshAdditive1)
            {
                Transform meshTransparent1 = meshAdditive1.GetChild(0);
                if (meshTransparent1.TryGetComponent(out MeshRenderer mr2))
                {
                    Material mat = UnityEngine.Object.Instantiate(mr2.material);
                    SwanSongPlugin.LoadAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_FalseSonBoss.texFSBLunarSpikeRampGrey_png, (tex) =>
                    {
                        mat.SetTexture("_RemapTex", tex);
                        mat.SetColor("_TintColor", new Color32(54, 54, 54, 205));
                    });
                    mr2.material = mat;
                }
                meshTransparent1.transform.SetParent(squallPreBeamVfxPrefab.transform);
                TryDestroyChild(meshAdditive1.gameObject);
            }

            void TryDestroyChild(GameObject parent, int? index = null)
            {
                Transform t = index == null ? parent.transform : parent.transform.GetChild(index.Value);
                if (t != null)
                    t.gameObject.SetActive(false);
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

            if(howlingRallyBodyAttachment.TryGetComponent(out TetherVfxOrigin tether))
            {
                SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_EliteEarth.AffixEarthTetherVFX_prefab, (sharedSufferingTether) =>
                {
                    GameObject tetherPrefab = sharedSufferingTether.InstantiateClone("AffixSquallTetherVFX", false);
                    tether.tetherPrefab = tetherPrefab;

                    if(tetherPrefab.TryGetComponent(out LoopSoundPlayer loopSoundPlayer))
                    {
                        UnityEngine.Object.Destroy(loopSoundPlayer);
                    }

                    if (tetherPrefab.TryGetComponent(out LineRenderer line))
                    {
                        line.widthMultiplier = 2;

                        Material mat = UnityEngine.Object.Instantiate(line.sharedMaterials[0]);
                        mat.SetColor("_TintColor", new Color32(255, 241, 168, 255));
                        mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampMissileTrail_png).WaitForCompletion());
                        mat.SetTexture("_Cloud2Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_TiledTextures.texCloudIce_png).WaitForCompletion());


                        Material mat2 = UnityEngine.Object.Instantiate(line.sharedMaterials[1]);
                        mat2.SetColor("_TintColor", new Color32(255, 255, 255, 255));
                        mat2.SetTexture("_MainTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidJailer.texVoidJailerTentacleMask2_png).WaitForCompletion());
                        line.sharedMaterials = new Material[] { mat, mat };
                    }

                    Transform endTransform = tetherPrefab.transform.GetChild(0);
                    if (endTransform)
                    {
                        Transform light = endTransform.transform.GetChild(0);
                        if (light)
                        {
                            UnityEngine.GameObject.Destroy(light.gameObject);
                        }
                        Transform healedFx = endTransform.transform.GetChild(1);
                        if (healedFx)
                        {
                            UnityEngine.GameObject.Destroy(healedFx.gameObject);
                        }
                    }
                });
            }

            Transform activeVfx = howlingRallyBodyAttachment.transform.GetChild(0);

            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Elites_EliteBead.EliteBeadTether_prefab, (earthTetherVfx) =>
            {
                tetherVfxPrefab = earthTetherVfx.InstantiateClone("AffixSquallTetherVfx2");
                controller.activeVfx = tetherVfxPrefab;
            });

            Modules.Content.AddNetworkedObjectPrefab(howlingRallyBodyAttachment);
        }

        public override void Hooks()
        {
            GetStatCoefficients += HowlingStats;
            OnBodyHealthGateTriggeredGlobal += HowlingRetaliate;
            On.RoR2.CharacterBody.AddOrRemoveEliteItemBehavior += AddAffixBehavior;
            On.RoR2.HealthComponent.TakeDamage += CycloneBlock;

            On.RoR2.CharacterAI.BaseAI.EvaluateSkillDrivers += LeaderSquallOverride;
            On.EntityStates.AI.Walker.Combat.FixedUpdate += HowlingConvergeCombat2;
            On.EntityStates.AI.Walker.Wander.FixedUpdate += HowlingConvergeWander2;
            On.EntityStates.AI.Walker.LookBusy.FixedUpdate += HowlingConvergeLookBusy2;
            //RemoveOspForever();
        }

        private void HowlingRetaliate(CharacterBody sender)
        {
            if (IsElite(sender))
            {
                if(sender.TryGetComponent(out AffixSquallBehavior behavior))
                {
                    behavior.QueueMissiles(missileCtBase + Mathf.FloorToInt((float)missileCtPerSize * sender.radius));
                }
            }
        }

        #region ai overriding
        private BaseAI.SkillDriverEvaluation LeaderSquallOverride(On.RoR2.CharacterAI.BaseAI.orig_EvaluateSkillDrivers orig, BaseAI self)
        {
            if (self.body != null && self.body.HasBuff(StormsCore.CycloneLeader))
            {
                if (CycloneController.instance != null
                    && CycloneController.instance.cycloneState >= CycloneController.CycloneState.PreparingSquall
                    && CycloneController.instance.HowlSquallDriver != null)
                {
                    self.UpdateTargets();
                    if(CycloneController.squallTargetBody == null || IsBodySheltered(CycloneController.squallTargetBody))
                    {
                        CycloneController.squallTargetBody = null;
                        CharacterBody body;
                        IEnumerable<CharacterMaster> masterCandidates = CharacterMaster.instancesList
                            .Where(x => x.teamIndex == TeamIndex.Player
                            && (body = x.GetBody()) != null
                            && body.isPlayerControlled == true
                            && IsBodySheltered(body) == false
                            );
                        if (masterCandidates.Count() > 0)
                        {
                            CycloneController.squallTargetBody = 
                                masterCandidates
                                .Select(x => x.GetBody())
                                .OrderByDescending(x => (x.corePosition - self.body.corePosition))
                                .FirstOrDefault()
                                ;

                            GameObject leaderBodyObject = self.master.GetBodyObject();
                            if(leaderBodyObject && leaderBodyObject.TryGetComponent(out AffixSquallBehavior squall))
                            {
                                squall.OnTargetUpdated();
                            }
                        }
                    }
                    if (CycloneController.squallTargetBody != null)
                        self.customTarget.gameObject = CycloneController.squallTargetBody.gameObject;

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
            if (IsElite(self.body))
            {
                if (CycloneController.GetShouldConverge())
                {
                    BaseAIState nextState = new Converge();
                    self.outer.SetNextState(nextState);
                    return;
                }
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
        #region ai overriding 2
        public static void RemoveOspForever()
        {
            // removes one-shot protection (OSP)
            Hook hookTuah = new Hook(
              typeof(EntityStates.AI.Walker.Combat).GetMethod("FixedUpdate", (BindingFlags)(-1)),
              typeof(WhirlwindAspect).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
        }

        public static void ReflectOnThatThang(orig_aiStateFixedUpdate orig, BaseAIState self)
        {

        }
        public delegate bool orig_aiStateFixedUpdate(BaseAIState self);
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
            if (slot.characterBody && slot.characterBody.TryGetComponent(out AffixSquallBehavior squallBehavior))
            {
                squallBehavior.SetSquallTimer(playerSquallDuration);
                return true;
            }
            return false;
        }
    }

    public class AffixSquallBehavior : BaseStormEliteBehavior
    {
        public enum FiringState
        {
            Off,
            Aiming,
            Firing
        }
        private Queue<Vector3> targetFixedPositions = new Queue<Vector3>();
        Vector3 lastFixedUpdatePosition;
        Vector3 currentFixedUpdatePosition;
        private float lastTargetPositionUpdateTimestamp = float.NegativeInfinity;
        float aimLerp => timeSinceLastTargetPositionUpdate / (isRetargeting ? WhirlwindAspect.squallAimDamping : Time.fixedDeltaTime);
        float timeSinceLastTargetPositionUpdate => Time.time - lastTargetPositionUpdateTimestamp;
        private float targetAcquiredFixedTimestamp = float.NegativeInfinity;
        float timeSinceTargetAcquired => Time.time - targetAcquiredFixedTimestamp;
        bool isRetargeting => timeSinceTargetAcquired < WhirlwindAspect.squallAimDamping;
        public static bool GetShouldConverge()
        {
            return CycloneController.GetShouldConverge();
        }
        private static List<AffixSquallBehavior> instancesList = new List<AffixSquallBehavior>();
        public static ReadOnlyCollection<AffixSquallBehavior> readOnlyInstancesList = new ReadOnlyCollection<AffixSquallBehavior>(AffixSquallBehavior.instancesList);
        private GameObject affixSquallAttachment;

        int missilesQueued = 0;
        float missileInterval = 0f;
        float missileCountdown = 0f;
        /// <summary>
        /// squall timer is used exclusively for howling elite players
        /// </summary>
        float squallTimer;
        bool isPlayer;
        bool isPlayerTeam;
        BaseAI baseAI;
        bool hasAuthority;
        private GameObject beamVfxInstance;
        private LoopSoundManager.SoundLoopPtr loopPtr;

        FiringState _firingState = FiringState.Off;
        public FiringState firingState
        {
            get
            {
                return _firingState;
            }
            set
            {
                if (_firingState != value)
                    UpdateFiringState(value);
                _firingState = value;
            }
        }

        public void OnTargetUpdated()
        {
            UpdateAimPosition(reset: true);
        }

        public Ray GetBeamRay()
        {
            Vector3 targetPosition = Vector3.Lerp(lastFixedUpdatePosition, currentFixedUpdatePosition, aimLerp);

            if (body == null)
            {
                if (CycloneController.instance != null && CycloneController.instance.leaderElite == this)
                {
                    CycloneController.DemoteCurrentLeader();
                }
                UpdateFiringState(FiringState.Off);
                firingState = FiringState.Off;
                return new Ray(transform.position, isPlayer ? transform.forward : PositionToDirection(transform.position, targetPosition));
            }

            if (body.inputBank)
            {
                return new Ray(body.inputBank.aimOrigin, isPlayer ? body.inputBank.aimDirection : PositionToDirection(body.inputBank.aimOrigin, targetPosition));
            }
            return new Ray(transform.position, isPlayer ? transform.forward : PositionToDirection(transform.position, targetPosition));

            Vector3 PositionToDirection(Vector3 origin, Vector3 position)
            {
                return (position - origin).normalized;
            }
        }

        private void UpdateBeamTransform()
        {
            if(CycloneController.squallTargetBody == null 
                || CycloneController.instance == null 
                || CycloneController.instance.cycloneState != CycloneController.CycloneState.FiringSquall 
                || CycloneController.instance.leaderElite == null)
            {
                Debug.LogError("UpdateBeamTransform demote");
                firingState = FiringState.Off;
                CycloneController.DemoteCurrentLeader();
                return;
            }

            Ray beamRay = this.GetBeamRay();
            this.beamVfxInstance.transform.SetPositionAndRotation(beamRay.origin, Quaternion.LookRotation(beamRay.direction));
        }

        private void UpdateFiringState(FiringState newState)
        {
            bool wasOn = _firingState != FiringState.Off;
            bool newOn = newState != FiringState.Off;
            //Debug.Log($"Updating firing state, {_firingState.ToString()} to {newState.ToString()}");
            if (wasOn == true)
            {
                //if was firing
                if (_firingState == FiringState.Firing)
                {
                    VfxKillBehavior.KillVfxObject(this.beamVfxInstance);
                    LoopSoundManager.StopSoundLoopLocal(this.loopPtr);
                }
                else
                {
                    Destroy(this.beamVfxInstance);
                }
                this.beamVfxInstance = null;
                //if will not be firing
                if (newOn == false)
                {
                    UpdateAimPosition(reset: true);
                    RoR2Application.onLateUpdate -= this.UpdateBeamTransform;
                }
            }

            if(newOn == true)
            {
                GameObject beamVfxPrefab = null;
                if (newState == FiringState.Firing)
                {
                    beamVfxPrefab = WhirlwindAspect.squallBeamVfxPrefab;
                    this.loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, EntityStates.VoidRaidCrab.SpinBeamAttack.loopSound);
                }
                else
                {
                    beamVfxPrefab = WhirlwindAspect.squallPreBeamVfxPrefab;
                }

                this.beamVfxInstance = UnityEngine.Object.Instantiate<GameObject>(beamVfxPrefab);
                this.beamVfxInstance.transform.SetParent(body.aimOriginTransform, true);

                if(wasOn == false)
                {
                    UpdateAimPosition(true);
                    RoR2Application.onLateUpdate += this.UpdateBeamTransform;
                }
                this.UpdateBeamTransform();
            }
        }

        void OnEnable()
        {
            instancesList.Add(this);
        }

        void OnDisable()
        {
            UpdateFiringState(FiringState.Off);
            if (instancesList.Contains(this))
            {
                instancesList.Remove(this);
            }
        }

        void Start()
        {
            this.targetFixedPositions = new Queue<Vector3>();
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

            if (missilesQueued > 0)
            {
                if (missileCountdown > 0)
                    missileCountdown -= Time.fixedDeltaTime;

                if (missileCountdown <= 0)
                    FireMissile();
            }

            DoBodyAttachment();

            DoBeamAttackServer();
        }

        float beamTickTimer = 0f;
        bool hasUpdatedThisFrame = false;
        private void UpdateAimPosition(bool reset)
        {
            if (isPlayer)
                return;
            lastTargetPositionUpdateTimestamp = Time.fixedTime;

            if(isRetargeting == false)
                lastFixedUpdatePosition = currentFixedUpdatePosition;

            if (reset == true)
            {
                targetFixedPositions.Clear();
                currentFixedUpdatePosition = GetTargetPosition();
                targetAcquiredFixedTimestamp = Time.time;
            }
            else if (isRetargeting == false)
            {
                currentFixedUpdatePosition = targetFixedPositions.Count > 0 ? targetFixedPositions.Dequeue() : GetTargetPosition();
            }

            //Debug.Log($"Player current position [{GetTargetPosition()}] " +
            //    $"Last Aim position [{currentFixedUpdatePosition}] " +
            //    $"Next Aim position [{currentFixedUpdatePosition}] " +
            //    $"Positions queued [{targetFixedPositions.Count}] " +
            //    $"Is retargeting [{isRetargeting}] " +
            //    $"Is resetting [{reset}]");
            if (firingState == FiringState.Off || CycloneController.squallTargetBody == null)
                return;
            targetFixedPositions.Enqueue(GetTargetPosition());

            Vector3 GetTargetPosition()
            {
                if (CycloneController.squallTargetBody != null)
                    return CycloneController.squallTargetBody.corePosition;
                return currentFixedUpdatePosition;
            }
        }
        private void DoBeamAttackServer()
        {
            if (squallTimer > 0)
            {
                squallTimer -= Time.fixedDeltaTime;
                firingState = squallTimer > 0 ? FiringState.Firing : FiringState.Off;
            }
            if (firingState == FiringState.Off)
            {
                beamTickTimer = 1f / WhirlwindAspect.squallBeamTickFrequency;
                return;
            }

            UpdateAimPosition(reset: false);

            beamTickTimer -= Time.fixedDeltaTime;
            if (beamTickTimer > 0)
                return;
            beamTickTimer += 1f / WhirlwindAspect.squallBeamTickFrequency;

            Ray beamRay = GetBeamRay();
            //this is here again because getbeamray has the magical ability to turn off firing :D
            if (firingState != FiringState.Firing)
                return;
            DamageTypeCombo damageType = new DamageTypeCombo();
            damageType.damageSource = DamageSource.Equipment;
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
                damage = WhirlwindAspect.squallDamagePerSecond * Tools.GetAmbientLevelScalar(WhirlwindAspect.squallDamagePerLevel) / WhirlwindAspect.squallBeamTickFrequency,
                damageColorIndex = DamageColorIndex.Default,
                damageType = damageType,
                falloffModel = BulletAttack.FalloffModel.None,
                force = 0f,
                hitEffectPrefab = null,// EntityStates.VoidRaidCrab.SpinBeamAttack.beamImpactEffectPrefab,
                tracerEffectPrefab = null,
                isCrit = false,
                HitEffectNormal = false
            }.Fire();
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

        internal void SetSquallTimer(float playerSquallDuration)
        {
            if (body.teamComponent.teamIndex != TeamIndex.Player)
                return;
            squallTimer = playerSquallDuration;
        }

        internal void QueueMissiles(int ct)
        {
            missilesQueued = ct;
            missileInterval = 1f / (float)missilesQueued;
            FireMissile();
        }

        private void FireMissile()
        {
            if (!NetworkServer.active)
                return;
            if (body == null || body.healthComponent == null || body.healthComponent.alive == false)
                return;

            missilesQueued--;
            missileCountdown = missileInterval;

            float missileDamage = WhirlwindAspect.missileDamageBase * Tools.GetAmbientLevelScalar(WhirlwindAspect.missileDamagePerLevel);
            MissileUtils.FireMissile(
                body.corePosition, body,
                default(ProcChainMask), victim: body.healthComponent.lastHitAttacker,
                missileDamage, Util.CheckRoll(body.crit),
                WhirlwindAspect.howlWindMissilePrefab,
                DamageColorIndex.Item, addMissileProc: true);
        }
    }
}
