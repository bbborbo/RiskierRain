using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRainContent.CoreModules.StatHooks;

namespace RiskierRainContent.Items
{
    class LeechingSeed : ItemBase<LeechingSeed>
    {
        public static float regenDurationPerStack = 0.5f;
        public override string ItemName => "Leeching Seed";

        public override string ItemLangTokenName => throw new NotImplementedException();

        public override string ItemPickupDesc => throw new NotImplementedException();

        public override string ItemFullDescription => throw new NotImplementedException();

        public override string ItemLore => throw new NotImplementedException();

        public override ItemTier Tier => throw new NotImplementedException();

        public override ItemTag[] ItemTags => throw new NotImplementedException();

        public override GameObject ItemModel => throw new NotImplementedException();

        public override Sprite ItemIcon => throw new NotImplementedException();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            IL.RoR2.GlobalEventManager.OnHitEnemy += FuckLeechingSeed;
            GetHitBehavior += NewSeedBehavior;
            LanguageAPI.Add("ITEM_SEED_PICKUP", "Dealing damage heals you.");
            LanguageAPI.Add("ITEM_SEED_DESC", $"Dealing damage increases <style=cIsHealing>base health regeneration</style> by <style=cIsHealing>+2 hp/s</style> " +
                $"for <style=cIsUtility>{regenDurationPerStack}s</style> <style=cStack>(+{regenDurationPerStack}s per stack)</style>.");
        }

        private void NewSeedBehavior(CharacterBody body, DamageInfo damageInfo, GameObject victim)
        {
            if (!damageInfo.procChainMask.HasProc(ProcType.HealOnHit))
            {
                int seedCount = GetCount(body);
                if (seedCount > 0)
                {
                    ProcChainMask procChainMask = damageInfo.procChainMask;
                    procChainMask.AddProc(ProcType.HealOnHit);
                    body.AddTimedBuff(JunkContent.Buffs.MeatRegenBoost, regenDurationPerStack * seedCount * damageInfo.procCoefficient);
                }
            }
        }

        private void FuckLeechingSeed(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int seedLoc = 14;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Seed"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out seedLoc)
                );
            c.Index--;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }

        public override void Init(ConfigFile config)
        {
            ItemsDef = RiskierRainContent.RetierItem(nameof(RoR2Content.Items.Seed), ItemTier.Tier1);
            Hooks();
        }
    }
}
