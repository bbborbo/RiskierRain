using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.CoreModules.StatHooks;
using static R2API.RecalculateStatsAPI;
using EntityStates;
using RiskierRain.CoreModules;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        #region buffs

        float elephantBuffDuration = 10;
        int elephantArmor = 200;
        private void JadeElephantChanges()
        {
            ChangeBuffStacking(nameof(RoR2Content.Buffs.ElephantArmorBoost), true);
            On.RoR2.EquipmentSlot.FireGainArmor += ChangeElephantDuration;
            GetStatCoefficients += ReduceElephantArmor;
            LanguageAPI.Add("EQUIPMENT_GAINARMOR_PICKUP", "Gain massive armor for 10 seconds.");
            LanguageAPI.Add("EQUIPMENT_GAINARMOR_DESC",
                "Gain <style=cIsDamage>200 armor</style> for <style=cIsUtility>10 seconds.</style>");
        }

        private void ReduceElephantArmor(CharacterBody sender, StatHookEventArgs args)
        {
            int elephantBuffCount = sender.GetBuffCount(RoR2Content.Buffs.ElephantArmorBoost);

            if (elephantBuffCount > 0)
            {
                args.armorAdd += (elephantBuffCount * elephantArmor) - 500;
            }
        }
        private bool ChangeElephantDuration(On.RoR2.EquipmentSlot.orig_FireGainArmor orig, EquipmentSlot self)
        {
            self.characterBody.AddTimedBuff(RoR2Content.Buffs.ElephantArmorBoost, elephantBuffDuration);
            return true;
        }
        #endregion

        #region damage
        float critHudDamageMul = 1;
        private void OcularHudBuff()
        {
            GetStatCoefficients += HudCritDamage;
            LanguageAPI.Add("EQUIPMENT_CRITONUSE_PICKUP", "Increased 'Critical Strike' damage. Gain 100% Critical Strike Chance for 8 seconds.");
            LanguageAPI.Add("EQUIPMENT_CRITONUSE_DESC",
                "<style=cIsHealth>Passively double Critical Strike Damage</style>. " +
                "On use, gain <style=cIsDamage>+100% Critical Strike Chance</style> for 8 seconds.");
        }


        private void HudCritDamage(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.equipmentSlot)
            {
                if(sender.equipmentSlot.equipmentIndex == RoR2Content.Equipment.CritOnUse.equipmentIndex)
                    args.critDamageMultAdd += critHudDamageMul;
            }
        }
        #endregion
    }
}
