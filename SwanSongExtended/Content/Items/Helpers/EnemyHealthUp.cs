﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class EnemyHealthUp : ItemBase<EnemyHealthUp>
    {
        public override bool lockEnabled => true;
        public override string ConfigName => "";
        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        public override string ItemName => "Enemy Health Up";

        public override string ItemLangTokenName => "ENEMYHEALTHUP";

        public override string ItemPickupDesc => "guess what idiot";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.CannotSteal };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/enemyHealthUp.prefab");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("Assets/Icons/texIconPickupITEM_ENEMYHEALTHUP.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.DirectorCore.TrySpawnObject += StoreEnemyAsVariable; //move this to a seperate class and make it generic
            On.RoR2.CombatDirector.Spawn += GiveEnemyItem;
        }

        private bool GiveEnemyItem(On.RoR2.CombatDirector.orig_Spawn orig, CombatDirector self, SpawnCard spawnCard, EliteDef eliteDef, Transform spawnTarget, DirectorCore.MonsterSpawnDistance spawnDistance, bool preventOverhead, float valueMultiplier, DirectorPlacementRule.PlacementMode placementMode)
        {
            bool value = orig(self, spawnCard, eliteDef, spawnTarget, spawnDistance, preventOverhead, valueMultiplier, placementMode);
            if (value)
            {
                Inventory inv = enemySpawned.GetComponent<Inventory>();
                int num = 0;//number of enemy health up items on the player team
                using (IEnumerator<CharacterMaster> enumerator = CharacterMaster.readOnlyInstancesList.GetEnumerator())//gets each character on the player team and checks each ones inventory
                {
                    while (enumerator.MoveNext())
                    {
                        int itemCount = enumerator.Current.inventory.GetItemCount(this.ItemsDef);
                        if (itemCount > 0)
                        {
                            num += itemCount;
                        }
                    }
                }
                if (inv != null && num > 0)
                {
                    inv.GiveItem(HealthUp.instance.ItemsDef, num);
                }
            }
            return value;
        }
        private GameObject StoreEnemyAsVariable(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore self, DirectorSpawnRequest directorSpawnRequest)
        {
            enemySpawned = orig(self, directorSpawnRequest);
            return enemySpawned;
        }

        GameObject enemySpawned;

    }
}
