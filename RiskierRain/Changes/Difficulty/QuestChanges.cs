using RoR2;
using SwanSongExtended.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace RiskierRain.Changes
{
    public static partial class DifficultyChanges
    {
        #region trial of greed
        public static uint trialGreedRequiredMoney = 175;
        public static void ChangeTrialOfGreed()
        {
            On.RoR2.Quests.TrialQuests.TrialQuestSadismMoney.OnEnter += TrialQuestSadismMoney_OnEnter;
        }

        private static void TrialQuestSadismMoney_OnEnter(On.RoR2.Quests.TrialQuests.TrialQuestSadismMoney.orig_OnEnter orig, RoR2.Quests.TrialQuests.TrialQuestSadismMoney self)
        {
            self.requiredMoneyBase = trialGreedRequiredMoney;
            orig(self);
        }
        #endregion
    }
}
