using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.CombatDirector;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.EliteModule;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Elites
{
    class FrenziedAspect : T1EliteEquipmentBase<FrenziedAspect>
    {
        #region config
        public override string ConfigName => "Elites : " + EliteModifier;

        [AutoConfig("Cooldown Reduction Fraction", 0.3f)]
        public static float cooldownReduction = 0.3f;
        [AutoConfig("Bonus Movement Speed Multiplier", 0.8f)]
        public static float moveSpeedBonus = 0.8f;
        [AutoConfig("Bonus Attack Speed Multiplier", 1.2f)]
        public static float atkSpeedBonus = 1.2f;
        #endregion

        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;


        public override string EliteEquipmentName => "Chir\u2019s Tempo"; //momentum, tempo, alacrity, velocity

        public override string EliteEquipmentPickupDesc => "Become an aspect of velocity.";

        public override string EliteAffixToken => "AFFIX_SPEED";

        public override string EliteModifier => "Frenzied";

        public override string EliteEquipmentFullDescription => "Increase movement, attack, and ability recharge speed.";

        public override string EliteEquipmentLore => "";
        public override float EliteHealthModifier => 2f; //soft elite health modifier, as opposed to 3f

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matFrenzied.mat");
        public override string EliteRampTextureName { get; set; } = "texRampFrenzied";

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;

        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteLightning/texBuffAffixBlue.tif").WaitForCompletion();
        public override Color EliteBuffColor => new Color(1.0f, 0.7f, 0.0f, 1.0f);


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += FrenziedStatBuff;
            On.RoR2.CharacterBody.RecalculateStats += FrenziedCooldownBuff;
            On.RoR2.GlobalEventManager.OnCharacterDeath += FrenziedTransferDeath;
        }

        public static float frenziedTransferDuration = 8f;
        public static float overloadingSmiteCountBase = 1;
        public static float overloadingSmiteCountPerRadius = 1f;
        public static float overloadingSmiteRangeBase = 24f;
        public static float overloadingSmiteRangePerRadius = 12f;
        private void FrenziedTransferDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            CharacterBody victimBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (victimBody != null && attackerBody != null)
            {
                if (victimBody.HasBuff(EliteBuffDef))
                {
                    int maxTransferCount = Mathf.CeilToInt(overloadingSmiteCountBase + victimBody.radius * overloadingSmiteCountPerRadius);
                    float range = overloadingSmiteRangeBase + victimBody.radius * overloadingSmiteRangePerRadius;

                    //procChainMask6.AddProc(ProcType.LightningStrikeOnHit);

                    SphereSearch sphereSearch = new SphereSearch
                    {
                        mask = LayerIndex.entityPrecise.mask,
                        origin = victimBody.transform.position,
                        queryTriggerInteraction = QueryTriggerInteraction.Collide,
                        radius = range
                    };

                    TeamMask teamMask = TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex);
                    List<HurtBox> hurtBoxesList = new List<HurtBox>();

                    sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().OrderCandidatesByDistance().GetHurtBoxes(hurtBoxesList);

                    int hurtBoxCount = hurtBoxesList.Count;

                    for(int i = 0; i < maxTransferCount; i++)
                    {
                        if (i >= hurtBoxCount)
                            break;

                        HurtBox targetHurtBox = hurtBoxesList[i];
                        if(targetHurtBox == null)
                            continue;

                        HealthComponent healthComponent = targetHurtBox.healthComponent;
                        CharacterBody enemyBody = healthComponent.body;
                        if (enemyBody == null || enemyBody == victimBody || enemyBody.HasBuff(EliteBuffDef))
                            continue;

                        enemyBody.AddTimedBuff(EliteBuffDef, frenziedTransferDuration);
                        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova"), new EffectData
                        {
                            origin = enemyBody.corePosition,
                            scale = 5
                        }, true);
                    }
                }
            }
            orig(self, damageReport);
        }

        private void FrenziedCooldownBuff(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (IsElite(self, EliteBuffDef))
            {
                float scale = 1 - cooldownReduction;
                if (self.skillLocator.primary)
                {
                    self.skillLocator.primary.cooldownScale *= scale;
                }
                if (self.skillLocator.secondary)
                {
                    self.skillLocator.secondary.cooldownScale *= scale;
                }
                if (self.skillLocator.utility)
                {
                    self.skillLocator.utility.cooldownScale *= scale;
                }
                if (self.skillLocator.special)
                {
                    self.skillLocator.special.cooldownScale *= scale;
                }
            }
        }

        private void FrenziedStatBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (IsElite(sender, EliteBuffDef))
            {
                args.moveSpeedMultAdd += moveSpeedBonus;
                args.baseAttackSpeedAdd += atkSpeedBonus;
            }
        }
        public override void Init()
        {
            base.Init();
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }
}
