using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.Language.Styling;
using static SwanSongExtended.Modules.HitHooks;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;

namespace SwanSongExtended.Items
{
    class PearWiggler : ItemBase<PearWiggler>
    {
        #region config
        [AutoConfig("Healing Base", 4)]
        public static int healingBase = 4;
        [AutoConfig("Healing Stack", 4)]
        public static int healingStack = 4;
        [AutoConfig("Barrier Base", 8)]
        public static int barrierBase = 8;
        [AutoConfig("Barrier Stack", 8)]
        public static int  barrierStack = 8;

        public override string ConfigName => "Item: " + ItemName;
        #endregion
        #region Abstract
        public override string ItemName => "Pear Wiggler";

        public override string ItemLangTokenName => "PEARWIGGLER";

        public override string ItemPickupDesc => $"High damage attacks store {HealthColor("Pears.")} When struck, release stored pears, which can be {HealthColor("picked up for health and barrier")}";

        public override string ItemFullDescription => $"High damage attacks store {HealthColor("Pears.")} When struck, release stored pears, which can be {HealthColor("picked up for health and barrier")}";

        public override string ItemLore => "Pears taste better wiggled.";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing};

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSOTS;
        #endregion
        public static BuffDef pearBuff;
        GameObject pear;
        float pearLifetime = 5;
        float gravRadius = 2f;
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += PearWigglerTakeDamage;
            GetHitBehavior += PearWigglerOnHit;
            pearBuff = Content.CreateAndAddBuff("PearBuff", null, Color.green, true, false);
        }


        private void CreatePear()
        {
            pear = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Tooth/HealPack.prefab").WaitForCompletion().InstantiateClone("Pear", true);

            GravitatePickup gravPickup = pear.GetComponentInChildren<GravitatePickup>();
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

            DestroyOnTimer destroyTimer = pear.GetComponentInChildren<DestroyOnTimer>();
            if (destroyTimer)
            {
                destroyTimer.duration = pearLifetime;
                BeginRapidlyActivatingAndDeactivating braad = pear.GetComponent<BeginRapidlyActivatingAndDeactivating>();
                if (braad)
                {
                    braad.delayBeforeBeginningBlinking = pearLifetime - 2f;
                }
            }
        }
        private void PearWigglerOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (damageInfo.damage / attackerBody.damage < 4)
            {
                return;
            }
            int pearCount = GetCount(attackerBody);
            if (pearCount <= 0 || !NetworkServer.active)
            {
                return;
            }
            attackerBody.AddBuff(pearBuff);
        }

        private void PearWigglerTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            CharacterBody body = self?.body;
            int itemCount = GetCount(body);
            if (itemCount <= 0)
            {
                return;
            }
            if (self.body.GetBuffCount(pearBuff) <= 0)
            {
                return;
            }
            int percentLost = (int)Math.Round((damageInfo.damage / self.fullCombinedHealth) * 100);
            WigglePears(itemCount --, percentLost, body, self);
        }

        private void WigglePears(int a, int b, CharacterBody body, HealthComponent healthComponent)
        {
            for (int i = 0; i < b && body.GetBuffCount(pearBuff) > 0; i++)//a=itemcount -1; b=%hp lost
            {
                WigglePear(a, healthComponent);
                body.RemoveBuff(pearBuff);
            }
           
        }


        private void WigglePear(int i, HealthComponent a)
        {
            GameObject pearInstance = UnityEngine.Object.Instantiate<GameObject>(pear, a.body.corePosition + UnityEngine.Random.insideUnitSphere * a.body.radius * 20/*hopefully this will make it so the pears arent immediately munched*/, UnityEngine.Random.rotation);
            TeamFilter pearFilter = pearInstance.GetComponent<TeamFilter>();
            if (pearFilter)
            {
                pearFilter.teamIndex = a.body.teamComponent.teamIndex;
            }
            HealthPickup pearPickup = pearInstance.GetComponentInChildren<HealthPickup>();
            if (pearPickup)
            {
                pearPickup.fractionalHealing = 0;
                pearPickup.flatHealing = healingBase + healingStack * i;
            }
            NetworkServer.Spawn(pearInstance);
            //for (int b = 0; b > )
            a.AddBarrier(barrierBase + (barrierStack * i));
        }

        public override void Init()
        {
            CreatePear();
            base.Init();
        }
    }
}
