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
        [AutoConfig("Total Buffs", 10)]
        public static int buffTotal = 10;
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

        public override GameObject ItemModel => LoadDropPrefab("mdlCritRetaliate");

        public override Sprite ItemIcon => LoadItemIcon("texIconCritRetaliate");

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
            On.RoR2.GlobalEventManager.ProcessHitEnemy += DoCritRetaliateBuff;
            //GetHitBehavior += WatchGetHit;
        }

        private void DoCritRetaliateBuff(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.damage > 0 && !damageInfo.rejected && victim.TryGetComponent(out CharacterBody victimBody))
            {
                Inventory inv = victimBody.inventory;
                if (inv)
                {
                    int itemCount = GetCount(victimBody);
                    if (itemCount > 0)
                    {
                        victimBody.ClearTimedBuffs(watchCritBuff);
                        float duration = buffDurationStack * (itemCount - 1) + buffDurationBase;
                        for (int i = 0; i < buffTotal; i++)
                        {
                            victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration * (float)(i + 1) / (float)buffTotal);
                        }
                        //victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        private void WatchGetHit(CharacterBody body, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (damageInfo.procCoefficient > 0 && damageInfo.damage > 0 && !damageInfo.rejected)
            {
                Inventory inv = victimBody.inventory;
                if (inv)
                {
                    int itemCount = GetCount(victimBody);
                    if (itemCount > 0)
                    {
                        victimBody.ClearTimedBuffs(watchCritBuff);
                        float duration = buffDurationStack * (itemCount - 1) + buffDurationBase;
                        for (int i = 0; i < buffTotal; i++)
                        {
                            victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration * (float)(i + 1) / (float)buffTotal);
                        }
                        //victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration);
                    }
                }
            }
        }

        private void WatchCritChance(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(watchCritBuff);
            args.critAdd += critChancePerBuff * buffCount;

            if (GetCount(sender) > 0)
                args.critAdd += 2;
        }
    }
}
