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
        public static BuffDef temporaryCritBuff;
        public static BuffDef hiddenGuaranteedCritBuff;
        #region config
        public override string ConfigName => "Items : Destroyer Emblem";
        [AutoConfig("Critical Strike Chance Bonus", 100)]
        public static float critChanceBonus = 100;
        [AutoConfig("Critical Strike Chance Free", 5)]
        public static float critChanceFree = 5;
        public static float critChancePerBuff => critChanceBonus / tierTotal;
        [AutoConfig("Total Buff Tiers", "In order to avoid constantly recalculating stats, the total crit chance bonus is divided into this amount of tiers. For example with 10 tiers at 100% crit chance, it would decay 10% crit chance at a time.", 10)]
        public static int tierTotal = 10;
        [AutoConfig("Duration Fraction Of Guaranteed Crit", 0.25f)]
        public static float guaranteeDurationBase = 0.25f;
        [AutoConfig("Base Duration Of Buffs", 4f)]
        public static float buffDurationBase = 4f;
        [AutoConfig("Stack Duration Of Buffs", 2f)]
        public static float buffDurationStack = 2f;
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

        public override GameObject ItemModel => LoadDropPrefab("mdlCritRetaliate");

        public override Sprite ItemIcon => LoadItemIcon("texIconCritRetaliate");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            temporaryCritBuff = Content.CreateAndAddBuff("bdRetaliateCritBonusTemporary",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.yellow,
                true, false,
                BuffDef.StackingDisplayMethod.Percentage,
                isHidden: false);
            hiddenGuaranteedCritBuff = Content.CreateAndAddBuff("bdRetaliateCritBonusHidden",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.yellow,
                true, false,
                BuffDef.StackingDisplayMethod.Default,
                isHidden: true);

            base.Init();
        }

        public override void Hooks()
        {
            GetStatCoefficients += WatchCritChance;
            On.RoR2.GlobalEventManager.OnCrit += RemoveGuaranteeCritOnCrit;
        }

        private void RemoveGuaranteeCritOnCrit(On.RoR2.GlobalEventManager.orig_OnCrit orig, GlobalEventManager self, CharacterBody body, DamageInfo damageInfo, CharacterMaster master, float procCoefficient, ProcChainMask procChainMask)
        {
            orig(self, body, damageInfo, master, procCoefficient, procChainMask);
            if (body == null 
                || NetworkServer.active == false
                || (damageInfo.damageType.IsDamageSourceSkillBased == false 
                    && damageInfo.damageType.damageSource != DamageSource.Equipment)
                )
                return;
            if (body.HasBuff(hiddenGuaranteedCritBuff))
                body.RemoveBuff(hiddenGuaranteedCritBuff);
        }

        private void WatchCritChance(CharacterBody sender, StatHookEventArgs args)
        {
            float crit = sender.GetBuffCount(temporaryCritBuff);
            if (sender.HasBuff(hiddenGuaranteedCritBuff))
                crit += 100;
            if (GetCount(sender) > 0)
                args.critAdd += Mathf.Clamp(crit, critChanceFree, 100);
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

            victimBody.ClearTimedBuffs(CritRetaliate.temporaryCritBuff);

            float totalDuration = CritRetaliate.buffDurationBase + (CritRetaliate.buffDurationStack * (stack - 1));
            float buffsPerTier = CritRetaliate.critChanceBonus / (float)CritRetaliate.tierTotal;
            int lastTier = 0;
            for (int i = 0; i < CritRetaliate.tierTotal; i++)
            {
                float nextDuration = totalDuration * (float)(i + 1) / (float)CritRetaliate.tierTotal;
                int nextTier = Mathf.RoundToInt((i + 1) * buffsPerTier);
                for(int n = 0; n < nextTier - lastTier; n++)
                {
                    victimBody.AddTimedBuff(CritRetaliate.temporaryCritBuff.buffIndex, nextDuration);
                }
                lastTier = nextTier;
            }
            victimBody.AddTimedBuff(CritRetaliate.hiddenGuaranteedCritBuff.buffIndex, totalDuration * CritRetaliate.guaranteeDurationBase);
        }
    }
}
