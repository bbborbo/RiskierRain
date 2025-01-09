using BepInEx.Configuration;
using SwanSongExtended.States.Captain;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Skills
{
    class PocketWormholeSkill : SkillBase<PocketWormholeSkill>
    {
        public override float BaseCooldown => 15f;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;

        public override Type BaseSkillDef => typeof(SkillDef);

        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;
        public override string ConfigName => "Skills : Captain : Pocket Wormhole";

        [AutoConfig("Max Wormhole Distance", 60)]
        public static int maxTunnelDistance = 60;
        [AutoConfig("Max Wormhole Duration", 15)]
        public static float maxTunnelDuration = 15;

        [AutoConfig("Base Enter Duration", 0.8f)]
        public static float baseEnterDuration = 0.8f;
        [AutoConfig("Base Exit Duration", 0.7f)]
        public static float baseExitDuration = 0.7f;

        public override string SkillName => "Pocket Wormhole";

        public override string SkillDescription => $"Create a {UtilityColor("quantum tunnel")} for ALL allies to use. " +
            $"Lasts for {UtilityColor(maxTunnelDuration.ToString())} seconds.";

        public override string SkillLangTokenName => "CAPTAINTUNNEL";

        public override UnlockableDef UnlockDef => null;
        public override Sprite Icon => assetBundle.LoadAsset<Sprite>(CommonAssets.iconsPath + "Skill/" + "PocketWormhole" + ".png");

        public override Type ActivationState => typeof(PocketWormhole);

        public override string CharacterName => "CaptainBody";

        public override SkillSlot SkillSlot => SkillSlot.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
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
            RoR2Content.Equipment.Gateway.canBeRandomlyTriggered = false;
            orig(newEquipmentDefs);
        }
    }
}
