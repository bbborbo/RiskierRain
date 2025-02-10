using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;
using static SwanSongExtended.Modules.HitHooks;


namespace SwanSongExtended.Items
{
    class ChocolateCoin : ItemBase<ChocolateCoin>
    {

        public override string ConfigName => "Items : Chocolate Coin";
        GameObject chocolate;
        int fruitChanceBase = 1;
        int goldBase = 1;
        int goldStack = 2;
        float healFraction = 0.00f;
        float healFlatBase = 5f;
        float healFlatStack = 5f;
        float chocolateLifetime = 10f;

        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();
        public override string ItemName => "Chocolate Coin";

        public override string ItemLangTokenName => "CHOCOLATECOIN";

        public override string ItemPickupDesc => "Chance on hit to spawn a chocolate coin for gold and healing.";

        public override string ItemFullDescription => "yum";

        public override string ItemLore => "don't eat the wrapping!";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Utility };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/pickupmodels/PickupGoldOnHurt");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/itemicons/texGoldOnHurtIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }

        public override void Hooks()
        {
            GetHitBehavior += ChocolateCoinOnHit;
        }

        private void CreateChocolate()
        {
            chocolate = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotFruitPack.prefab").WaitForCompletion().InstantiateClone("MeatChunk", true);

            HealthPickup healthPickup = chocolate.GetComponentInChildren<HealthPickup>();
            if (healthPickup)
            {
                healthPickup.fractionalHealing = healFraction;
                healthPickup.flatHealing = healFlatBase;
            }

            DestroyOnTimer destroyTimer = chocolate.GetComponentInChildren<DestroyOnTimer>();
            destroyTimer.duration = chocolateLifetime;

            Content.AddNetworkedObjectPrefab(chocolate);
        }

        private void ChocolateCoinOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            int itemCount = GetCount(attackerBody);
        }
    }
}
