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
using RoR2.Items;
using MonoMod.Cil;
using Mono.Cecil.Cil;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class ChocyCoin : ReworkBase<ChocyCoin>
    {
        public static BuffDef chocoCooldownDebuff;
        public static GameObject chocolate;
        public static float coinCooldown = 5;
        public static float gravRadius = 4f;
        public static int goldBase = 1;
        public static int goldStack = 1;
        public static float healFraction = 0.00f;
        public static float healFlatBase = 5f;
        public static float healFlatStack = 5f;
        public static float chocolateLifetime = 10f;

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GoldOnHurt.GoldOnHurt_asset;

        public override string ItemName => $"Chocolate Coins";

        public override string ItemPickupDesc => "Enemies drop a treat for gold and healing. Recharges over time.";

        public override string ItemFullDesc => 
            $"On hit, " +
            $"drop a chocolate coin that heals for " +
            $"{HealingColor(healFlatBase.ToString() + " health")} {StackText($"+{healFlatStack}")} " +
            $"plus {UtilityColor(goldBase.ToString() + " gold")} {StackText($"+{goldStack}")}. " +
            $"{UtilityColor("Scales over time")}. " +
            $"{UtilityColor($"recharges after {coinCooldown} seconds.")}";
        //public override string ItemLore => "don't eat the wrapping!";


        public override void Init()
        {
            chocoCooldownDebuff = Content.CreateAndAddBuff(
                "bdChocoCooldown",
                null,
                Color.white,
                false,
                true);
            chocoCooldownDebuff.isHidden = true;
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
            for (int i = 0; i < psrs.Length; i++)
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

                if (matName != "")
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
            if (itemCount <= 0)
                return;

            if (attackerBody.HasBuff(chocoCooldownDebuff))
                return;
            attackerBody.AddTimedBuff(chocoCooldownDebuff, coinCooldown);

            SpawnTreat(attackerBody, damageInfo, victimBody, itemCount);
        }

        private static void SpawnTreat(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody, int itemCount)
        {
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
                chocolateGold.baseGoldReward = Run.instance.GetDifficultyScaledCost(goldBase + goldStack * (itemCount - 1), Stage.instance.entryDifficultyCoefficient);
                chocolateGold.shouldScale = false;
            }
            NetworkServer.Spawn(chocolateInstance);
        }
    }
}
