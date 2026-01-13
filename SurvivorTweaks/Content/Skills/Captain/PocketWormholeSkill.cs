using BepInEx.Configuration;
using SurvivorTweaks.States.Captain;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SurvivorTweaks.Modules;
using static SurvivorTweaks.Modules.Language.Styling;
using R2API;

namespace SurvivorTweaks.Skills
{
    class PocketWormholeSkill : SkillBase<PocketWormholeSkill>
    {
        public DeployableAPI.GetDeployableSameSlotLimit GetWormholeSlotLimit;
        public static DeployableSlot wormholeDeployableSlot;
        public static GameObject ziplinePrefab;
        public override float BaseCooldown => 20f;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;

        public override Type BaseSkillDef => typeof(SkillDef);

        public override AssetBundle assetBundle => SurvivorTweaksPlugin.mainAssetBundle;
        public override string ConfigName => "Skills : Captain : Pocket Wormhole";

        [AutoConfig("Max Wormhole Count Base", 1)]
        public static float maxWormholesBase = 1;
        [AutoConfig("Max Wormhole Count Per Upgrade", 0.5f)]
        public static float maxWormholesUpgrade = 0.5f;
        [AutoConfig("Max Wormhole Distance", 60)]
        public static int maxTunnelDistance = 60;
        [AutoConfig("Max Wormhole Duration", 999)]
        public static float maxTunnelDuration = 999;

        [AutoConfig("Base Enter Duration", 0.8f)]
        public static float baseEnterDuration = 0.8f;
        [AutoConfig("Base Exit Duration", 0.7f)]
        public static float baseExitDuration = 0.7f;

        public override string SkillName => "Pocket Wormhole";

        public override string SkillDescription => $"Create a {UtilityColor("quantum tunnel")} for ALL allies to use. " +
            $"Lasts until replaced.";

        public override string TOKEN_IDENTIFIER => "CAPTAINTUNNEL";

        //public override Type RequiredUnlock => (typeof(UgornsMusicUnlock));
        public override Sprite Icon => assetBundle.LoadAsset<Sprite>("Assets/Icons/pocketwormhole.png");

        public override Type ActivationState => typeof(PocketWormhole);

        public override string CharacterName => "CaptainBody";

        public override SkillSlot SkillSlot => SkillSlot.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                mustKeyPress: true,
                isCombatSkill: false,
                beginSkillCooldownOnSkillEnd: true,
                canceledFromSprinting: true,
                suppressSkillActivation: true
            );
        public override void Init()
        {
            base.Init();
            GetWormholeSlotLimit += GetMaxWormholes;
            wormholeDeployableSlot = DeployableAPI.RegisterDeployableSlot(GetWormholeSlotLimit);
            Content.AddEntityState(typeof(FireWormhole));
            SurvivorTweaksPlugin.LoadAsync<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Gateway.Gateway_asset, (equip) =>
            {
                equip.canDrop = false;
                equip.enigmaCompatible = false;
                equip.canBeRandomlyTriggered = false;
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Gateway.Zipline_prefab, (zipline) =>
            {
                ziplinePrefab = zipline.InstantiateClone("PocketWormholeZipline", true);
                if (!ziplinePrefab.TryGetComponent(out TeamFilter teamFilter))
                {
                    teamFilter = ziplinePrefab.AddComponent<TeamFilter>();
                }
                if (!ziplinePrefab.TryGetComponent(out GenericOwnership genericOwnership))
                {
                    genericOwnership = ziplinePrefab.AddComponent<GenericOwnership>();
                }
                if (!ziplinePrefab.TryGetComponent(out Deployable deployableComponent))
                {
                    deployableComponent = ziplinePrefab.AddComponent<Deployable>();
                }
            });
        }

        public override void Hooks()
        {
        }


        private int GetMaxWormholes(CharacterMaster self, int deployableCountMultiplier)
        {
            int wormholes = 
                (int)maxWormholesBase + 
                Mathf.CeilToInt(self.inventory.GetItemCountPermanent(RoR2Content.Items.SecondarySkillMagazine) * maxWormholesUpgrade);
            GameObject body = self.GetBodyObject();
            return wormholes * deployableCountMultiplier;
        }
    }
}
