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

namespace SwanSongExtended.Storms
{
    public static class StormsCore
    {
        public static bool stormsEnabled = true;
        public const string stormShelterObjectiveToken = "OBJECTIVE_SHELTER";
        public const string wishboneObjectiveToken = "OBJECTIVE_WISHBONE";
        public static GameObject StormsRunBehaviorPrefab;
        public static GameObject StormsControllerPrefab;
        public static EliteTierDef StormEliteT1;
        public static EliteTierDef StormEliteT2;
        public static bool IsCharacterStormElite(CharacterBody body)
        {
            return false;
        }


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
        public static GameObject meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikePredictionEffect.prefab").WaitForCompletion();
        public static GameObject meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikeImpact.prefab").WaitForCompletion();
        public static float waveMinInterval = 0.7f;
        public static float waveMaxInterval = 1.0f;
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


        public static void Init()
        {
            ShelterUtilsModule.UseGlobalShelters = true;
            RoR2Application.onLoad += AddDifficultyStats;
            stormDamageType = ReserveDamageType();
            CreateStormEliteTiers();
            CreateStormsRunBehaviorPrefab();
            LanguageAPI.Add(stormShelterObjectiveToken, "Seek <style=cDeath>shelter <sprite name=\"TP\" tint=1></style> from the Storm!");
            LanguageAPI.Add(wishboneObjectiveToken, "Collect <style=cIsDamage>Wishbones</style>");

            //On.RoR2.HoldoutZoneController.OnEnable += RegisterHoldoutZone;
            //On.RoR2.HoldoutZoneController.OnDisable += UnregisterHoldoutZone;

            meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikePredictionEffect.prefab").WaitForCompletion().InstantiateClone("StormStrikePredictionEffect");
            meteorWarningEffectPrefab.transform.localScale =  new Vector3(meteorBlastRadius * 0.85f, meteorBlastRadius * 5, meteorBlastRadius * 0.85f);
            DestroyOnTimer DOT = meteorWarningEffectPrefab.GetComponent<DestroyOnTimer>();
            if (DOT)
            {
                DOT.duration = meteorImpactDelay + 0.5f;
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

            LanguageAPI.Add($"OBJECTIVE_METEORDEFAULT_2R4R", "Meteor Storm Imminent");
            LanguageAPI.Add($"OBJECTIVE_LIGHTNING_2R4R", "Thunderstorm Imminent");
            LanguageAPI.Add($"OBJECTIVE_FIRE_2R4R", "Fire Storm Imminent");
            LanguageAPI.Add($"OBJECTIVE_COLD_2R4R", "Blizzard Imminent");
            //LanguageAPI.Add($"OBJECTIVE_METEORDEFAULT_2R4R", "");
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

        private static void RegisterHoldoutZone(On.RoR2.HoldoutZoneController.orig_OnEnable orig, HoldoutZoneController self)
        {
            orig(self);
            if (!StormRunBehavior.holdoutZones.Contains(self))
                StormRunBehavior.holdoutZones.Add(self);
        }
        private static void UnregisterHoldoutZone(On.RoR2.HoldoutZoneController.orig_OnDisable orig, HoldoutZoneController self)
        {
            orig(self);
            if (StormRunBehavior.holdoutZones.Contains(self))
                StormRunBehavior.holdoutZones.Remove(self);
        }

        private static void CreateStormEliteTiers()
        {
            StormEliteT1 = new EliteTierDef();
            StormEliteT1.costMultiplier = 2;
            StormEliteT1.canSelectWithoutAvailableEliteDef = false;
            StormEliteT1.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormRunBehavior.instance && StormRunBehavior.instance.hasBegunStorm);
            StormEliteT1.eliteTypes = new EliteDef[0];
            //EliteAPI.AddCustomEliteTier(StormT1);

            StormEliteT2 = new EliteTierDef();
            StormEliteT2.costMultiplier = 2;
            StormEliteT2.canSelectWithoutAvailableEliteDef = false;
            StormEliteT2.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormRunBehavior.instance && StormRunBehavior.instance.hasBegunStorm &&
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
            StormsRunBehaviorPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/DLC1RunBehavior.prefab").WaitForCompletion().InstantiateClone("2R4RExpansionRunBehavior", true);

            ExpansionRequirementComponent erc = StormsRunBehaviorPrefab.GetComponent<ExpansionRequirementComponent>();
            erc.requiredExpansion = SwanSongPlugin.expansionDefSS2;

            StormsRunBehaviorPrefab.AddComponent<StormRunBehavior>();

            SwanSongPlugin.expansionDefSS2.runBehaviorPrefab = StormsRunBehaviorPrefab;

            StormsControllerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion().InstantiateClone("2R4RStormController", true);
            MonoBehaviour[] components = StormsControllerPrefab.GetComponentsInChildren<MonoBehaviour>();
            bool directorInstanceFound = false;
            foreach (MonoBehaviour component in components)
            {
                if (component is CombatDirector cd && directorInstanceFound == false)
                {
                    cd.creditMultiplier = 0.5f;
                    cd.expRewardCoefficient = 1f;
                    cd.goldRewardCoefficient = 1f;
                    cd.minRerollSpawnInterval = 15f;
                    cd.maxRerollSpawnInterval = 25f;
                    cd.teamIndex = TeamIndex.Monster;

                    directorInstanceFound = true;
                    cd.onSpawnedServer.AddListener(OnStormDirectorSpawnServer);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }

            EntityStateMachine esm = StormsControllerPrefab.AddComponent<EntityStateMachine>();
            esm.initialStateType = new SerializableEntityStateType(typeof(StormController.StormApproach));
            esm.mainStateType = new SerializableEntityStateType(typeof(StormController.StormApproach));
            StormsControllerPrefab.AddComponent<StormController>();
            StormsControllerPrefab.AddComponent<NetworkIdentity>();
            StormsControllerPrefab.AddComponent<NetworkStateMachine>().stateMachines = new EntityStateMachine[] { esm };

            Content.AddNetworkedObjectPrefab(StormsRunBehaviorPrefab);
            Content.AddNetworkedObjectPrefab(StormsControllerPrefab);
            Content.AddEntityState(typeof(StormController.IdleState));
            Content.AddEntityState(typeof(StormController.StormApproach));
            Content.AddEntityState(typeof(StormController.StormWarning));
            Content.AddEntityState(typeof(StormController.StormActive));

            void OnStormDirectorSpawnServer(GameObject masterObject)
            {
                EliteDef eliteDef = WhirlwindAspect.instance.EliteDef;
                if (Util.CheckRoll(50))
                    eliteDef = SurgingAspect.instance.EliteDef;

                EquipmentIndex equipmentIndex = EquipmentIndex.None;
                if (eliteDef != null)
                {
                    EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                    equipmentIndex = ((eliteEquipmentDef != null) ? eliteEquipmentDef.equipmentIndex : EquipmentIndex.None);
                }

                CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
                GameObject bodyObject = component.GetBodyObject();
                if (bodyObject)
                {
                    foreach (EntityStateMachine entityStateMachine in bodyObject.GetComponents<EntityStateMachine>())
                    {
                        entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                    }
                }
                if (equipmentIndex != EquipmentIndex.None)
                {
                    Log.Warning("Spawning Storm Elite: " + eliteDef.name);
                    component.inventory.SetEquipmentIndex(equipmentIndex);
                }
            }
        }
    }
}
