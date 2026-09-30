using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using System.Linq;

namespace RainrotSharedUtils
{
    public static class CustomScavengers
    {
        internal static bool _hooksEnabled = false;
        public static void AddScavengerMaster(GameObject masterPrefab, float weight, ExpansionDef requiredExpansion = null)
        {
            MultiExpansionSpawnCard.MasterInfo info = new MultiExpansionSpawnCard.MasterInfo();
            info.masterPrefab = masterPrefab;
            info.requiredExpansion = requiredExpansion;
            info.weight = weight;
            CustomScavengers.masterInfos.Add(info);
        }
        private static List<MultiExpansionSpawnCard.MasterInfo> masterInfos = new List<MultiExpansionSpawnCard.MasterInfo>();

        private static bool _useCustomScavengers;
        public static bool UseCustomScavengers
        {
            get => _useCustomScavengers;
            set
            {
                if (value == true)
                    SetHooks();
                _useCustomScavengers = value;
            }
        }

        private static MultiExpansionSpawnCard spawnCard;

        private static bool _repopulateVanillaScavengers = true;
        /// <summary>
        /// default true, please set to false if you intend on changing the vanilla scavengers in any way, you will have to repopulate all of them yourself
        /// </summary>
        public static bool RepopulateVanillaScavengers
        {
            get => _repopulateVanillaScavengers;
            set
            {
                _repopulateVanillaScavengers = value;
            }
        }

        private static void SetHooks()
        {
            if (_hooksEnabled)
                return;
            _hooksEnabled = true;

            RoR2Application.onLoad += FinalizeSpawnCard;
            Run.onRunStartGlobal += RegenerateSpawnCard;
            On.RoR2.ScriptedCombatEncounter.Start += OverrideTwistedScavengerEncounter;
        }

        private static void OverrideTwistedScavengerEncounter(On.RoR2.ScriptedCombatEncounter.orig_Start orig, ScriptedCombatEncounter self)
        {
            if(SceneCatalog.GetSceneDefForCurrentScene().nameToken == "MAP_LIMBO_TITLE")
            {
                ScriptedCombatEncounter.SpawnInfo spawnInfo = new ScriptedCombatEncounter.SpawnInfo();
                spawnInfo.spawnCard = spawnCard;
                spawnInfo.explicitSpawnPosition = self.spawns[0].explicitSpawnPosition;
                self.spawns = new ScriptedCombatEncounter.SpawnInfo[1] { spawnInfo };
            }
            orig(self);
        }

        private static void RegenerateSpawnCard(Run runInstance)
        {
            spawnCard.RegenerateSelection();
        }

        private static void FinalizeSpawnCard()
        {
            RainrotSharedUtils.SharedUtilsPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_limbo.ScavLunarEncounter_prefab, (encounterObject) =>
            {
                if(encounterObject.TryGetComponent(out ScriptedCombatEncounter encounter))
                {
                    spawnCard = ScriptableObject.CreateInstance<MultiExpansionSpawnCard>();

                    spawnCard.masterInfos = masterInfos.ToArray();

                    spawnCard.sendOverNetwork = true;
                    spawnCard.hullSize = HullClassification.Golem;
                    spawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                    spawnCard.requiredFlags = RoR2.Navigation.NodeFlags.TeleporterOK;
                    spawnCard.directorCreditCost = 0;
                    spawnCard.occupyPosition = false;
                    spawnCard.noElites = true;
                    spawnCard.eliteRules = SpawnCard.EliteRules.Default;
                    spawnCard.forbiddenAsBoss = false;
                    CharacterSpawnCard orig = encounter.spawns[0].spawnCard as CharacterSpawnCard;
                    if(orig != null)
                    {
                        spawnCard.loadout = orig.loadout;
                        spawnCard._loadout = orig._loadout;
                    }

                    ScriptedCombatEncounter.SpawnInfo spawnInfo = new ScriptedCombatEncounter.SpawnInfo();
                    spawnInfo.spawnCard = spawnCard;
                    spawnInfo.explicitSpawnPosition = encounter.spawns[0].explicitSpawnPosition;
                    encounter.spawns = new ScriptedCombatEncounter.SpawnInfo[1] { spawnInfo };
                }
            });
        }
    }
}
