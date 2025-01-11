using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class CobaltShield : ItemBase
    {
        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;
        public override string ConfigName => "Items : Raw Chicken";
        public static BuffDef cobaltDefense;
        public static float cobaltWaitTime = 0.25f;

        public static int baseArmor = 60;
        public static int baseStationaryArmor = 60;
        public static int stackStationaryArmor = 80;
        public override string ItemName => "Cobalt Shield";

        public override string ItemLangTokenName => "CUCKLER";

        public override string ItemPickupDesc => "Become immune to knockback. Greatly reduces incoming damage while stationary.";

        public override string ItemFullDescription => $"Become <style=cIsUtility>immune to ALL knockback</style>, and <style=cIsHealing>increase armor</style> " +
            $"by <style=cIsHealing>{baseArmor}</style> <style=cStack>(+{baseArmor} per stack)</style>. " +
            $"While stationary, gain " +
            $"<style=cIsHealing>{baseStationaryArmor}</style> <style=cStack>(+{stackStationaryArmor} per stack)</style> additional armor.";

        public override string ItemLore => "<style=cIsHealth>I cannot let you enter until you free me of my curse.</style>";

        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags { get; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>(CommonAssets.dropPrefabsPath + "Item/CobaltShield.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>(CommonAssets.iconsPath + "Item/texIconCobaltShield.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            cobaltDefense = Content.CreateAndAddBuff("bdCobaltDefense",
                assetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png"),
                new Color(0.15f, 0.4f, 0.9f),
                false, false);
            base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += CobaltArmorBuff;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterMotor.ApplyForce += ConditionalRemoveSelfForce;
            On.RoR2.HealthComponent.TakeDamageProcess += RemoveDamageForce;
        }

        private void RemoveDamageForce(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            int itemCount = GetCount(self.body);
            if(self.body != null && itemCount > 0)
            {
                damageInfo.force *= 0;
            }

            orig(self, damageInfo);
        }

        private void ConditionalRemoveSelfForce(On.RoR2.CharacterMotor.orig_ApplyForce orig, CharacterMotor self, Vector3 force, bool alwaysApply, bool disableAirControlUntilCollision)
        {
            CharacterBody body = self.body;
            if (body != null && GetCount(body) > 0 && body.HasBuff(cobaltDefense))
            {
                force *= 0;
            }

            orig(self, force, alwaysApply, disableAirControlUntilCollision);
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<ShieldItemBehavior>(GetCount(self));
            }
        }

        private void CobaltArmorBuff(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                int armor = baseArmor * itemCount;
                if (sender.HasBuff(cobaltDefense))
                {
                    armor += baseStationaryArmor + stackStationaryArmor * (itemCount - 1);
                }

                args.armorAdd += armor;
            }
        }
    }
    public class ShieldItemBehavior : CharacterBody.ItemBehavior
    {
        private void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                float notMovingStopwatch = this.body.notMovingStopwatch;

                if (stack > 0 && notMovingStopwatch >= CobaltShield.cobaltWaitTime)
                {
                    if (!body.HasBuff(CobaltShield.cobaltDefense))
                    {
                        this.body.AddBuff(CobaltShield.cobaltDefense);
                        return;
                    }
                }
                else if (body.HasBuff(CobaltShield.cobaltDefense))
                {
                    body.RemoveBuff(CobaltShield.cobaltDefense);
                }
            }
        }

        private void OnDisable()
        {
            this.body.RemoveBuff(CobaltShield.cobaltDefense);
        }
    }
}
