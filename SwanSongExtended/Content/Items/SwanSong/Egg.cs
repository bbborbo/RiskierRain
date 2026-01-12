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

namespace SwanSongExtended.Items
{
    class Egg : ItemBase<Egg>
    {
        public override bool lockEnabled => true;
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

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texEggIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();
            Log.Error("Egg Cant Hide Eggpiles Because Eggpile Not Implemented !!");
        }

        public override void Hooks()
        {
            GlobalEventManager.onCharacterDeathGlobal += EggOnAnyDeath;
            On.RoR2.GlobalEventManager.OnInteractionBegin += EggOnPurchase;//includes uhhhhhh the uh yea. (item pickups)
            RecalculateStatsAPI.GetStatCoefficients += EggStats;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
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

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            //to add: -chocolate egg
            //compat: -donut (mystics) -probably a bunch of ss2 stuff
            ItemDef.Pair transformation2 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Infusion, //consumes infusion
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemDef.Pair transformation3 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.FlatHealth, //consumes meat
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemDef.Pair transformation4 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.AlienHead, //consumes gah
                itemDef2 = Egg.instance.ItemsDef
            };
            /*ItemDef.Pair transformation5 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Seed, //consumes gah
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                 = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation5);*/
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation2);
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                 = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation3);
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                 = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation4);
            orig();
        }
    }
}
