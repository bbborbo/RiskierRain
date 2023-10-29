using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class FlameOrb : ItemBase<FlameOrb>
    {
        public static GameObject flameNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");//change this later
        const int flameRadius = 25;
        const float durationBase = 5;
        const float durationStack = 5;


        public override string ItemName => "Miniature Star";

        public override string ItemLangTokenName => "FLAMEORB";

        public override string ItemPickupDesc => "Taking damage at full health sets everything on fire, including you." +
            "";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.WorldUnique, ItemTag.Cleansable };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlLunarStar.prefab");

        public override Sprite ItemIcon => RiskierRainPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texEggIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += FlameOrbTakeDamage;
        }

        private void FlameOrbTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            CharacterBody body = self.body;
            int itemCount = GetCount(body);
            if (itemCount > 0 && self.combinedHealth >= self.fullCombinedHealth)
            {
                FlameOrbFlameOn(body, itemCount);
            }
            orig(self, damageInfo);
        }
        private void FlameOrbFlameOn(CharacterBody flamer, int itemCount)
        {
            EffectManager.SpawnEffect(flameNovaEffectPrefab, new EffectData
            {
                origin = flamer.transform.position,
                scale = flameRadius
            }, true);
            ApplyFlameSphere(flamer, itemCount);
            BlastAttack flameBoom = new BlastAttack()
            {
                baseDamage = flamer.damage,
                radius = flameRadius,
                procCoefficient = 0.5f,
                position = flamer.transform.position,
                attacker = flamer.gameObject,
                crit = Util.CheckRoll(flamer.crit, flamer.master),
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.Generic,
                teamIndex = TeamIndex.Neutral
            };
        }
        static void ApplyFlameSphere(CharacterBody body, int itemCount)
        {
            Vector3 corePosition = body.corePosition;
            flameSphereSearch.origin = corePosition;
            flameSphereSearch.mask = LayerIndex.entityPrecise.mask;
            flameSphereSearch.radius = flameRadius;
            flameSphereSearch.RefreshCandidates();
            flameSphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex));
            flameSphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
            flameSphereSearch.OrderCandidatesByDistance();
            flameSphereSearch.GetHurtBoxes(flameOnKillHurtBoxBuffer);
            flameSphereSearch.ClearCandidates();

            for (int i = 0; i < flameOnKillHurtBoxBuffer.Count; i++)
            {
                HurtBox hurtBox = flameOnKillHurtBoxBuffer[i];
                if (hurtBox.healthComponent)
                {
                    hurtBox.healthComponent.body.AddTimedBuff(RoR2Content.Buffs.OnFire, durationBase + durationStack * itemCount);
                }
            }
            flameOnKillHurtBoxBuffer.Clear();
        }
        private static readonly SphereSearch flameSphereSearch = new SphereSearch();
        private static readonly List<HurtBox> flameOnKillHurtBoxBuffer = new List<HurtBox>();
        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
