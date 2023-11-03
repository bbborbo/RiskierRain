using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using RiskierRain.States;

namespace RiskierRain.Skills
{
    class FireTurretFlamer : SkillBase<FireTurretFlamer>
    {
        public static bool initialized = false;
        public override string SkillName => "Spew Motor Spirit Breath";

        public override string SkillDescription => "Killeth";

        public override string SkillLangTokenName => "FIRETURRETFLAMER";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(EntityStates.Mage.Weapon.Flamethrower);

        public override string CharacterName => "";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData 
        { 
            baseRechargeInterval = 8f,
            baseMaxStock = 1,
            rechargeStock = 1,
            resetCooldownTimerOnUse = true,
            beginSkillCooldownOnSkillEnd = true
        };

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateSkill();
            CreateLang();
            Hooks();
            initialized = true;

            if (PlaceFlamerTurret.initialized)
                PlaceFlamerTurret.CreateFlamerTurret();
        }
    }
}
