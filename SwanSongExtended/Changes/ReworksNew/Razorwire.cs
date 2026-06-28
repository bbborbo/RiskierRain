using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Modules;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using RoR2.Items;
using UnityEngine.AddressableAssets;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class Razorwire : ReworkBase<Razorwire>
    {
		public static BuffDef razorChargeBuff;
        static float razorwireArmorBase = 0;
        static float razorwireArmorStack = 0;
        private static float razorwireRangeBase = 40; //25
        private static float razorwireRangeStack = 0; //10
        private static float razorwireTargetsBase = 5; //5
        private static float razorwireTargetsStack = 2; //2
        private static float razorwireBleedDuration = 5; //3

        private static float razorwireDamage = 3.6f;
        private static float razorwireProcCoeff = 0.2f;
        private static float razorwireCooldown = 1f;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Thorns.Thorns_asset;

        public override string ItemName => "Razorwire";

        public override string ItemPickupDesc =>
            "Retaliate in a burst of bleeding razors on taking damage. Recharges over time.";

        public override string ItemFullDesc =>
            $"Getting hit causes you to explode in a burst of razors, " +
            $"<style=cIsDamage>bleeding</style> up to <style=cIsDamage>{razorwireTargetsBase}</style> " +
            $"<style=cStack>(+{razorwireTargetsStack} per stack)</style> nearby enemies " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(razorwireBleedDuration * 0.8f)}</style> base damage " +
            $"per <style=cIsDamage>razor charge</style> expelled. " +
            $"You can hold up to {RazorwireBehavior.baseRazors} <style=cStack>(+{RazorwireBehavior.stackRazors} per stack)</style> " +
            $"razor charges, all reloading over <style=cIsUtility>{RazorwireBehavior.rechargeTime}</style> seconds.";

        public override void Init()
		{
			razorChargeBuff = Content.CreateAndAddBuff(
				"bdRazorChargeBuff",
				Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(), //replace me
				Color.white,
				true,
				false);
			razorChargeBuff.isCooldown = true;
			base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += RazorwireArmor;

			IL.RoR2.HealthComponent.TakeDamageProcess += RazorwireBegin;
			IL.RoR2.Orbs.LightningOrb.OnArrival += RazorwireArrival;
        }

		private void RazorwireArmor(CharacterBody sender, StatHookEventArgs args)
		{
			if (!sender.inventory)
				return;
			int itemCount = sender.inventory.GetItemCountEffective(RoR2Content.Items.Thorns);
			if (itemCount > 0)
			{
				args.armorAdd += razorwireArmorBase + razorwireArmorStack * (itemCount - 1);
			}
		}

		private void RazorwireArrival(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			int healthComponentLoc = 0;
			c.GotoNext(MoveType.After,
				x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
				x => x.MatchStloc(out healthComponentLoc));

			c.GotoNext(MoveType.After,
				x => x.MatchLdloc(healthComponentLoc));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<HealthComponent, LightningOrb, HealthComponent>>((hc, orb) =>
			{
				if (orb.lightningType == LightningOrb.LightningType.RazorWire && hc != null)
				{
					for (int i = 0; i < orb.procCoefficient; i++)
					{
						DotController.InflictDot(hc.gameObject, orb.attacker, hc.body.mainHurtBox, DotController.DotIndex.Bleed, razorwireBleedDuration, orb.damageValue);
					}
					return null;
				}
				return hc;
			});
		}

		private void RazorwireBegin(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			int buffCount = 0;

			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent.ItemCounts>("thorns")
				);
			c.GotoPrev(MoveType.Before,
				x => x.MatchLdcI4(out _),
				x => x.MatchLdcI4(out _)
				);
			c.Next.Operand = razorwireTargetsBase;
			c.Index++;
			c.Next.Operand = razorwireTargetsStack;


			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent.ItemCounts>("thorns"),
				x => x.MatchLdcI4(0)
				);
			c.Index--;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Int32, HealthComponent, Int32>>((itemCount, hc) =>
			{
				if (!NetworkServer.active)
					return 0;
				CharacterBody body = hc.body;
				buffCount = 0;
				while (body.HasBuff(Razorwire.razorChargeBuff))
				{
					body.RemoveBuff(Razorwire.razorChargeBuff);
					buffCount++;
				}
				if (buffCount <= 0)
				{
					return 0;
				}
				return itemCount;
			});

			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent.ItemCounts>("thorns")
				);
			c.GotoPrev(MoveType.Before,
				x => x.MatchLdcI4(out _),
				x => x.MatchLdcI4(out _)
				);
			c.Next.Operand = razorwireRangeBase;
			c.Index++;
			c.Next.Operand = razorwireRangeStack;

			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt<OrbManager>(nameof(OrbManager.AddOrb)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<LightningOrb, HealthComponent, LightningOrb>>((razorwireOrb, hc) =>
			{
				CharacterBody body = hc.body;
				razorwireOrb.procCoefficient = buffCount;
				razorwireOrb.damageValue = body.teamComponent.teamIndex == TeamIndex.Player ? 1 : 0.2f;
				return razorwireOrb;
			});
		}
		private void NerfRazorwireOrb(On.RoR2.Orbs.LightningOrb.orig_Begin orig, LightningOrb self)
		{
			if (self.lightningType == LightningOrb.LightningType.RazorWire)
			{
				self.procCoefficient = razorwireProcCoeff;
				self.damageType.damageType = DamageType.BleedOnHit;
			}

			orig(self);
		}
	}
	public class RazorwireBehavior : BaseItemBodyBehavior
	{
		[ItemDefAssociation(useOnServer = true, useOnClient = false)]
		private static ItemDef GetItemDef() => RoR2Content.Items.Thorns;
		public static float rechargeTime = 5;
		public static int baseRazors = 2;
		public static int stackRazors = 1;

		private float reloadTimer;
		BuffDef razorBuff => Razorwire.razorChargeBuff;

		void OnDisable()
		{
			if (body != null)
			{
				body.SetBuffCount(razorBuff.buffIndex, 0);
			}
		}

		void FixedUpdate()
		{
			int totalRazors = baseRazors + (this.stack - 1) * stackRazors;

			int buffCount = body.GetBuffCount(razorBuff);
			if (buffCount < totalRazors)
			{
				float rechargeInterval = rechargeTime / totalRazors;
				reloadTimer += Time.fixedDeltaTime;
				while (this.reloadTimer > rechargeInterval && buffCount < totalRazors && NetworkServer.active)
				{
					buffCount++;
					body.AddBuff(razorBuff);
					reloadTimer -= rechargeInterval;
				}
			}
		}
    }
}
