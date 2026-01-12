using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class ChargedSpine : ItemBase<ChargedSpine>
    {

        public static float baseShield = 50;
        public static float stackShield = 50;
        public static float baseDuration = 5;
        public static float stackDuration = 5;
        public static float baseArmor = 200;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Charged Malachite Spine";

        public override string ItemLangTokenName => "CHARGEDMALACHITESPINE";

        public override string ItemPickupDesc => "Poison yourself when shields are broken, gaining great damage resistence.";

        public override string ItemFullDescription => $"Gain {HealingColor($"{baseShield} shield")} {StackText($"+{stackShield}")}. " +
            $"While poisoned, gain {HealingColor($"{baseArmor} armor")}. " +
            $"{RedText($"Poison is inflicted for {baseDuration} seconds on shield break")} {StackText($"+{stackDuration} seconds")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlChargedSpine.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CHARGED_MALACHITE_SPINE.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += ChargedSpineStats;
        }

        private void ChargedSpineStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.baseShieldAdd += baseShield * itemCount;
                //gives armor when poisoned
                if (sender.HasBuff(RoR2Content.Buffs.HealingDisabled))
                {
                    args.armorAdd += baseArmor;
                }
            }
        }
    }

    public class SpineBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => ChargedSpine.instance.ItemsDef;
        bool hadShield = false;
        void Start()
        {
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);
            hadShield = HasShield();
        }
        void OnDestroy()
        {
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
        }
        void FixedUpdate()
        {
            hadShield = HasShield();
        }

        bool HasShield()
        {
            return (body.healthComponent?.shield ?? 0) > 0;
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if (!hadShield || HasShield() || !body.healthComponent.alive)
                return;

            if (stack > 0)
            {
                body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, ChargedSpine.baseDuration + ChargedSpine.stackDuration * (stack - 1));
            }
        }
    }
}
