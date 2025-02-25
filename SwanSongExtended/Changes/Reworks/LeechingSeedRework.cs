using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static MoreStats.OnHit;
using static SwanSongExtended.Modules.Language;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public static ItemDef seedItemDef;
        public static float seedRegenDurationBase = 0.25f;
        public static float seedRegenDurationStack = 0.25f;
        public void ReworkLeechingSeed()
        {
            seedItemDef = RetierItem("Seed", ItemTier.Tier1);
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += FuckLeechingSeed;
            GetHitBehavior += NewSeedBehavior;
            LanguageAPI.Add("ITEM_SEED_PICKUP", "Dealing damage heals you.");
            LanguageAPI.Add("ITEM_SEED_DESC", $"Dealing damage increases <style=cIsHealing>base health regeneration</style> by <style=cIsHealing>+2 hp/s</style> " +
                $"for <style=cIsUtility>{seedRegenDurationBase}s</style> <style=cStack>(+{seedRegenDurationStack}s per stack)</style>.");
        }

        private void NewSeedBehavior(CharacterBody body, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (!damageInfo.procChainMask.HasProc(ProcType.HealOnHit))
            {
                Inventory inv = body.inventory;
                if(inv != null)
                {
                    int seedCount = inv.GetItemCount(seedItemDef);
                    if (seedCount > 0)
                    {
                        ProcChainMask procChainMask = damageInfo.procChainMask;
                        procChainMask.AddProc(ProcType.HealOnHit);
                        body.AddTimedBuff(JunkContent.Buffs.MeatRegenBoost, (seedRegenDurationBase + seedRegenDurationStack * (seedCount - 1)) * damageInfo.procCoefficient);
                    }
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
    }
}
