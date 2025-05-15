using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using static MoreStats.OnHit;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class VoidIchorViolet : ItemBase<VoidIchorViolet>
    {
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public float cooldown = 1;
        int barrierBase = 22;
        int barrierStack = 22;
        public override string ItemName => "Metamorphic Ichor (Violet)";

        public override string ItemLangTokenName => "ICHORVIOLET";

        public override string ItemPickupDesc => $"Gain barrier when hurt. {VoidColor("Corrupts all Medkits and Red Ichors")}.";

        public override string ItemFullDescription => $"Gain {HealingColor($"{barrierBase} barrier")} when hurt {StackText($"+{barrierStack}")}. " +
            $"{VoidColor("Corrupts all Medkits and Red Ichors")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorV.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_ICHORVIOLET.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        //buff
        public static BuffDef violetBuff;
        public override void Init()
        {
            violetBuff = Content.CreateAndAddBuff(
                "bdVioletCooldown",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                new Color(0.9f, 0.8f, 0.0f),
                false, true);
            violetBuff.isHidden = true;
            violetBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            base.Init();
        }
        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.HealthComponent.TakeDamageProcess += VioletIchorOnTakeDamage;
            //GetHitBehavior += VioletIchorOnHit;
        }

        private void VioletIchorOnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            CharacterBody body = self.body;
            if (body == null || !self.alive)
                return;      
            
            int itemCount = GetCount(body);
            if (itemCount > 0)
            {
                DamageType damageType = damageInfo.damageType.damageType;
                bool badDamage = damageType.HasFlag(DamageType.BypassArmor) || damageType.HasFlag(DamageType.DoT);
                bool selfDamage = self.gameObject != damageInfo.attacker;
                if (!body.HasBuff(violetBuff) && !badDamage && !selfDamage)
                {
                    int barrierToAdd = barrierBase + barrierStack * (itemCount - 1);
                    self.AddBarrier(barrierToAdd);
                    body.AddTimedBuffAuthority(violetBuff.buffIndex, cooldown);//make this not hardcoded //i did it - borbo
                }
            }            
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Medkit, //consumes medkit
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            ItemDef.Pair rockPaperScissors = new ItemDef.Pair()
            {
                itemDef1 = VoidIchorRed.instance.ItemsDef, //:3
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(rockPaperScissors);
            orig();
        }
    }
    
}
