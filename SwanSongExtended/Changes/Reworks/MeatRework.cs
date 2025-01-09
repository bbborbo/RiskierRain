using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public void ReworkFreshMeat()
        {
            ChangeBuffStacking(nameof(JunkContent.Buffs.MeatRegenBoost), true);
            GetStatCoefficients += LetMeatActuallyStack;
            On.RoR2.GlobalEventManager.OnCharacterDeath += MeatRegen;
            LanguageAPI.Add("ITEM_FLATHEALTH_PICKUP", "Regenerate health after killing an enemy.");
            LanguageAPI.Add("ITEM_FLATHEALTH_DESC", "Increases <style=cIsHealing>base health regeneration</style> by <style=cIsHealing>+2 hp/s</style> " +
                "for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> after killing an enemy.");
        }
        private void MeatRegen(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            if (attackerBody != null && attackerBody.inventory != null)
            {
                Inventory inv = attackerBody.inventory;
                int meatCount = inv.GetItemCount(RoR2Content.Items.FlatHealth);
                if (meatCount > 0)
                {
                    attackerBody.AddTimedBuffAuthority(JunkContent.Buffs.MeatRegenBoost.buffIndex, 3 * meatCount);
                }
            }
            orig(self, damageReport);
        }
        private void LetMeatActuallyStack(CharacterBody sender, StatHookEventArgs args)
        {
            int meatBuffCount = sender.GetBuffCount(JunkContent.Buffs.MeatRegenBoost);

            if (meatBuffCount > 1)
            {
                args.baseRegenAdd += 2 * (1 + 0.2f * (sender.level - 1)) * (meatBuffCount - 1);
            }

            Inventory inv = sender.inventory;
            if (inv != null)
            {
                //this is so fucking lazy but whatever
                args.baseHealthAdd -= inv.GetItemCount(RoR2Content.Items.FlatHealth) * 25;
            }
        }
    }
}
