using EntityStates;
using R2API;
using RainrotSharedUtils.Difficulties;
using RainrotSharedUtils.Shelters;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Components;
using SwanSongExtended.Elites;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.DamageAPI;
using static RoR2.CombatDirector;
using static MoreStats.StatHooks;
using R2API.Networking;

namespace SwanSongExtended.Storms
{
    public static class StormsCore
    {
        public static bool IsStormDamage(DamageInfo damageInfo, CharacterBody attackerBody = null)
        {
            if (damageInfo.damageType.HasModdedDamageType(stormDamageType))
                return true;
            if (attackerBody != null && attackerBody.IsStormElite())
                return true;
            return false;
        }
        public static bool stormsEnabled = true;
        public const string stormShelterObjectiveToken = "OBJECTIVE_SHELTER";
        public const string wishboneObjectiveToken = "OBJECTIVE_WISHBONE";
        public static GameObject StormsRunBehaviorPrefab;
        public static GameObject StormsControllerPrefab;
        public const string esmStormName = "StormMain";
        public const string esmCycloneName = "Cyclone";

        //storm combat:
        public static EliteTierDef StormEliteT1;
        public static EliteTierDef StormEliteT2;
        public static BuffDef StormEliteWeak;
        public static float stormDirectorCreditStimulus = 35f;
        public static float stormDirectorCreditGainMultiplier = 0.3f;
        public static float stormDirectorSpawnIntervalMin = 22.5f; //12.5f
        public static float stormDirectorSpawnIntervalMax = 37.5f; //22.5f
        public static int stormEliteHealthGateCountBase = 1;
        public static int stormEliteHealthGateCountPerSize = 1;
        public static float stormEliteHealthGateDurationBase = 2.0f;
        public static float stormEliteHealthGateDurationPerSize = 0.5f;

        //storm scheduling:
        public const float drizzleStormDelayMinutes = 10;
        public const float drizzleStormWarningMinutes = 3;
        public const float rainstormStormDelayMinutes = 7;
        public const float rainstormStormWarningMinutes = 2;
        public const float monsoonStormDelayMinutes = 4f;
        public const float monsoonStormWarningMinutes = 1f;
        public const float stormMaxRandomDelayMinutes = 0.5f;
        public const float firstStageStormDelayMinutes = 1f;
        public const float stormStrengthIncreaseTimerSeconds = 90;
        public const float stormStrengthIncreasePerDifficulty = 0.15f;
        public const float stormStrengthIncreaseBase = 0.1f;

        //meteors:
        public static GameObject meteorDelayBlastPrefab;
        public static GameObject meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Meteor.MeteorStrikePredictionEffect_prefab).WaitForCompletion();
        public static GameObject meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Meteor.MeteorStrikeImpact_prefab).WaitForCompletion();
        public static float waveMinInterval = 0.8f;
        public static float waveMaxInterval = 1.2f;
        public static float waveMissChance = 0.6f;
        public static float meteorTargetEnemyChance = 15f;
        public static float meteorTravelEffectDuration = 0f;
        public static float meteorImpactDelay = 2.5f;
        public static float meteorBlastDamageCoefficient = 13;
        public static float meteorBlastDamageScalarPerLevel = 0.5f;
        public static float meteorBlastRadius = 10;
        public static float meteorBlastForce = 0;
        public static float shelterPerimeterStrikeGap = 20;
        public static BlastAttack.FalloffModel meteorFalloffModel = BlastAttack.FalloffModel.None;
        public static ModdedDamageType stormDamageType;

        //cyclones:
        public static BuffDef CycloneProtection;
        public static BuffDef CycloneLeader;
        public static GameObject cycloneWardPrefab;
        public static Material cycloneMaterial;
        public static bool isCycloneShelter = true;
        public static float cycloneRadius = 20f;
        public static float cycloneDuration = 999f;
        public static float squallReelectionInterval = 1f;
        public static float squallReelectionRallyTimeLoss = 0.5f;
        public static float squallRallyTimeMin = 5f;
        public static float squallRallyTimeMax = 11f;
        public static float squallRallyContributorThreshold = 8f;
        public static float squallFireDurationMin = 4f;
        public static float squallFireDurationBonusPerOverspill = 2f;

        public static void Init()
        {
            DifficultyUtilsModule.DisplayCurrentStageTime = true;
            ShelterUtilsModule.UseGlobalShelters = true;
            RoR2Application.onLoad += AddDifficultyStats;
            stormDamageType = ReserveDamageType();
            CreateStormEliteTiers();
            CreateStormsRunBehaviorPrefab();
            LanguageAPI.Add(stormShelterObjectiveToken, "Seek <style=cDeath>shelter <sprite name=\"TP\" tint=1></style> from the Storm!");
            LanguageAPI.Add(wishboneObjectiveToken, "Collect <style=cIsDamage>Wishbones</style>");

            //On.RoR2.HoldoutZoneController.OnEnable += RegisterHoldoutZone;
            //On.RoR2.HoldoutZoneController.OnDisable += UnregisterHoldoutZone;

            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.GenericDelayBlast_prefab, CreateMeteorDelayBlast);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RandomDamageZone.DamageZoneWard_prefab, CreateCycloneWard);
            
            LanguageAPI.Add($"OBJECTIVE_METEORDEFAULT_2R4R", "Meteor Storm Imminent");
            LanguageAPI.Add($"OBJECTIVE_LIGHTNING_2R4R", "Thunderstorm Imminent");
            LanguageAPI.Add($"OBJECTIVE_FIRE_2R4R", "Fire Storm Imminent");
            LanguageAPI.Add($"OBJECTIVE_COLD_2R4R", "Blizzard Imminent");
            //LanguageAPI.Add($"OBJECTIVE_METEORDEFAULT_2R4R", "");

            GetMoreStatCoefficients += StormEliteHealthGates;
            OnBodyHealthGateTriggeredGlobal += OnStormEliteHealthGateTriggered;

            NetworkingAPI.RegisterMessageType<SyncStormApproach>();

            CycloneLeader = Content.CreateAndAddBuff(
                "bdCycloneLeader",
                null,//Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.texBuffCloakIcon_tif).WaitForCompletion(),
                Color.gray,
                canStack: false,
                isDebuff: false,
                isHidden: true
                );
            CycloneProtection = Content.CreateAndAddBuff(
                "bdCycloneProtection",
                null,//Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.texBuffCloakIcon_tif).WaitForCompletion(),
                Color.gray,
                canStack: false,
                isDebuff: false,
                isHidden: true
                );
            SwanSongPlugin.LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.bdSmallArmorBoost_asset, (bd) =>
            {
                CycloneProtection.iconSprite = bd.iconSprite;
            });
            StormEliteWeak = Content.CreateAndAddBuff(
                "bdStormEliteWeak",
                null,//Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.texBuffCloakIcon_tif).WaitForCompletion(),
                Color.gray,
                canStack: false,
                isDebuff: false
                );
            SwanSongPlugin.LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.bdCloak_asset, (bd) =>
            {
                StormEliteWeak.iconSprite = bd.iconSprite;
            });
        }

        private static void OnStormEliteHealthGateTriggered(CharacterBody sender)
        {
            if (!NetworkServer.active)
                return;
            if (sender.IsStormElite())
            {
                sender.AddTimedBuff(RoR2Content.Buffs.Immune, stormEliteHealthGateDurationBase + Mathf.Max(0, sender.radius - 1) * stormEliteHealthGateDurationPerSize);
            }
        }

        private static void StormEliteHealthGates(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (!sender.IsStormElite())
                return;
            args.ModifyHealthGateCount(stormEliteHealthGateCountBase + Mathf.CeilToInt(sender.radius - 1f) * Mathf.Max(0, stormEliteHealthGateCountPerSize));
        }

        private static void CreateCycloneWard(GameObject damageZoneWard)
        {
            cycloneWardPrefab = damageZoneWard.InstantiateClone("StormCycloneWard", true);
            Content.AddNetworkedObjectPrefab(cycloneWardPrefab);

            VerticalTubeZone tubeZone = cycloneWardPrefab.AddComponent<VerticalTubeZone>();
            tubeZone.radius = cycloneRadius;
            tubeZone.indicatorSmoothTime = 1.0f;
            if (isCycloneShelter)
            {
                ShelterProviderBehavior shelterProvider = cycloneWardPrefab.AddComponent<ShelterProviderBehavior>();
                shelterProvider.fallbackRadius = cycloneRadius;
                shelterProvider.zoneBehavior = tubeZone;
            }

            if (cycloneWardPrefab.TryGetComponent(out BuffWard buffWard))
            {
                buffWard.radius = cycloneRadius;
                buffWard.buffDef = CycloneProtection;
                buffWard.interval = 0.5f;
                buffWard.buffDuration = 0.6f;
                buffWard.expireDuration = cycloneDuration;
                buffWard.shape = BuffWard.BuffWardShape.VerticalTube;
                buffWard.requireGrounded = false;
                buffWard.animateRadius = false;
                tubeZone.rangeIndicator = buffWard.rangeIndicator;
            }

            if(cycloneWardPrefab.TryGetComponent(out Deployable dep))
            {
                UnityEngine.Object.Destroy(dep);
            }

            Transform shrinker = cycloneWardPrefab.transform.GetChild(1);
            //totem
            shrinker.GetChild(0).gameObject.SetActive(false);
            //decal
            shrinker.GetChild(1).gameObject.SetActive(false);

            Transform indicator = shrinker.GetChild(2);
            indicator.transform.localScale = Vector3.one * cycloneRadius;
            //indicator sphere
            indicator.GetChild(0).gameObject.SetActive(false);
            //decal_aoe
            indicator.GetChild(1).gameObject.SetActive(false);

            SwanSongPlugin.LoadAsync<GameObject>(
            RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GameModes_InfiniteTowerRun_ITAssets.InfiniteTowerSafeWardAwaitingInteraction_prefab,
            (itSafeWard) =>
            {
                GameObject verticalWard = itSafeWard.transform.Find("Indicator")?.gameObject;
                GameObject cycloneIndicator = PrefabAPI.InstantiateClone(verticalWard, "CycloneIndicatorPrefab");

                SwanSongPlugin.LoadAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_WardOnLevel.matWarbannerSphereIndicator2_mat, (matWarbanner) =>
                {
                    cycloneMaterial = UnityEngine.Object.Instantiate(matWarbanner);
                    cycloneMaterial.SetColor("_TintColor", new Color32(168, 120, 90, 110)/*(150, 110, 0, 191)*/);
                    cycloneMaterial.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_conduitcanyon.texCCTreeRamp4_png).WaitForCompletion());
                    cycloneMaterial.SetTexture("_Cloud2Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.texCloudGradient_png).WaitForCompletion());
                    cycloneMaterial.SetFloat("_Boost", 0.776f); //0.34
                    cycloneMaterial.SetFloat("_RimPower", 1.206f);
                    cycloneMaterial.SetFloat("_RimStrength", 0.828f);
                    cycloneMaterial.SetFloat("_TriplanarOn", 0);

                    MeshRenderer mr = cycloneIndicator.GetComponentInChildren<MeshRenderer>(includeInactive: false);
                    if (mr)
                    {
                        mr.sharedMaterials = new Material[1] { cycloneMaterial };
                        mr.material = cycloneMaterial;
                    }
                });

                cycloneIndicator.transform.parent = indicator;
                cycloneIndicator.transform.localPosition = Vector3.zero;
                cycloneIndicator.transform.rotation = Quaternion.identity;
                cycloneIndicator.transform.localScale = Vector3.one;
            });
        }

        private static void CreateMeteorDelayBlast(GameObject delayBlastPrefab)
        {
            meteorDelayBlastPrefab = delayBlastPrefab.InstantiateClone("StormStrikeDelayBlastProjectile", true);

            if(meteorDelayBlastPrefab.TryGetComponent(out NetworkIdentity netId))
            {
                netId.localPlayerAuthority = true;
            }
            if(meteorDelayBlastPrefab.TryGetComponent(out DelayBlast delayBlast))
            {
                delayBlast.damageType.AddModdedDamageType(stormDamageType);
            }

            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Meteor.MeteorStrikePredictionEffect_prefab, (predictionEffect) =>
            {
                meteorWarningEffectPrefab = predictionEffect.InstantiateClone("StormStrikePredictionEffect");
                meteorWarningEffectPrefab.transform.localScale = new Vector3(meteorBlastRadius * 0.85f, meteorBlastRadius * 5, meteorBlastRadius * 0.85f);
                DestroyOnTimer DOT = meteorWarningEffectPrefab.GetComponent<DestroyOnTimer>();
                if (DOT)
                {
                    DOT.duration = meteorImpactDelay + 1f;
                }
                Transform indicator = meteorWarningEffectPrefab.transform.Find("GroundSlamIndicator");
                if (indicator)
                {
                    AnimateShaderAlpha asa = indicator.GetComponent<AnimateShaderAlpha>();
                    if (asa)
                    {
                        asa.timeMax = meteorImpactDelay + 0.1f;
                    }
                    MeshRenderer meshRenderer = indicator.GetComponent<MeshRenderer>();
                    if (meshRenderer)
                    {
                        Material mat = UnityEngine.Object.Instantiate(meshRenderer.material);
                        mat.name = "matStormStrikeImpactIndicator";
                        meshRenderer.material = mat;
                        mat.SetFloat("_Boost", 0.64f);
                        mat.SetFloat("_AlphaBoost", 4.29f);
                        mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampArtifactShellSoft.png").WaitForCompletion());
                        mat.SetColor("_TintColor", Color.white);
                    }
                }

                Content.CreateAndAddEffectDef(meteorWarningEffectPrefab);
            });
        }

        private static void AddDifficultyStats()
        {
            for (int i = (int)DifficultyIndex.Easy; i < (int)DifficultyIndex.Count; i++)
            {
                DifficultyIndex difficultyIndex = (DifficultyIndex)i;
                DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficultyIndex);
                MoreDifficultyStats.StartingDifficulty startingDifficulty = 0;
                float desiredStormTime = -1;
                float desiredStormWarningTime = -1;
                float stormIntensifyStrength = -1;
                bool delayFirstStorm = true;

                switch (difficultyIndex)
                {
                    case DifficultyIndex.Easy:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Easy;
                        desiredStormTime = drizzleStormDelayMinutes;
                        desiredStormWarningTime = drizzleStormWarningMinutes;
                        stormIntensifyStrength = stormStrengthIncreaseBase + stormStrengthIncreasePerDifficulty * 1;
                        break;
                    case DifficultyIndex.Normal:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Medium;
                        desiredStormTime = rainstormStormDelayMinutes;
                        desiredStormWarningTime = rainstormStormWarningMinutes;
                        stormIntensifyStrength = stormStrengthIncreaseBase + stormStrengthIncreasePerDifficulty * 2;
                        break;
                    case DifficultyIndex.Hard:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Hard;
                        desiredStormTime = monsoonStormDelayMinutes;
                        desiredStormWarningTime = monsoonStormWarningMinutes;
                        stormIntensifyStrength = stormStrengthIncreaseBase + stormStrengthIncreasePerDifficulty * 3;
                        break;
                    //assumes eclipse
                    default:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Hard;
                        desiredStormTime = monsoonStormDelayMinutes;
                        desiredStormWarningTime = monsoonStormWarningMinutes;
                        stormIntensifyStrength = stormStrengthIncreaseBase + stormStrengthIncreasePerDifficulty * 3;
                        break;
                }

                MoreDifficultyStats difficultyStats = DifficultyUtilsModule.GetMoreDifficultyStats(difficultyIndex);
                difficultyStats.desiredStormTime_ForSwanSong = desiredStormTime;
                difficultyStats.desiredStormWarningTime_ForSwanSong = desiredStormWarningTime;
                difficultyStats.delayFirstStorm_ForSwanSong = delayFirstStorm;
                difficultyStats.stormIntensifyStrength_ForSwanSong = stormIntensifyStrength;
                DifficultyUtilsModule.difficultyCustomStats[difficultyIndex] = difficultyStats;
            }
        }

        private static void CreateStormEliteTiers()
        {
            StormEliteT1 = new EliteTierDef();
            StormEliteT1.costMultiplier = 2;
            StormEliteT1.canSelectWithoutAvailableEliteDef = false;
            StormEliteT1.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormRunBehavior.instance && StormRunBehavior.hasBegunStorm);
            StormEliteT1.eliteTypes = new EliteDef[0];
            EliteAPI.AddCustomEliteTier(StormEliteT1);

            StormEliteT2 = new EliteTierDef();
            StormEliteT2.costMultiplier = 2;
            StormEliteT2.canSelectWithoutAvailableEliteDef = false;
            StormEliteT2.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormRunBehavior.instance && StormRunBehavior.hasBegunStorm &&
                    !SwanSongPlugin.is2R4RLoaded ? (Run.instance.loopClearCount > 0) :
                    ((Run.instance.stageClearCount >= 10 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty <= DifficultyIndex.Easy)
                    || (Run.instance.stageClearCount >= 5 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Normal)
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Hard)
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty > DifficultyIndex.Hard)));
            StormEliteT2.eliteTypes = new EliteDef[0];
            //EliteAPI.AddCustomEliteTier(StormT2);
        }

        private static void CreateStormsRunBehaviorPrefab()
        {
            //storm run behavior prefab is instantiated by the game automatically during the run as long as the expansion is enabled
            StormsRunBehaviorPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/DLC1RunBehavior.prefab").WaitForCompletion().InstantiateClone("2R4RExpansionRunBehavior", true);

            ExpansionRequirementComponent erc = StormsRunBehaviorPrefab.GetComponent<ExpansionRequirementComponent>();
            erc.requiredExpansion = SwanSongPlugin.expansionDefSS2;

            StormsRunBehaviorPrefab.AddComponent<StormRunBehavior>();

            SwanSongPlugin.expansionDefSS2.runBehaviorPrefab = StormsRunBehaviorPrefab;

            //storm controller prefab is instantiated by the run behavior prefab only on stages that have storms
            StormsControllerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion().InstantiateClone("2R4RStormController", true);
            MonoBehaviour[] components = StormsControllerPrefab.GetComponentsInChildren<MonoBehaviour>();
            bool directorInstanceFound = false;
            foreach (MonoBehaviour component in components)
            {
                if (component is CombatDirector cd && directorInstanceFound == false)
                {
                    cd.creditMultiplier = stormDirectorCreditGainMultiplier;
                    cd.eliteBias = 0;
                    cd.maximumNumberToSpawnBeforeSkipping = 6;
                    cd.expRewardCoefficient = 1f;
                    cd.goldRewardCoefficient = 0f;
                    cd.teamIndex = TeamIndex.Monster;
                    //duration between monster waves
                    cd.minRerollSpawnInterval = stormDirectorSpawnIntervalMin;
                    cd.maxRerollSpawnInterval = stormDirectorSpawnIntervalMax;

                    directorInstanceFound = true;
                    cd.onSpawnedServer.AddPersistentListener(OnStormDirectorSpawnServer);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }

            EntityStateMachine esmStorm = StormsControllerPrefab.AddComponent<EntityStateMachine>();
            esmStorm.customName = esmStormName;
            esmStorm.initialStateType = new SerializableEntityStateType(typeof(StormController.StormApproach));
            esmStorm.mainStateType = new SerializableEntityStateType(typeof(StormController.StormApproach));
            StormsControllerPrefab.AddComponent<StormController>();
            //hi hiiiii
            EntityStateMachine esmCyclone = StormsControllerPrefab.AddComponent<EntityStateMachine>();
            esmCyclone.customName = esmCycloneName;
            esmCyclone.initialStateType = new SerializableEntityStateType(typeof(CycloneController.PrepareCyclone));
            esmCyclone.mainStateType = new SerializableEntityStateType(typeof(CycloneController.PrepareCyclone));
            StormsControllerPrefab.AddComponent<CycloneController>();

            StormsControllerPrefab.AddComponent<NetworkIdentity>();
            StormsControllerPrefab.AddComponent<NetworkStateMachine>().stateMachines = new EntityStateMachine[] { esmStorm };//, esmCyclone };

            Content.AddNetworkedObjectPrefab(StormsRunBehaviorPrefab);
            Content.AddNetworkedObjectPrefab(StormsControllerPrefab);
            Content.AddEntityState(typeof(StormController.IdleState));
            Content.AddEntityState(typeof(StormController.StormApproach));
            Content.AddEntityState(typeof(StormController.StormWarning));
            Content.AddEntityState(typeof(StormController.StormActive));
            Content.AddEntityState(typeof(CycloneController.Idle));
            Content.AddEntityState(typeof(CycloneController.PrepareCyclone));
            Content.AddEntityState(typeof(CycloneController.ElectLeader));
            Content.AddEntityState(typeof(CycloneController.PrepareSquall));
            Content.AddEntityState(typeof(CycloneController.FireSquall));

            void OnStormDirectorSpawnServer(GameObject masterObject)
            {
                int surgingCount = AffixFloodBehavior.readOnlyInstancesList.Count;
                int howlingCount = AffixSquallBehavior.readOnlyInstancesList.Count;

                EliteDef eliteDef = SurgingAspect.instance.EliteDef;
                if (surgingCount > 0 && Util.CheckRoll0To1((surgingCount + 1) / (howlingCount + 1)))
                    eliteDef = WhirlwindAspect.instance.EliteDef;

                EquipmentIndex equipmentIndex = EquipmentIndex.None;
                if (eliteDef == null)
                    return;
                EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                equipmentIndex = ((eliteEquipmentDef != null) ? eliteEquipmentDef.equipmentIndex : EquipmentIndex.None);

                CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
                //GameObject bodyObject = component.GetBodyObject();
                //if (bodyObject)
                //{
                //    foreach (EntityStateMachine entityStateMachine in bodyObject.GetComponents<EntityStateMachine>())
                //    {
                //        entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                //    }
                //}
                if (equipmentIndex != EquipmentIndex.None)
                {
                    component.inventory.SetEquipmentIndex(equipmentIndex, false);
                }
            }
        }
    }
}
