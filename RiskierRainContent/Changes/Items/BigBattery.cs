using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRainContent.Items
{
    class BigBattery : ItemBase<BigBattery>
    {
        public static float shieldPercentBase = 0.02f;
        public static float shieldPercentStack = 0.02f;
        public static float rechargeRateIncrease = 0.15f;
        public static float aspdIncrease = 0.2f;

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "AAAAAAA Battery";

        public override string ItemLangTokenName => "BORBOBIGBATTERY";

        public override string ItemPickupDesc => "Increase shield recharge rate. While shields are active, gain an attack speed bonus.";

        public override string ItemFullDescription => $"Gain a <style=cIsHealing>shield</style> equal to " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(shieldPercentBase)}</style> of your maximum health " +
            $"<style=cStack>(+{Tools.ConvertDecimal(shieldPercentStack)} per stack)</style>. " +
            $"Increases <style=cIsHealing>shield recharge rate</style> " +
            $"by <style=cIsHealing>{Tools.ConvertDecimal(rechargeRateIncrease)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(rechargeRateIncrease)} per stack)</style>. " +
            $"While shields are active, increase <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsDamage>{Tools.ConvertDecimal(aspdIncrease)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(aspdIncrease)} per stack)</style>.";
        public override string ItemLore =>
@"Order: Prototype Battey
Tracking Number: 11***********
Estimated Delivery: 5/6/2056
Shipping Method:  Priority
Shipping Address: Palia Research Station, Sun
Shipping Details:

Okay, how’s this one?

We’ve uh,

*snicker*

Uh, we’ve reviewed your notes on the last prototype, and, uh

(background chatter)

We’ve adjusted the dimensions slightly

*snicker*

Now, it is slightly longer than your generator thing, but, uh

(muffled laughter)

We also drew up this little, uh, connector thing…

(Shut the fuck up guys)

Uh, anyway, you can just, like, stick it right in there, like in the demo video

(loud footsteps)

And honestly I think we’re,

*snicker*

I think we’re really onto something here, man

Like this could be the uh

The new standard power supply

*snicker*

You know?";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlAAAAAAA.prefab");

        public override Sprite ItemIcon => CoreModules.Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BORBOBIGBATTERY.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += BatteryAspd;
            On.RoR2.CharacterBody.FixedUpdate += BatteryDelayReduction;
            IL.RoR2.HealthComponent.ServerFixedUpdate += BatteryRechargeIncrease;
        }

        private void BatteryRechargeIncrease(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.HealthComponent>(nameof(HealthComponent.body)),
                x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("get_maxShield"),
                x => x.MatchLdcR4(out _)
                );

            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((baseFraction, healthComponent) =>
            {
                float endFraction = baseFraction; // baseFraction is the portion of MAX SHIELDS that gets healed per second
                Inventory inv = healthComponent.body.inventory;

                if(inv != null)
                {
                    endFraction = endFraction;
                }

                return endFraction;
            });
        }

        private void BatteryDelayReduction(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            float outOfDangerStopwatch = self.outOfDangerStopwatch;

            int batteryCount = GetCount(self);
            if (batteryCount > 0)
            {
                float rateIncreaseTotal = Mathf.Min(6f, rechargeRateIncrease * batteryCount);
                self.outOfDangerStopwatch = outOfDangerStopwatch + rateIncreaseTotal * Time.fixedDeltaTime;
            }

            orig(self);
        }

        private void BatteryAspd(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                HealthComponent hc = sender.healthComponent;
                if (hc != null)
                {
                    args.baseShieldAdd += sender.maxHealth * (shieldPercentBase + (shieldPercentStack * (itemCount - 1)));

                    if (Fuse.HasShield(hc))
                    {
                        args.attackSpeedMultAdd += aspdIncrease * itemCount;
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
