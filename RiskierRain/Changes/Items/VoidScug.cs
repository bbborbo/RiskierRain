using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class VoidScug : ItemBase<VoidScug>
    {
        public static GameObject scugNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static float radiusBase = 16;
        public static float radiusStack = 4;

        public override string ItemName => "Cautious scug";

        public override string ItemLangTokenName => "VOIDSCUG";

        public override string ItemPickupDesc => "Chill enemies when hit. Recharges when out of danger.";

        public override string ItemFullDescription => "fucking     SCUUUUUUUUUUUUUGGGGG";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility};

        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");
        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += ScugTakeDamage;
        }

        private void ScugTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            bool danger = !self.body.outOfDanger;
            CharacterBody body = self.body;
            int scugItemCount = GetCount(body);
            orig(self, damageInfo);

            if (scugItemCount > 0 && danger)
            {
                ScugBlast(body, scugItemCount);
            }

        }
        void ScugBlast(CharacterBody body, int itemCount)
        {
            float currentRadius = radiusBase + radiusStack * (itemCount - 1);

            EffectManager.SpawnEffect(scugNovaEffectPrefab, new EffectData
            {
                origin = body.transform.position,
                scale = currentRadius
            }, true);
            BlastAttack scugNova = new BlastAttack()
            {
                baseDamage = body.damage,
                radius = currentRadius,
                procCoefficient = 0.5f,
                position = body.transform.position,
                attacker = body.gameObject,
                crit = Util.CheckRoll(body.crit, body.master),
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.SlowOnHit,
                teamIndex = TeamComponent.GetObjectTeam(body.gameObject)
            };
            scugNova.Fire();
        }
        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
