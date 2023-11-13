using BepInEx.Configuration;
using RiskierRainContent.EntityState.Bandit;
using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRainContent.Skills
{
    class MinigunSkill : SkillBase
    {
        public static int baseMaxStock = 6;

        public override string SkillName => "Breach";

        public override string SkillDescription => $"Fire an automatic breach " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(FireMinigun.damageCoeff)} damage</style>. " +
            $"Can hold up to {baseMaxStock} bullets; reloads <style=cIsUtility>2 at a time</style>.";

        public override string SkillLangTokenName => "BANDITMINIGUN";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(FireMinigun);

        public override string CharacterName => "Bandit2Body";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 1.2f,
                baseMaxStock: baseMaxStock,
                rechargeStock: 2,
                interruptPriority: InterruptPriority.Any,
                mustKeyPress: false,
                beginSkillCooldownOnSkillEnd: true,
                resetCooldownTimerOnUse: true
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateSkill();
            Hooks();
        }
    }
}
