using BepInEx;
using R2API;
using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        float itemBlastDamageCoefficient = 30; //60
        void FixVagrantNova()
        {
            EntityStates.VagrantNovaItem.DetonateState.blastProcCoefficient = 0.3f;
            EntityStates.VagrantNovaItem.DetonateState.blastDamageCoefficient = itemBlastDamageCoefficient;
            LanguageAPI.Add("ITEM_NOVAONLOWHEALTH_DESC", 
                $"Falling below <style=cIsHealth>25% health</style> causes you to explode, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(itemBlastDamageCoefficient)} base damage</style>. " +
                $"Recharges every <style=cIsUtility>30 / (2 <style=cStack>+1 per stack</style>) seconds</style>.");

            On.EntityStates.VagrantMonster.ChargeMegaNova.OnEnter += (orig, self) =>
            {
                orig(self);
                self./*private*/duration = EntityStates.VagrantMonster.ChargeMegaNova.baseDuration;
            };
            On.EntityStates.VagrantNovaItem.ChargeState.OnEnter += (orig, self) =>
            {
                orig(self);
                self./*private*/duration = 3;
            };
        }
    }
}
