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

        public override void Init()
        {
            CreateChocolate();
            base.Init();
        }
        public override void Hooks()
        {
            GetHitBehavior += ChocolateCoinOnHit;
        }

        private void CreateChocolate()
        {
            chocolate = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Tooth/HealPack.prefab").WaitForCompletion().InstantiateClone("Chocolate", true);

            foreach(Transform child in chocolate.transform)
            {
                if (child.gameObject.GetComponent<HealthPickup>() != null)
                {
                    MoneyPickup chocolateMoney = child.gameObject.AddComponent<MoneyPickup>();
                    chocolateMoney.baseGoldReward = 1;
                    chocolateMoney.shouldScale = false;
                }
            }

            DestroyOnTimer destroyTimer = chocolate.GetComponentInChildren<DestroyOnTimer>();
            destroyTimer.duration = chocolateLifetime;

            Content.AddNetworkedObjectPrefab(chocolate);
        }

        private void ChocolateCoinOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            int itemCount = GetCount(attackerBody);
            if(itemCount <= 0)
            {
                return;
            }
            GameObject chocolateInstance = UnityEngine.Object.Instantiate<GameObject>(chocolate, damageInfo.position + UnityEngine.Random.insideUnitSphere * victimBody.radius * 0.5f, UnityEngine.Random.rotation); //stolen from chef which was stolen from rex lmao
            TeamFilter chocolateInstanceTeamFilter = chocolateInstance.GetComponent <TeamFilter>();
            if (chocolateInstanceTeamFilter)
            {
                chocolateInstanceTeamFilter.teamIndex = attackerBody.teamComponent.teamIndex;
            }
            HealthPickup chocolatePickup = chocolateInstance.GetComponentInChildren<HealthPickup>();
            if (chocolatePickup)
            {
                chocolatePickup.fractionalHealing = healFraction;
                chocolatePickup.flatHealing = healFlatBase + healFlatStack * (itemCount - 1);
            }
            MoneyPickup chocolateGold = chocolateInstance.GetComponent<MoneyPickup>();
            if (chocolateGold)
            {
                chocolateGold.baseGoldReward = goldBase + goldStack * (itemCount - 1);
            }

        }
    }
}
