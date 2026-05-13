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
using static MoreStats.OnHit;


namespace SwanSongExtended.Items
{
    class ChocolateCoin : ItemBase<ChocolateCoin>
    {

        public override string ConfigName => "Items : Chocolate Coin";
        GameObject chocolate;
        float gravRadius = 4f;
        int goldBase = 1;
        int goldStack = 1;
        float healFraction = 0.00f;
        float healFlatBase = 5f;
        float healFlatStack = 5f;
        float chocolateLifetime = 10f;

        public override string ItemName => "Chocolate Coin";

        public override string ItemLangTokenName => "CHOCYCOIN";

        public override string ItemPickupDesc => "Enemies drop a treat for gold and healing. Recharges over time.";

        public override string ItemFullDescription =>
            $"On hit, " +
            $"drop a chocolate coin that heals for " +
            $"{HealingColor(healFlatBase.ToString() + " health")} {StackText($"+{healFlatStack}")} " +
            $"plus {UtilityColor(goldBase.ToString() + " gold")} {StackText($"+{goldStack}")}. " +
            $"{UtilityColor("Scales over time")}. " +
            $"{UtilityColor("recharges after 5 seconds.")}";
            

        public override string ItemLore => "don't eat the wrapping!";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Utility, ItemTag.FoodRelated };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GoldOnHurt/PickupRollOfPennies.prefab").WaitForCompletion();//Resources.Load<GameObject>("prefabs/pickupmodels/PickupGoldOnHurt");

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/GoldOnHurt/texRollOfPenniesIcon.png").WaitForCompletion();//Resources.Load<Sprite>("textures/itemicons/texGoldOnHurtIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            SwanSongPlugin.RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_GoldOnHurt.GoldOnHurt_asset);
            CreateChocolate();
            base.Init();
        }
        public override void Hooks()
        {
            GetHitBehavior += ChocolateCoinOnHit;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    ChocolateCoinBehavior ringBehavior = self.AddItemBehavior<ChocolateCoinBehavior>(GetCount(self));
                }
            }
        }
        private void CreateChocolate()
        {
            chocolate = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Tooth/HealPack.prefab").WaitForCompletion().InstantiateClone("Chocolate", true);

            TeamFilter teamFilter = chocolate.GetComponent<TeamFilter>();

            HealthPickup healthPickup = chocolate.GetComponentInChildren<HealthPickup>();
            if (healthPickup)
            {
                MoneyPickup chocolateMoney = healthPickup.gameObject.AddComponent<MoneyPickup>();
                chocolateMoney.baseGoldReward = 1;
                chocolateMoney.shouldScale = false; //we scale it manually
                chocolateMoney.teamFilter = teamFilter;
            }

            GravitatePickup gravPickup = chocolate.GetComponentInChildren<GravitatePickup>();
            if (gravPickup != null)
            {
                gravPickup.gravitateAtFullHealth = true;
                Collider gravitateTrigger = gravPickup.gameObject.GetComponent<Collider>();
                if (gravitateTrigger.isTrigger)
                {
                    gravitateTrigger.transform.localScale = Vector3.one * gravRadius;
                }
            }
            else
            {
                Debug.Log("No gravitatepickup");
            }

            DestroyOnTimer destroyTimer = chocolate.GetComponentInChildren<DestroyOnTimer>();
            if (destroyTimer)
            {
                destroyTimer.duration = chocolateLifetime;
                BeginRapidlyActivatingAndDeactivating braad = chocolate.GetComponent<BeginRapidlyActivatingAndDeactivating>();
                if (braad)
                {
                    braad.delayBeforeBeginningBlinking = chocolateLifetime - 2f;
                }
            }

            ParticleSystemRenderer[] psrs = chocolate.GetComponentsInChildren<ParticleSystemRenderer>();
            for(int i = 0; i < psrs.Length; i++)
            {
                ParticleSystemRenderer psr = psrs[i];
                string name = psr.gameObject.name;
                Color32 color = Color.white;
                string matName = "";
                if (name == "Core")
                {
                    matName = "matCholocateTrail";
                    color = new Color32(62, 37, 0, 255);
                }
                if (name == "PulseGlow")
                {
                    matName = "matChocolateGlow";
                    color = new Color32(79, 46, 0, 255);
                }

                if(matName != "")
                {
                    Material mat = UnityEngine.Object.Instantiate(psr.material);
                    psr.material = mat;
                    mat.name = matName;
                    mat.DisableKeyword("VERTEXCOLOR");
                    mat.SetFloat("_VertexColorOn", 0);
                    mat.SetColor("_TintColor", color);
                }
            }
            Content.AddNetworkedObjectPrefab(chocolate);
        }

        private void ChocolateCoinOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (!NetworkServer.active)
                return;

            int itemCount = GetCount(attackerBody);
            if(itemCount <= 0)
            {
                return;
            }
            ChocolateCoinBehavior itemBehavior = attackerBody.gameObject.GetComponent<ChocolateCoinBehavior>();
            if (itemBehavior == null)
            {
                return;
            }
            if (!itemBehavior.IsReady())
            {
                return;
            }
            itemBehavior.ResetCooldown();
            GameObject chocolateInstance = UnityEngine.Object.Instantiate<GameObject>(chocolate, damageInfo.position + UnityEngine.Random.insideUnitSphere * victimBody.radius * 0.5f, UnityEngine.Random.rotation); //stolen from chef which was stolen from rex lmao
            TeamFilter chocolateInstanceTeamFilter = chocolateInstance.GetComponent<TeamFilter>();
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
                int baseGold = goldBase + goldStack * (itemCount - 1);
                chocolateGold.baseGoldReward = baseGold * Mathf.RoundToInt(attackerBody.level);
            }
            NetworkServer.Spawn(chocolateInstance);
            
        }
    }
    public class ChocolateCoinBehavior : CharacterBody.ItemBehavior
    {
        float coinCooldown = 5;
        float timer = 0;
        bool chocolateReady = true;
        
        public bool IsReady()
        {
            return chocolateReady;
        }
        public void ResetCooldown()
        {
            timer = coinCooldown;
            chocolateReady = false;
        }
        private void FixedUpdate()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                chocolateReady = true;
            }
        }
    }
}
