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
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class VoidIchorViolet : ItemBase<VoidIchorViolet>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Interactables.VoidHusk.GetIchorConfig();
        }
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public static float cooldown = 1;
        public static int barrierBase = 22;
        public static int barrierStack = 22;
        public override string ItemName => "Metamorphic Ichor (Violet)";

        public override string ItemLangTokenName => "ICHORVIOLET";

        public override string ItemPickupDesc => $"Gain barrier when hurt. {VoidColor("Corrupts all Medkits and Red Ichors")}.";

        public override string ItemFullDescription => $"Gain {HealingColor($"{barrierBase} barrier")} when hurt {StackText($"+{barrierStack}")}. " +
            $"{VoidColor("Corrupts all Medkits and Red Ichors")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorV.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/voidichorviolet.png");
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
        }

        public override void AddVoidRelationships()
        {
            base.AddVoidRelationships();
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Medkit.Medkit_asset);
            AddVoidItemRelationship(VoidIchorRed.instance.ItemsDef);
        }
    }


    public class VioletIchorBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => VoidIchorViolet.instance.ItemsDef;

        void Start()
        {
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);
        }
        void OnDestroy()
        {
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if (body.HasBuff(VoidIchorViolet.violetBuff))
                return;

            DamageType damageType = damageReport.damageInfo.damageType.damageType;
            bool badDamage = damageType.HasFlag(DamageType.DoT);
            bool selfDamage = body.gameObject == damageReport.attackerBody;
            if (badDamage)
                return;
         
            int barrierToAdd = VoidIchorViolet.barrierBase + VoidIchorViolet.barrierStack * (stack - 1);
            body.healthComponent.AddBarrier(barrierToAdd);
            body.AddTimedBuffAuthority(VoidIchorViolet.violetBuff.buffIndex, VoidIchorViolet.cooldown);//make this not hardcoded //i did it - borbo
        }
    }
}
