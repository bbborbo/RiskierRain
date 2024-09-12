using BepInEx.Configuration;
using RiskierRainContent.EntityState;
using RiskierRainContent.EntityState.Captain;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using RiskierRainContent.CoreModules;

namespace RiskierRainContent.Skills
{
    class ThermalCannonSkill : SkillBase
    {
        public override string SkillName => "Thermal Cannon";

        public override string SkillDescription => $"Fire a weak <style=cIsUtility>Sonic Boom</style> that <style=cIsDamage>damages</style> enemies " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(ThermalCannonFire.damageCoefficient)} damage.</style> " +
            $"Ignites every target hit for <style=cIsDamage>{Tools.ConvertDecimal(ThermalCannonFire.burnDuration)}</style> damage.";

        public override string SkillLangTokenName => "CAPTAINFLAMER";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "ThermalCannon";

        public override Type ActivationState => typeof(ThermalCannonPrep);

        public override string CharacterName => "CaptainBody";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 4,
                interruptPriority: InterruptPriority.Any,
                mustKeyPress: false,
                beginSkillCooldownOnSkillEnd: true,
                resetCooldownTimerOnUse: true,
                baseMaxStock: 3
            );

        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            CoreModules.Assets.RegisterEntityState(typeof(ThermalCannonFire));

            KeywordTokens = new string[1] { "KEYWORD_SONIC_BOOM" };
            CreateLang();
            CreateSkill();
        }
    }
}
