using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static MoreStats.OnHit;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class CritRetaliate : ItemBase<CritRetaliate>
    {
        public static BuffDef watchCritBuff;
        #region config
        public override string ConfigName => "Items : Destroyer Emblem";
        [AutoConfig("Critical Strike Chance Bonus", 100)]
        public static float critChanceBonus = 100;
        public static float critChancePerBuff => critChanceBonus / buffTotal;
        [AutoConfig("Total Buffs", 20)]
        public static int buffTotal = 20;
        [AutoConfig("Base Duration Of Buffs", 6f)]
        public static float buffDurationBase = 6f;
        [AutoConfig("Stack Duration Of Buffs", 4f)]
        public static float buffDurationStack = 4f;
        #endregion
        public override string ItemName => "Destroyer Emblem";

        public override string ItemLangTokenName => "CRITRETALIATE";

        public override string ItemPickupDesc => "Increase critical strike chance for a short time after being hit.";

        public override string ItemFullDescription => $"After getting hit, gain a <style=cIsDamage>{critChanceBonus}%</style> chance " +
            $"to <style=cIsDamage>Critically Strike</style>, fading over " +
            $"<style=cIsDamage>{buffDurationBase} seconds</style> <style=cStack>(+{buffDurationStack} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlDestroyerEmblem.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/critretaliate.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            watchCritBuff = Content.CreateAndAddBuff("bdWatchCritChance",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.yellow,
                true, false,
                BuffDef.StackingDisplayMethod.Default);

            base.Init();
        }

        public override void Hooks()
        {
            GetStatCoefficients += WatchCritChance;
        }

        private void WatchCritChance(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(watchCritBuff);
            args.critAdd += critChancePerBuff * buffCount;

            if (GetCount(sender) > 0)
                args.critAdd += 2;
        }
    }
    public class DestroyerEmblemBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => CritRetaliate.instance.ItemsDef;

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
            CharacterBody victimBody = damageReport.victimBody;
            if (victimBody == null)
                return;

            victimBody.ClearTimedBuffs(CritRetaliate.watchCritBuff);
            float duration = CritRetaliate.buffDurationStack * (stack - 1) + CritRetaliate.buffDurationBase;
            for (int i = 0; i < CritRetaliate.buffTotal; i++)
            {
                victimBody.AddTimedBuffAuthority(CritRetaliate.watchCritBuff.buffIndex, duration * (float)(i + 1) / (float)CritRetaliate.buffTotal);
            }
            //victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration);
        }
    }
}
