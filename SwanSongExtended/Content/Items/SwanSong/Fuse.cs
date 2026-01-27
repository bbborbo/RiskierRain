using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;
using System.Linq;
using RoR2.Orbs;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class Fuse : ItemBase<Fuse>
    {
        public override string ConfigName => "Items : Fuse";
        public static GameObject fuseNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef fuseRecharge;
        public static float fuseRechargeTime = 1;

        public static float baseShield = 25;
        public static float radiusBase = 40;
        public static float radiusStack = 4;

        public static float minStunDuration = 2f;
        public static float maxStunDuration = 10f;

        public static int targetCountBase = 3;
        public static int targetCountStack = 1;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override string ItemName => "Volatile Fuse";

        public override string ItemLangTokenName => "BORBOFUSE";

        public override string ItemPickupDesc => "Creates a stunning nova when your shields break.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{baseShield} shield</style> <style=cStack>(+{baseShield} per stack)</style>. " +
            $"Breaking your shields <style=cIsUtility>Shocks</style> up to " +
            $"{targetCountBase} {StackText($"+{targetCountStack}")} enemies within <style=cIsUtility>{radiusBase}m</style> " +
            $"<style=cStack>(+{radiusStack} per stack)</style>. " +
            $"<style=cIsDamage>Shock duration scales with shield health</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };
        //testing egg model
        public override GameObject ItemModel => LoadDropPrefab("mdlFuse");

        public override Sprite ItemIcon => LoadItemIcon("texIconFuse");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            fuseRecharge = Content.CreateAndAddBuff(
                "bdFuseCooldown",
                LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffTeslaIcon"),
                Color.gray,
                false, true);
            fuseRecharge.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            fuseRecharge.isHidden = true;
            base.Init();
        }

        public override void Hooks()
        {
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
    }

    public class FuseBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => Fuse.instance.ItemsDef;
        bool hadShield = false;
        void Start()
        {
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);
            hadShield = HasShield();
        }
        void OnDestroy()
        {
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
        }
        void FixedUpdate()
        {
            hadShield = HasShield();
        }

        bool HasShield()
        {
            return (body.healthComponent?.shield ?? 0) > 0;
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if (!hadShield || HasShield() || !body.healthComponent.alive)
                return;

            if (stack > 0 && !body.HasBuff(Fuse.fuseRecharge))
            {
                float maxShield = body.maxShield;
                float maxHealth = body.maxHealth;
                float shieldHealthFraction = maxShield / (maxHealth + maxShield);

                TeamIndex team = TeamIndex.Player;
                if (body.teamComponent)
                    team = body.teamComponent.teamIndex;
                bool crit = Util.CheckRoll(body.crit, body.master);
                float procCoefficient = Mathf.Lerp(Fuse.minStunDuration, Fuse.maxStunDuration, shieldHealthFraction) / 5;
                float currentRadius = Fuse.radiusBase + Fuse.radiusStack * (stack - 1);
                int targetCount = Fuse.targetCountBase + Fuse.targetCountStack * (stack - 1);

                BullseyeSearch search = new BullseyeSearch();
                search.searchOrigin = body.transform.position;
                search.maxDistanceFilter = currentRadius;
                search.teamMaskFilter.RemoveTeam(team);
                search.sortMode = BullseyeSearch.SortMode.Distance;
                search.RefreshCandidates();

                List<HurtBox> results = search.GetResults().ToList();
                for (int i = 0; i < Mathf.Min(targetCount, results.Count()); i++)
                {
                    HurtBox hurtBox = results[i];
                    if (hurtBox)
                    {
                        LightningOrb lightningOrb = new LightningOrb();
                        lightningOrb.bouncedObjects = new List<HealthComponent>();
                        lightningOrb.attacker = body.gameObject;
                        lightningOrb.teamIndex = team;
                        lightningOrb.damageValue = body.damage;
                        lightningOrb.isCrit = crit;
                        lightningOrb.origin = body.corePosition;
                        lightningOrb.bouncesRemaining = 0;
                        lightningOrb.lightningType = LightningOrb.LightningType.Loader;
                        lightningOrb.procCoefficient = procCoefficient;
                        lightningOrb.target = hurtBox;
                        lightningOrb.damageType = new DamageTypeCombo(DamageType.Shock5s, DamageTypeExtended.Generic, DamageSource.NoneSpecified);
                        OrbManager.instance.AddOrb(lightningOrb);
                    }
                }

                body.AddTimedBuffAuthority(Fuse.fuseRecharge.buffIndex, Fuse.fuseRechargeTime);
                EffectManager.SpawnEffect(Fuse.fuseNovaEffectPrefab, new EffectData
                {
                    origin = transform.position,
                    scale = currentRadius
                }, true);
                //BlastAttack fuseNova = new BlastAttack()
                //{
                //    baseDamage = self.body.damage,
                //    radius = currentRadius,
                //    procCoefficient = Mathf.Lerp(minStunDuration, maxStunDuration, shieldHealthFraction),
                //    position = self.transform.position,
                //    attacker = self.gameObject,
                //    crit = Util.CheckRoll(self.body.crit, self.body.master),
                //    falloffModel = BlastAttack.FalloffModel.None,
                //    damageType = DamageType.Stun1s,
                //    teamIndex = TeamComponent.GetObjectTeam(self.gameObject)
                //};
                //fuseNova.Fire();
            }
        }
    }
}
