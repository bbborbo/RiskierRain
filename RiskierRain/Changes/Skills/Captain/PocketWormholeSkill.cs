using BepInEx.Configuration;
using RiskierRain.EntityState.Captain;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.Skills
{
    class PocketWormholeSkill : SkillBase
    {
        public static int maxTunnelDistance = 60;
        public static float maxTunnelDuration = 15;

        public override string SkillName => "Pocket Wormhole";

        public override string SkillDescription => $"Create a <style=cIsUtility>quantum tunnel</style> for ALL allies to use. " +
            $"Reaches a max distance of up to <style=cIsUtility>{maxTunnelDistance}m</style> and lasts {maxTunnelDuration} seconds.";

        public override string SkillLangTokenName => "CAPTAINTUNNEL";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "PocketWormhole";

        public override Type ActivationState => typeof(PocketWormhole);

        public override string CharacterName => "CaptainBody";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 15,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: true,
                isCombatSkill: false,
                beginSkillCooldownOnSkillEnd: true
            );

        public override void Hooks()
        {
            On.RoR2.EquipmentCatalog.SetEquipmentDefs += Gah;
        }

        private void Gah(On.RoR2.EquipmentCatalog.orig_SetEquipmentDefs orig, EquipmentDef[] newEquipmentDefs)
        {
            RoR2Content.Equipment.Gateway.canDrop = false;
            RoR2Content.Equipment.Gateway.enigmaCompatible = false;
            orig(newEquipmentDefs);
        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateSkill();
            Hooks();
        }
    }
}
