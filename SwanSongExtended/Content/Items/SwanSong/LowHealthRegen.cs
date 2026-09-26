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
using static SwanSongExtended.Modules.Language.Styling;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    public class LowHealthRegen : ItemBase<LowHealthRegen>
    {
        public static BuffDef regenBoostBuff;
        public static float threshold = 0.6f;
        public static float regenBonusBase = 2f;
        public static float regenBonusStack = 2f;

        public override string ItemName => "Low Health Regen";

        public override string ItemLangTokenName => "LOWHEALTHREGEN";

        public override string ItemPickupDesc => $"Increase health regen while below {threshold.AsPercent()} health.";

        public override string ItemFullDescription => $"While below {HealthColor(threshold.AsPercent() + " maximum health")}, " +
            $"increase {HealthColor("base health regeneration")} " +
            $"by {HealthColor($"+{regenBonusBase} hp/s")} {StackText($"+{regenBonusStack} hp/s")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.LowHealth };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return null;
        }
        public override void Init()
        {
            regenBoostBuff = Content.CreateAndAddBuff(
                "bdLowHealthRegen",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.cyan,
                true, false,
                isHidden: true
                );
            base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += this.GiveBonusArmor;
        }

        private void GiveBonusArmor(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(regenBoostBuff) == false)
                return;
            int itemCount = GetCount(sender);
            if (itemCount <= 0)
                return;
            float regen = regenBonusBase + regenBonusStack * (itemCount - 1);
            args.baseRegenAdd += regen;
            args.levelRegenAdd += regen * 0.2f;
        }
    }
    public class LowHealthRegenBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => LowHealthRegen.instance.ItemsDef;
        HealthComponent healthComponent;
        BuffIndex iceBarrierBuffIndex = LowHealthRegen.regenBoostBuff.buffIndex;
        //bool hasBuff = false;
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
            if (pollCountdown > 0)
            {
                pollCountdown -= Time.fixedDeltaTime;
                return;
            }
            pollCountdown = pollInterval;
            CalculateBuffCount();
        }

        void CalculateBuffCount()
        {
            int buffCount = 0;
            if (healthComponent.healthFraction <= LowHealthRegen.threshold)
                buffCount = 1;
            body.SetBuffCount(iceBarrierBuffIndex, buffCount);
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            CalculateBuffCount();
        }
    }
}
