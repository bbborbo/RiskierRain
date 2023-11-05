using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Items
{
    class VoidHappyMask : ItemBase<VoidHappyMask>
    {
        public float procChance = 7;
        public int baseInfestors = 2;
        public int stackInfestors = 1;
        public static SpawnCard infestorSpawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscVoidInfestor");
        public override string ItemName => "Tragic Facade";

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
        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlTragicFacade.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_VOIDHAPPIESTMASK.png");
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.GlobalEventManager.OnCharacterDeath += CreateVoidInfestors;
        }

        private void CreateVoidInfestors(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            if(damageReport.attackerBody != null && damageReport.attackerMaster != null && damageReport.victimTeamIndex != TeamIndex.Void)
            {
                int maskCount = GetCount(damageReport.attackerBody);//inventory.GetItemCount(RoR2Content.Items.GhostOnKill);
                if (maskCount > 0 && Util.CheckRoll(procChance, damageReport.attackerMaster))
                {
                    int infestorCount = baseInfestors + stackInfestors * (maskCount - 1);
                    for(int i = 0; i < infestorCount; i++)
                    {
                        ScriptedCombatEncounter.SpawnInfo spawnInfo = new ScriptedCombatEncounter.SpawnInfo();
                        spawnInfo.explicitSpawnPosition = damageReport.victimBody.transform;
                        spawnInfo.spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/EliteVoid/cscVoidInfestor.asset").WaitForCompletion();
                        this.Spawn(ref spawnInfo);
                    }
                }
            }
            orig(self, damageReport);
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
        public override void Init(ConfigFile config)
        {
            //Debug.LogError("Void Happy Mask should require SOTV but it doesnt! dont forget to fix!");
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.GhostOnKill, //consumes lepton daisy
                itemDef2 = VoidHappyMask.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
