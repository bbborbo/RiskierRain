using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class MassAnomaly : ItemBase<MassAnomaly>
    {
        public static int baseArmorCount = 3;
        public static int armorPerPercent = 10;
        public static int armorDecayPerSecond = armorPerPercent * 4;
        public static int armorCap = armorPerPercent * 40;

        public override string ItemName => "Relic of Mass";

        public override string ItemLangTokenName => "MASSANOMALY";

        public override string ItemPickupDesc => "Reduce damage taken from successive hits."; //Temporarily reduce damage taken after getting hit?

        public override string ItemFullDescription => $"When taking damage, " +
            $"gain <style=cIsHealing>{armorPerPercent} temporary armor</style> " +
            $"<style=cStack>(+{armorPerPercent} per stack)</style> " +
            $"per <style=cIsHealth>1%</style> of health lost. " +
            $"<style=cIsUtility>This temporary armor caps at {armorCap} " +
            $"and decays {armorDecayPerSecond} per second.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Boss;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += NerfAdaptiveArmor;
            IL.RoR2.HealthComponent.ServerFixedUpdate += AdaptiveArmorDecay;
            On.RoR2.HealthComponent.ServerFixedUpdate += Fuck;
            On.RoR2.HealthComponent.OnInventoryChanged += AdaptiveArmorHook;
        }

        private void Fuck(On.RoR2.HealthComponent.orig_ServerFixedUpdate orig, HealthComponent self, float deltaTime)
        {
            orig(self, deltaTime);
            if(self.itemCounts.adaptiveArmor > 0)
            {
                //Debug.Log(self.adaptiveArmorValue);
            }
        }

        private void AdaptiveArmorDecay(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld<HealthComponent>("adaptiveArmorValue"),
                x => x.MatchLdcR4(out _)
                //, x => x.MatchCallOrCallvirt<Time>(nameof(Time.fixedDeltaTime))
                );
            c.Index++;
            c.Next.Operand = (float)armorDecayPerSecond;
        }

        private void NerfAdaptiveArmor(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdflda<HealthComponent>("itemCounts"),
                x => x.MatchLdfld<HealthComponent.ItemCounts>("adaptiveArmor"),
                x => x.MatchLdcI4(out _)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchDiv(),
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.Next.Operand = (float)armorPerPercent;
            
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<UnityEngine.Mathf>("Min")
                );
            c.Index--;
            c.Next.Operand = (float)armorCap;
        }

        private void AdaptiveArmorHook(On.RoR2.HealthComponent.orig_OnInventoryChanged orig, RoR2.HealthComponent self)
        {
            orig(self);
            if (self.body)
            {
                self.itemCounts.adaptiveArmor = GetCount(self.body) +
                    (self.body.inventory.GetItemCount(RoR2.RoR2Content.Items.AdaptiveArmor) * baseArmorCount);
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            //CreateBuff();
            Hooks();
        }
    }
}
