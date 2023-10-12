using BepInEx.Configuration;
using RiskierRain.Skills;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.Enemies.VoidDreamers
{
    class DreamersSimulateSkill : SkillBase
    {
        public override string SkillName => "Dream";

        public override string SkillDescription => "";

        public override string SkillLangTokenName => "DREAMERSDREAM";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(DreamersSimulateState);

        public override string CharacterName => "CommandoBody";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Utility;

        public override SimpleSkillData SkillData => new SimpleSkillData
                    (
                        baseRechargeInterval: 6,
                        interruptPriority: EntityStates.InterruptPriority.Skill
                    );
        public override void Hooks()
        {
            ;
        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateSkill();
        }
    }
}
