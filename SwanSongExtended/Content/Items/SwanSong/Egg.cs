using BepInEx.Configuration;
using SwanSongExtended.Interactables;
using SwanSongExtended.Items;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using HarmonyLib;
using On.RoR2.Items;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class Egg : ItemBase<Egg>
    {
        public static bool GetEggConfig()
        {
            return SwanSongPlugin.GetConfigBool(true, "Egg Suite", "Enables Egg");
        }

        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Egg.GetEggConfig();
        }
        public static int eggHealth = 5;
        public static float eggOnKillChance = 4;
        public static float eggOnInteractChance = 4;
        private Xoroshiro128Plus rng;


        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Egg";

        public override string ItemLangTokenName => "EGG";

        public override string ItemPickupDesc => "Slightly increase health. Start an egg hunt." +
            "<style=cIsVoid>Corrupts most edible and animal matter</style>.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{eggHealth} max health</style>. " +
            $"<style=cIsUtility>Start an egg hunt</style>. " +
            "<style=cIsVoid>Corrupts all Infusion, Bison Steak, and Alien Heads</style>.";

        public override string ItemLore => "this egg is so fuckign yummuy";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.OnStageBeginEffect, ItemTag.OnKillEffect, ItemTag.InteractableRelated/*, ItemTag.WorldUnique*/};

        public override GameObject ItemModel => LoadDropPrefab("mdlEgg");

        public override Sprite ItemIcon => LoadItemIcon("texIconEgg");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();
            Log.Error("Egg Cant Hide Eggpiles Because Eggpile Not Implemented !!");
        }
        public override void PostInit()
        {
            base.PostInit();
            //to add: -chocolate egg
            //compat: -donut (mystics) -probably a bunch of ss2 stuff
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Infusion.Infusion_asset);
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FlatHealth.FlatHealth_asset);
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_AlienHead.AlienHead_asset);
            //AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Seed.Seed_asset);
        }

        public override void Hooks()
        {
            GlobalEventManager.onCharacterDeathGlobal += EggOnAnyDeath;
            On.RoR2.GlobalEventManager.OnInteractionBegin += EggOnPurchase;//includes uhhhhhh the uh yea. (item pickups)
            RecalculateStatsAPI.GetStatCoefficients += EggStats;
        }

        private void EggOnAnyDeath(DamageReport damageReport)
        {
            if (damageReport.attackerBody == null)
                return;

            int eggCount = Util.GetItemCountForTeam(damageReport.attackerTeamIndex, this.ItemsDef.itemIndex, true, false);
            if (eggCount <= 0)
                return;

            if (Util.CheckRoll(eggOnKillChance, damageReport.attackerMaster?.luck ?? 0)) //1/100
            {
                CharacterBody victim = damageReport.victimBody;
                EggReward(victim.transform);
            }
        }

        private void EggStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.baseHealthAdd += eggHealth * itemCount;
            }
        }

        private void EggOnPurchase(On.RoR2.GlobalEventManager.orig_OnInteractionBegin orig, GlobalEventManager self, Interactor interactor, IInteractable interactable, GameObject interactableObject)
        {
            orig(self, interactor, interactable, interactableObject);

            CharacterBody interactorBody = interactor.gameObject?.GetComponent<CharacterBody>();
            if (interactorBody == null)
                return;

            int eggCount = Util.GetItemCountForTeam(interactorBody.teamComponent.teamIndex, this.ItemsDef.itemIndex, true, false);
            if (eggCount <= 0)
                return;

            if (Util.CheckRoll(eggOnKillChance, interactorBody.master?.luck ?? 0)) //1/100
            {
                EggReward(interactableObject.transform);
            }
        }

        public void EggReward(Transform dropletOrigin)
        {
            UniquePickup pickupIndex = EggWeightedSelection.GeneratePickup(new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong));

            PickupDropletController.CreatePickupDroplet(pickupIndex, 
                dropletOrigin.position + (dropletOrigin.up * 3f), 
                dropletOrigin.forward * 3f + dropletOrigin.up * 5f,
                isDuplicated: false,
                isRecycled: pickupIndex.pickupIndex == PickupCatalog.FindPickupIndex(Egg.instance.ItemsDef.itemIndex)
                );
        }

        private static ExplicitPickupDropTable _EggWeightedSelection;
        public static ExplicitPickupDropTable EggWeightedSelection
        {
            get
            {
                if (_EggWeightedSelection)
                    _EggWeightedSelection = GenerateWeightedSelection();
                return _EggWeightedSelection;
            }
            set
            {
                _EggWeightedSelection = value;
            }
        }
        public static ExplicitPickupDropTable GenerateWeightedSelection()
        {
            ExplicitPickupDropTable dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();

            List<ExplicitPickupDropTable.PickupDefEntry> pickupDefEntries = new List<ExplicitPickupDropTable.PickupDefEntry>();
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                {
                    pickupDef = Egg.instance.ItemsDef,
                    pickupWeight = 1f
                }
                );
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                {
                    pickupDef = GoldenEgg.instance.ItemsDef,
                    pickupWeight = 0.1f
                }
                );
            dropTable.pickupEntries = pickupDefEntries.ToArray();

            return dropTable;
        }
    }
}
