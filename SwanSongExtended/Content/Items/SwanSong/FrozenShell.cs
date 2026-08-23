using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Modules;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class FrozenShell : ItemBase<FrozenShell>
    {
        internal static BuffDef frozenShellArmorBuff;
        internal static int freeArmor = 10;
        internal static int maxBonusArmor = 50; //(100 / 3)
        public static int maxBuffCount = 10;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Frozen Turtle Shell";

        public override string ItemLangTokenName => "FROZENSHELL";

        public override string ItemPickupDesc => "Reduce incoming damage while at low health.";

        public override string ItemFullDescription => $"<style=cIsHealing>Increase armor</style> by " +
            $"<style=cIsHealing>{freeArmor}</style> <style=cStack>(+{freeArmor} per stack)</style>. " +
            $"For every missing <style=cIsHealth>{Mathf.RoundToInt(100 / (float)maxBuffCount)}% of max health</style>, " +
            $"gain <style=cIsHealing>{Mathf.RoundToInt(maxBonusArmor / maxBuffCount)}</style> " +
            $"<style=cStack>(+{Mathf.RoundToInt(maxBonusArmor / maxBuffCount)} per stack)</style> additional armor.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => LoadDropPrefab("mdlFrozenShell");

        public override Sprite ItemIcon => LoadItemIcon("texIconFrozenShell");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return null;
        }
        public override void Init()
        {
            frozenShellArmorBuff = Content.CreateAndAddBuff(
                "bdIceBarrier",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.cyan,
                true, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += this.GiveBonusArmor;
        }

        private void GiveBonusArmor(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                int buffCount = sender.GetBuffCount(frozenShellArmorBuff);
                float fraction = (float)buffCount / (float)maxBuffCount;
                int buffArmor = Mathf.RoundToInt((float)maxBonusArmor * fraction);
                args.armorAdd += itemCount * (freeArmor + buffArmor * buffCount);
            }
        }
    }
    public class FrozenShellBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => FrozenShell.instance.ItemsDef;
        HealthComponent healthComponent;
        BuffIndex iceBarrierBuffIndex = FrozenShell.frozenShellArmorBuff.buffIndex;
        //bool hasBuff = false;
        //new version
        int buffCount = 0;
        float pollInterval = 1f;
        float pollCountdown = 0;

        private void Start()
        {
            healthComponent = body.healthComponent;
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);
            CalculateBuffCount();
            //hasBuff = body.HasBuff(iceBarrierBuffIndex);
        }
        void OnDestroy()
        {
            body.SetBuffCount(iceBarrierBuffIndex, 0);
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
        }
        private void FixedUpdate()
        {
            if(pollCountdown > 0)
            {
                pollCountdown -= Time.fixedDeltaTime;
                return;
            }
            pollCountdown = pollInterval;
            CalculateBuffCount();
        }

        void CalculateBuffCount()
        {
            float combinedHealthFraction = healthComponent.combinedHealthFraction;
            /*if (hasBuff)
            {
                if (combinedHealthFraction > 0.5f)
                {
                    this.body.RemoveBuff(iceBarrierBuffIndex);
                    hasBuff = false;
                }
            }
            else if (combinedHealthFraction <= 0.5f)
            {
                this.body.AddBuff(iceBarrierBuffIndex);
                hasBuff = true;
            }*/
            //new version
            float missingHealthFraction = (1 - combinedHealthFraction);
            int newBuffCount = Mathf.CeilToInt(missingHealthFraction * (FrozenShell.maxBuffCount));
            body.SetBuffCount(iceBarrierBuffIndex, newBuffCount);
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            CalculateBuffCount();
        }
    }
}
