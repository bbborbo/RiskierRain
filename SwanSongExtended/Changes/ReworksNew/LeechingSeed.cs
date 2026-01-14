using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static MoreStats.OnHit;
using static SwanSongExtended.Modules.Language;

namespace SwanSongExtended.Changes
{
    public class LeechingSeed : ReworkBase<LeechingSeed>
    {
        public static float seedRegenDurationBase = 0.25f;
        public static float seedRegenDurationStack = 0.25f;

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Seed.Seed_asset;

        public override string ItemName => "Leeching Seed";

        public override string ItemPickupDesc => null;

        public override string ItemFullDesc => null;

        public override void OnItemLoaded(ItemDef item)
        {
            base.OnItemLoaded(item);

            item.tier = ItemTier.Tier1;
            item.deprecatedTier = ItemTier.Tier1;
            Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Leeching_Seed.png");
            if (sprite)
                itemDef.pickupIconSprite = sprite;
        }
        public override void Hooks()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += FuckLeechingSeed;
        }
        public static void NewSeedBehavior(CharacterBody body, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (!damageInfo.procChainMask.HasProc(ProcType.HealOnHit))
            {
                Inventory inv = body.inventory;
                if (inv != null)
                {
                    int seedCount = inv.GetItemCountEffective(instance.itemDef);
                    if (seedCount > 0)
                    {
                        ProcChainMask procChainMask = damageInfo.procChainMask;
                        procChainMask.AddProc(ProcType.HealOnHit);
                        body.AddTimedBuff(JunkContent.Buffs.MeatRegenBoost, (seedRegenDurationBase + seedRegenDurationStack * (seedCount - 1)) * damageInfo.procCoefficient);
                    }
                }
            }
        }

        public static void FuckLeechingSeed(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4((int)ProcType.HealOnHit),
                x => x.MatchCallOrCallvirt("RoR2.ProcChainMask", nameof(RoR2.ProcChainMask.HasProc))
                );
            if (!b)
                return;
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<bool, DamageInfo, bool>>((cantProc, damageInfo) =>
            {
                return cantProc || !(damageInfo.damageType.IsDamageSourceSkillBased || damageInfo.damageType.damageSource == DamageSource.Equipment);
            });

            //int seedLoc = 14;
            //c.GotoNext(MoveType.After,
            //    x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Seed"),
            //    x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
            //    x => x.MatchStloc(out seedLoc)
            //    );
            //c.Index--;
            //c.Emit(OpCodes.Pop);
            //c.Emit(OpCodes.Ldc_I4, 0);
        }
    }
}
