using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Items
{
    class BigBattery : ItemBase
    {
        public static float shieldPercent = 0.04f;
        public static float rechargeRateIncrease = 0.1f;
        public static float aspdIncrease = 0.1f;

        public override string ItemName => "AAAAAAA Battery";

        public override string ItemLangTokenName => "BORBOBIGBATTERY";

        public override string ItemPickupDesc => "Increase shield recharge rate. While shields are active, gain an attack speed bonus.";

        public override string ItemFullDescription => $"Gain a <style=cIsHealing>shield</style> equal to " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(shieldPercent)}</style> of your maximum health. " +
            $"Increases <style=cIsHealing>shield recharge rate</style> " +
            $"by <style=cIsHealing>{Tools.ConvertDecimal(rechargeRateIncrease)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(rechargeRateIncrease)} per stack)</style>. " +
            $"While shields are active, increase <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsDamage>{Tools.ConvertDecimal(rechargeRateIncrease)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(rechargeRateIncrease)} per stack)</style>.";

        /*public override string ItemLore =>
            "Okay, how’s this one?" +
            "\n\nWe’ve uh," +
            "\n\n*snicker*" +
            "\n\nUh, we’ve reviewed your notes on the last prototype, and, uh" +
            "\n\n(background chatter)" +
            "\n\nWe’ve adjusted the dimensions slightly" +
            "\n\n*snicker*" +
            "\n\nNow, it is slightly longer than your generator thing, but, uh" +
            "\n\n(muffled laughter)" +
            "\n\nWe also drew up this little, uh, connector thing…" +
            "\n\n(Shut the fuck up guys)" +
            "\n\nUh, anyway, you can just, like, stick it right in there, like in the demo video" +
            "\n\n(loud footsteps)" +
            "\n\nAnd honestly I think we’re," +
            "\n\n*snicker*" +
            "\n\nI think we’re really onto something here, man" +
            "\n\nLike this could be the uh" +
            "\n\nThe new standard power supply" +
            "\n\n*snicker*" +
            "\n\nYou know?";*/
        public override string ItemLore => 
@"Okay, how’s this one?

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

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility , ItemTag.Damage };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

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
                    args.baseShieldAdd += sender.maxHealth * shieldPercent;

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
