using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Changes
{
    public class Meat : ReworkBase<Meat>
    {
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FlatHealth.FlatHealth_asset;

        public override string ItemName => "Fresh Meat";

        public override string ItemPickupDesc =>
            "Regenerate health after killing an enemy.";

        public override string ItemFullDesc =>
            "Increases <style=cIsHealing>base health regeneration</style> by <style=cIsHealing>+2 hp/s</style> " +
            "for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> after killing an enemy.";

        public override void Init()
        {
            SwanSongPlugin.ChangeBuffStacking(nameof(JunkContent.Buffs.MeatRegenBoost), true);
            base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += LetMeatActuallyStack;
            IL.RoR2.CharacterBody.RecalculateStats += RemoveMeatHealth;
            GlobalEventManager.onCharacterDeathGlobal += MeatRegen;
        }

        private void RemoveMeatHealth(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", nameof(RoR2Content.Items.FlatHealth)),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            if (!b)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(RemoveMeatHealth));
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        private void MeatRegen(DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            if (attackerBody != null && attackerBody.inventory != null)
            {
                Inventory inv = attackerBody.inventory;
                int meatCount = inv.GetItemCountEffective(RoR2Content.Items.FlatHealth);
                if (meatCount > 0)
                {
                    attackerBody.AddTimedBuffAuthority(JunkContent.Buffs.MeatRegenBoost.buffIndex, 3 * meatCount);
                }
            }
        }
        private void LetMeatActuallyStack(CharacterBody sender, StatHookEventArgs args)
        {
            int meatBuffCount = sender.GetBuffCount(JunkContent.Buffs.MeatRegenBoost);

            if (meatBuffCount > 1)
            {
                args.baseRegenAdd += 2 * (1 + 0.2f * (sender.level - 1)) * (meatBuffCount - 1);
            }
        }
    }
}
