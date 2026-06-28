using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended.Items
{
    class VoidHappyMask : ItemBase<VoidHappyMask>
    {
        public override string ConfigName => "Items : Forgotten Facade";
        public float procChance = 7;
        public int baseInfestors = 2;
        public int stackInfestors = 1;
        public static SpawnCard infestorSpawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscVoidInfestor");
        public override string ItemName => "Forgotten Facade";

        public override string ItemLangTokenName => "VOIDHAPPIESTMASK";

        public override string ItemPickupDesc => "Chance on killing an enemy to summon void infestors. <style=cIsVoid>Corrupts all Happiest Masks.</style>";

        public override string ItemFullDescription => 
            $"Killing monsters has a " +
            $"<style=cIsDamage>{procChance}%</style> chance " +
            $"to spawn {baseInfestors} <style=cStack>(+{stackInfestors} per stack)</style> " +
            $"<style=cIsDamage>void infestors</style> in their place. " +
            $"<style=cIsVoid>Corrupts all Happiest Masks.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect, ItemTag.Utility };
        public override GameObject ItemModel => LoadDropPrefab("mdlVoidHappyMask");

        public override Sprite ItemIcon => LoadItemIcon("texIconVoidHappyMask");
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();
        }
        public override void PostInit()
        {
            base.PostInit();
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_GhostOnKill.GhostOnKill_asset);
        }
        public override void Hooks()
        {
            GlobalEventManager.onCharacterDeathGlobal += SpawnVoidInfestors;
        }

        private void SpawnVoidInfestors(DamageReport damageReport)
        {
            if (damageReport.attackerBody != null && damageReport.attackerMaster != null && damageReport.victimTeamIndex != TeamIndex.Void)
            {
                int maskCount = GetCount(damageReport.attackerBody);//inventory.GetItemCountEffective(RoR2Content.Items.GhostOnKill);
                if (maskCount > 0 && Util.CheckRoll(procChance, damageReport.attackerMaster))
                {
                    int infestorCount = baseInfestors + stackInfestors * (maskCount - 1);
                    for (int i = 0; i < infestorCount; i++)
                    {
                        ScriptedCombatEncounter.SpawnInfo spawnInfo = new ScriptedCombatEncounter.SpawnInfo();
                        spawnInfo.explicitSpawnPosition = damageReport.victimBody.transform;
                        spawnInfo.spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/EliteVoid/cscVoidInfestor.asset").WaitForCompletion();
                        this.Spawn(ref spawnInfo);
                    }
                }
            }
        }
        private void Spawn(ref ScriptedCombatEncounter.SpawnInfo spawnInfo)
        {
            DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                minDistance = 0f,
                maxDistance = 1000f,
                position = spawnInfo.explicitSpawnPosition.position,
                spawnOnTarget = spawnInfo.explicitSpawnPosition
            };
            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnInfo.spawnCard, directorPlacementRule, RoR2Application.rng);
            directorSpawnRequest.ignoreTeamMemberLimit = true;
            directorSpawnRequest.teamIndexOverride = TeamIndex.Void;
            DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
        }
    }
}
