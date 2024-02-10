using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRainContent.RiskierRainContent;
using static RiskierRainContent.JumpStatHook;
using On.RoR2.Items;
using HarmonyLib;
using EntityStates.Bandit2;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using UnityEngine.Networking;
using RiskierRainContent.CoreModules;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.Items
{
    class BottleFart : ItemBase<BottleFart>
    {
        static GameObject fartZone;
        static float resetFrequency = 3f;
        static GameObject novaEffectPrefab = null;// LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova");
        internal static float smokeBombRadius = 9f;
        static float fartBaseDamageCoefficient = 1f;
        static float fartStackDamageCoefficient = 1f;

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Sealed Pestilence";

        public override string ItemLangTokenName => "FARTBOTTLE";

        public override string ItemPickupDesc => "Gain an extra jump. Jumping near enemies cripples and damages them. " +
            "<style=cIsVoid>Corrupts all Cloud In A Bottles.</style>";

        public override string ItemFullDescription => $"Gain an extra jump. Double jumping within {smokeBombRadius}m of an enemy " +
            $"produces a <style=cIsDamage>toxic gas</style>, " +
            $"dealing <style=cIsDamage>{Tools.ConvertDecimal(fartBaseDamageCoefficient * resetFrequency)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(fartStackDamageCoefficient * resetFrequency)} per stack)</style> damage per second " +
            $"and Crippling enemies inside. " +
            $"Cannot be reactivated for <style=cIsUtility>{FartBottleBehavior.cooldownDuration}</style> seconds. " +
            $"<style=cIsVoid>Corrupts all Cloud In A Bottles.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/voidBottle.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_FARTBOTTLE.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            JumpStatCoefficient += FartJump;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    FartBottleBehavior ringBehavior = self.AddItemBehavior<FartBottleBehavior>(GetCount(self));
                }
            }
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = BottleCloud.instance.ItemsDef, //consumes cloud in a bottle
                itemDef2 = BottleFart.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }

        private void FartJump(CharacterBody sender, ref int jumpCount)
        {
            if (GetCount(sender) > 0)
            {
                jumpCount += 1;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateProjectile();
            //CreateBuff();
            Hooks();
        }
        private void CreateProjectile()
        {
            GameObject mushroomGas = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/MiniMushroom/SporeGrenadeProjectileDotZone.prefab").WaitForCompletion();
            if (mushroomGas == null)
                return;

            fartZone = mushroomGas.InstantiateClone("FartJarGas", true);

            fartZone.transform.localScale = Vector3.one * smokeBombRadius / 6;

            ProjectileDotZone pdz = fartZone.GetComponent<ProjectileDotZone>();
            if (pdz)
            {
                pdz.resetFrequency = resetFrequency;
                pdz.damageCoefficient = 1 / resetFrequency;
                pdz.overlapProcCoefficient = (1 / resetFrequency) / (3); //3 is the duration of cripple proc, this makes it the minimum proc coefficient for constant cripple
            }

            ProjectileDamage dmg = fartZone.GetComponent<ProjectileDamage>();
            if(dmg != null)
                dmg.damageType = DamageType.CrippleOnHit;
        }


        internal static void CreateFartCloud(CharacterBody self, int stack)
        {
            if (fartZone == null)
                return;
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo();
            fireProjectileInfo.owner = self.gameObject;
            fireProjectileInfo.crit = Util.CheckRoll(self.crit, self.master);
            fireProjectileInfo.position = self.transform.position - (Vector3.down * self.radius);
            fireProjectileInfo.projectilePrefab = fartZone;
            fireProjectileInfo.damage = self.damage * GetStackValue(fartBaseDamageCoefficient, fartStackDamageCoefficient, stack);

            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }
    }

    public class FartBottleBehavior : CharacterBody.ItemBehavior
    {
        public static float cooldownDuration = 5;
        public static float cooldownReductionPerStack = 0.2f;
        float cooldownTimer = 0;
        float bombRadiusSqr;

        void Start()
        {
            bombRadiusSqr = BottleFart.smokeBombRadius * BottleFart.smokeBombRadius;
            OnJumpEvent += CloudBottleJump;
        }
        void OnDestroy()
        {
            OnJumpEvent -= CloudBottleJump;
        }

        private void CloudBottleJump(CharacterMotor motor, ref float verticalBonus)
        {
            if (cooldownTimer > 0)
                return;

            CharacterBody body = motor.body;
            if (body && body.inventory?.GetItemCount(BottleFart.instance.ItemsDef) <= 0)
                return;

            if (!IsDoubleJump(motor, body))
                return;


            TeamIndex teamIndex = this.body.teamComponent.teamIndex;
            int num = 0;
            for (TeamIndex teamIndex2 = TeamIndex.Neutral; teamIndex2 < TeamIndex.Count; teamIndex2 += 1)
            {
                bool flag2 = teamIndex2 != teamIndex && teamIndex2 > TeamIndex.Neutral;
                if (flag2)
                {
                    foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(teamIndex2))
                    {
                        bool flag3 = (teamComponent.transform.position - this.body.corePosition).sqrMagnitude <= bombRadiusSqr;
                        if (flag3)
                        {
                            num++;
                            break;
                        }
                    }
                }
                if (num > 0)
                    break;
            }

            if (num > 0)
            {
                BottleFart.CreateFartCloud(motor.body, stack);
                verticalBonus += BottleCloud.verticalBonusOnCloudJump;
                cooldownTimer = cooldownDuration;// * Mathf.Pow(1 - cooldownReductionPerStack, stack - 1);
            }
        }

        private void FixedUpdate()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.fixedDeltaTime;
            }
        }
    }
}
