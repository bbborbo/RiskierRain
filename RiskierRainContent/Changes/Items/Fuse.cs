using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;

namespace RiskierRain.Items
{
    class Fuse : ItemBase<Fuse>
    {
        public static GameObject fuseNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef fuseRecharge;
        public static float fuseRechargeTime = 1;

        public static float baseShield = 40;
        public static float radiusBase = 16;
        public static float radiusStack = 4;

        public static float minStunDuration = 0.5f;
        public static float maxStunDuration = 4f;
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;

        public override string ItemName => "Volatile Fuse";

        public override string ItemLangTokenName => "BORBOFUSE";

        public override string ItemPickupDesc => "Creates a Shocking nova when your shields break.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{baseShield} shield</style> <style=cStack>(+{baseShield} per stack)</style>. " +
            $"<style=cIsUtility>Breaking your shields</style> creates a nova that " +
            $"<style=cIsUtility>Shocks</style> enemies within <style=cIsUtility>{radiusBase}m</style> " +
            $"<style=cStack>(+{radiusStack} per stack)</style>. " +
            $"<style=cIsDamage>Shock duration scales with shield health</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };
        //testing egg model
        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += FuseTakeDamage;
            GetStatCoefficients += FuseShieldBonus;
        }

        private void FuseShieldBonus(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                args.baseShieldAdd += baseShield * itemCount;
            }
        }

        private void FuseTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            bool hadShieldBefore = HasShield(self);
            CharacterBody body = self.body;
            int fuseItemCount = GetCount(body);

            orig(self, damageInfo);

            if (hadShieldBefore && !HasShield(self) && self.alive)
            {
                if (fuseItemCount > 0 && !body.HasBuff(fuseRecharge))
                {
                    float maxShield = self.body.maxShield;
                    float maxHealth = self.body.maxHealth;
                    float shieldHealthFraction = maxShield / (maxHealth + maxShield);

                    float currentRadius = radiusBase + radiusStack * (fuseItemCount - 1);

                    EffectManager.SpawnEffect(fuseNovaEffectPrefab, new EffectData
                    {
                        origin = self.transform.position,
                        scale = currentRadius
                    }, true);
                    BlastAttack fuseNova = new BlastAttack()
                    {
                        baseDamage = self.body.damage,
                        radius = currentRadius,
                        procCoefficient = Mathf.Lerp(minStunDuration, maxStunDuration, shieldHealthFraction),
                        position = self.transform.position,
                        attacker = self.gameObject,
                        crit = Util.CheckRoll(self.body.crit, self.body.master),
                        falloffModel = BlastAttack.FalloffModel.None,
                        damageType = DamageType.Stun1s,
                        teamIndex = TeamComponent.GetObjectTeam(self.gameObject)
                    };
                    fuseNova.Fire();

                    self.body.AddTimedBuffAuthority(fuseRecharge.buffIndex, fuseRechargeTime);
                }
            }
        }

        public static bool HasShield(HealthComponent hc)
        {
            return hc.shield > 1;
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateBuff()
        {
            fuseRecharge = ScriptableObject.CreateInstance<BuffDef>();
            {
                fuseRecharge.name = "FuseRechargeDebuff";
                fuseRecharge.buffColor = Color.cyan;
                fuseRecharge.canStack = false;
                fuseRecharge.isDebuff = true;
                fuseRecharge.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffTeslaIcon");
            };
            Assets.buffDefs.Add(fuseRecharge);
        }
    }
}
