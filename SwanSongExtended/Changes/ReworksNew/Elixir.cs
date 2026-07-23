using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Items;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Changes
{
    public class Elixir : ReworkBase<Elixir>
    {
        public override bool isEnabled => false;
        public static ItemDef brokenItemDef;
        public static BuffDef brewActiveBuff;

        [AutoConfig("Barrier Fraction On Use", 0.35f)]
        public static float barrierFraction = 0.35f;
        [AutoConfig("Health Fraction On Use", 0.25f)]
        public static float instantHeal = 0.25f; //0.75f
        [AutoConfig("Move Speed Bonus", 0.14f)]
        public static float moveSpeedBuff = 0.14f;
        [AutoConfig("Attack Speed Bonus", 0.15f)]
        public static float attackSpeedBuff = 0.15f;
        [AutoConfig("Cooldown Reduction Bonus", 0.06f)]
        public static float cooldownReduction = 0.06f;

        public static float buffDurationBase = 0;
        public static float buffDurationStack = 0;
        public static float damageBuff = 0.8f;
        public static float msBuff = 0.45f;
        public static int armorBuff = 60;

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_HealingPotion.HealingPotion_asset;

        public override string ItemName => "Berserker Brew";

        public override string ItemPickupDesc => "At low health, gain barrier, cleanse debuffs, and reset all cooldowns. Usable once per stage.";

        public override string ItemFullDesc =>
            $"Taking damage to below " +
            $"<style=cIsHealth>{Tools.ConvertDecimal(0.25f)} health</style> " +
            $"<style=cIsUtility>consumes</style> this item, " +
            $"instantly granting <style=cIsHealing>{Tools.ConvertDecimal(barrierFraction)}</style> " +
            $"of maximum health in <style=cIsHealing>barrier</style> " +
            $"and <style=cIsUtility>resetting</style> all cooldowns. " +
            $"Each empty bottle increases attack speed by <style=cIsDamage>{Tools.ConvertDecimal(attackSpeedBuff)}</style>, " +
            $"movement speed by <style=cIsDamage>{Tools.ConvertDecimal(moveSpeedBuff)}</style>, " +
            $"and reduces cooldowns by <style=cIsDamage>{Tools.ConvertDecimal(cooldownReduction)}</style>. " +
            $"Regenerates at the start of each stage.";

        public override void Init()
        {
            base.Init();
            SwanSongPlugin.LoadAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_HealingPotion.HealingPotionConsumed_asset, (itemDef) =>
            {
                LanguageAPI.Add(itemDef.nameToken, ItemName + " (Consumed...?)");
                LanguageAPI.Add(itemDef.pickupToken, "You feel lightweight.");

                string fullDesc = $"Increases attack speed by {Tools.ConvertDecimal(attackSpeedBuff)} (+{Tools.ConvertDecimal(attackSpeedBuff)} per stack), " +
                $"movement speed by {Tools.ConvertDecimal(moveSpeedBuff)} (+{Tools.ConvertDecimal(moveSpeedBuff)} per stack), " +
                $"and reduces cooldowns by {Tools.ConvertDecimal(cooldownReduction)} (-{Tools.ConvertDecimal(cooldownReduction)} per stack). " +
                $"Regenerates at the start of each stage.";
                LanguageAPI.Add(itemDef.descriptionToken, fullDesc);
            });
        }

        public override void OnItemLoaded(ItemDef item)
        {
            base.OnItemLoaded(item);

            item.tier = ItemTier.Tier2;
            item.deprecatedTier = ItemTier.Tier2;

            Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Power_Elixir.png");
            if (sprite)
                item.pickupIconSprite = sprite;
        }

        public override void Hooks()
        {
            IL.RoR2.HealthComponent.UpdateLastHitTime += ChangeElixirEffect;

            On.RoR2.CharacterMaster.OnServerStageBegin += TryRegenerateElixir;
            GetStatCoefficients += BerserkerBrewBuff;
        }
        private void ChangeElixirEffect(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ChangeHealingToBuff(c);
        }

        private void ChangeHealingToBuff(ILCursor c)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.HealFraction)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(ChangeElixirEffect), 1);
                return;
            }
            c.Next.Operand = 0;
            c.Emit(OpCodes.Ldarg_0);//self
            c.Emit(OpCodes.Ldarg_1);//self
            c.EmitDelegate<Action<HealthComponent, float>>(TriggerBerserkerBrew);
        }

        public static void TriggerBerserkerBrew(HealthComponent self, float damageInflicted)
        {
            if (!NetworkServer.active || damageInflicted <= 0)
                return;
            CharacterBody body = self.body;
            if (body == null)
                return;

            int count = instance.GetCount(body);
            if (count > 0 && self.isHealthLow)
            {
                float buffDuration = buffDurationBase + buffDurationStack * (count - 1);
                if (buffDuration > 0)
                    body.AddTimedBuff(brewActiveBuff, buffDuration);

                self.AddBarrier(body.maxHealth * barrierFraction);
                body.skillLocator.ApplyAmmoPack();
                CleanseSystem.CleanseBodyServer(
                    characterBody: body, 
                    removeDebuffs: true, 
                    removeBuffs: false, 
                    removeCooldownBuffs: false, 
                    removeDots: true, 
                    removeStun: true, 
                    removeNearbyProjectiles: true
                    );
            }
        }

        private void TryRegenerateElixir(On.RoR2.CharacterMaster.orig_OnServerStageBegin orig, CharacterMaster self, Stage stage)
        {
            orig(self, stage);
            if (NetworkServer.active && self.inventory)
            {
                int count = self.inventory.GetItemCountEffective(brokenItemDef);
                if (count > 0)
                {
                    RegeneratePotions(count, self);
                }
            }
        }
        private void RegeneratePotions(int count, CharacterMaster master)
        {
            Inventory inv = master.inventory;

            new Inventory.ItemTransformation
            {
                originalItemIndex = DLC1Content.Items.HealingPotionConsumed.itemIndex,
                newItemIndex = DLC1Content.Items.HealingPotion.itemIndex,
                maxToTransform = int.MaxValue,
                transformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen
            }.TryTransform(inv, out _);
        }

        private void BerserkerBrewBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(brewActiveBuff))
            {
                args.armorAdd += armorBuff;
                args.moveSpeedMultAdd += msBuff;
                args.damageMultAdd += damageBuff;
            }
            if (sender.inventory)
            {
                int stack = sender.inventory.GetItemCountEffective(brokenItemDef);
                if (stack > 0)
                {
                    args.attackSpeedMultAdd += attackSpeedBuff * stack;
                    args.moveSpeedMultAdd += attackSpeedBuff * stack;
                    args.allSkills.cooldownMultiplier *= Mathf.Pow(1 - cooldownReduction, stack);
                }
            }
        }
    }
}
