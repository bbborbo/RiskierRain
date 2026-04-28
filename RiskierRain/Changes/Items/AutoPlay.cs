using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using RoR2.Projectile;

namespace RiskierRain.Changes
{
    public static partial class ItemChanges
	{
		public static void DevilOrbSlowAndForce(ILContext il, DevilOrb.EffectType effectType, float slowDuration, float forceMultiplier)
		{
			ILCursor c = new ILCursor(il);

			bool b = c.TryGotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.DamageInfo>("force")
				);
			if (!b)
			{
				DebugBreakpoint(nameof(DevilOrbSlowAndForce) + $"/{effectType}");
				return;
			}
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Vector3, DevilOrb, Vector3>>((forceIn, orb) =>
			{
				if (orb.effectType != effectType)
					return forceIn;

				HealthComponent healthComponent = orb.target.healthComponent;
				if (slowDuration > 0)
					healthComponent.body.AddTimedBuffAuthority(RoR2Content.Buffs.Slow50.buffIndex, slowDuration);

				if (forceMultiplier <= 0)
					return forceIn;

				float baseForce = 0;
				if (healthComponent.body.characterMotor != null)
				{
					baseForce = healthComponent.body.characterMotor.mass;
				}
				else if (healthComponent.body.rigidbody != null)
				{
					baseForce = healthComponent.body.rigidbody.mass;
				}

				Vector3 forceOut = (orb.target.transform.position - orb.attacker.transform.position).normalized * (100 + (baseForce * forceMultiplier));

				return forceOut;
			});
		}
		public static void DevilOrbProcCoefficient(On.RoR2.Orbs.DevilOrb.orig_Begin orig, DevilOrb self, DevilOrb.EffectType effectType, float procCoefficient)
		{
			if (self.effectType == effectType)
				self.procCoefficient = procCoefficient;

			orig(self);
		}

		#region nkuhana
		public static float opinionDamageMultiplier = 3.5f; //2.5f
		public static float opinionForceMultiplier = 25f; //0f
		public static float opinionSlowDuration = 3f; //0f
		public static float opinionProcCoeff = 0.75f; //0.2f
		public static void ChangeNkuhana()
		{
			IL.RoR2.Orbs.DevilOrb.OnArrival += (il) => DevilOrbSlowAndForce(il, DevilOrb.EffectType.Skull, opinionSlowDuration, opinionForceMultiplier);
			On.RoR2.Orbs.DevilOrb.Begin += (orig, self) => DevilOrbProcCoefficient(orig, self, DevilOrb.EffectType.Skull, opinionProcCoeff);

			IL.RoR2.HealthComponent.ServerFixedUpdate += NkuhanasBuff;

			LanguageAPI.Add("ITEM_NOVAONHEAL_DESC",
				$"Store <style=cIsHealing>100%</style> <style=cStack>(+100% per stack)</style> of healing as <style=cIsHealing>Soul Energy</style>. " +
				$"After your <style=cIsHealing>Soul Energy</style> reaches <style=cIsHealing>10%</style> of your <style=cIsHealing>maximum health</style>, " +
				$"<style=cIsDamage>fire a skull</style> that deals <style=cIsDamage>{Tools.ConvertDecimal(opinionDamageMultiplier)}</style> " +
				$"of your <style=cIsHealing>Soul Energy</style> as <style=cIsDamage>damage</style>.");
		}

		private static void NkuhanasBuff(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<DevilOrb>(nameof(DevilOrb.damageValue))
				);

			c.Index -= 2;
			c.Remove();
			c.Emit(OpCodes.Ldc_R4, opinionDamageMultiplier);
		}
		#endregion

		#region little disciple
		public static float discipleForceMultiplier = 15f; //0f
		public static float discipleSlowDuration = 3f; //0f
		public static float discipleProcCoeff = 0.4f; //1.0f
		public static void ChangeDisciple()
		{
			IL.RoR2.Orbs.DevilOrb.OnArrival += (il) => DevilOrbSlowAndForce(il, DevilOrb.EffectType.Wisp, discipleSlowDuration, discipleForceMultiplier);
			On.RoR2.Orbs.DevilOrb.Begin += (orig, self) => DevilOrbProcCoefficient(orig, self, DevilOrb.EffectType.Skull, opinionProcCoeff);

			On.RoR2.Items.SprintWispBodyBehavior.FixedUpdate += FixDiscipleBaseMoveSpeed;
			IL.RoR2.Items.SprintWispBodyBehavior.Fire += NerfDiscipleDamage;

			LanguageAPI.Add("ITEM_SPRINTWISP_DESC",
				$"Fire a <style=cIsDamage>tracking wisp</style> " +
				$"for <style=cIsDamage>300% damage</style> " +
				$"that <style=cIsUtility>pushes and slows</style> enemies for 3 seconds. " +
				$"Fires every <style=cIsUtility>1</style><style=cStack>(-50% per stack)</style> seconds " +
				$"while sprinting. Fire rate increases with <style=cIsUtility>movement speed</style>.");
		}
		private static void NerfDiscipleDamage(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.Orbs.DevilOrb>("damageValue")
				);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, SprintWispBodyBehavior, float>>((damageIn, behavior) => {
				float damageOut = damageIn / behavior.stack;
				return damageOut;
			});
		}

		private static void FixDiscipleBaseMoveSpeed(On.RoR2.Items.SprintWispBodyBehavior.orig_FixedUpdate orig, SprintWispBodyBehavior self)
		{
			CharacterBody body = self.body;
			if (body.isSprinting)
			{
				self.fireTimer -= Time.fixedDeltaTime;
				if (self.fireTimer <= 0f)
				{
					self.fireTimer += (body.baseMoveSpeed * 1.45f) / (body.moveSpeed * SprintWispBodyBehavior.fireRate);
					self.Fire();
				}
			}
		}
		#endregion

		#region willowisp
		private static float willowispProcCoeff = 0.5f; //1.0f
		private static float willowispDamageBase = 4.8f; //3.5f
		private static float willowispDamageStack = 2.8f; //2.8f
		private static float willowispBaseRange = 16f; //12f
		private static float willowispStackRange = 0f; //2.4f
		public static void ChangeWillowisp()
		{
			LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ExplodeOnDeath.WilloWispDelay_prefab, (delayBlast) =>
			{
				if (delayBlast.TryGetComponent<DelayBlast>(out DelayBlast blast))
					blast.procCoefficient = willowispProcCoeff;
			});
			IL.RoR2.GlobalEventManager.OnCharacterDeath += WillOWispChanges;
			LanguageAPI.Add("ITEM_EXPLODEONDEATH_DESC",
				$"On killing an enemy, spawn a <style=cIsDamage>lava pillar</style> in a <style=cIsDamage>{willowispBaseRange}m</style> radius for " +
				$"<style=cIsDamage>{Tools.ConvertDecimal(willowispDamageBase)}</style> <style=cStack>(+{Tools.ConvertDecimal(willowispDamageStack)} per stack)</style> base damage.");
		}

		private static void WillOWispChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			int countLoc = -1;
			bool b1 = 
			c.TryGotoNext(MoveType.After,
				x => x.MatchLdsfld("RoR2.RoR2Content/Items", "ExplodeOnDeath")
				)
			&& c.TryGotoNext(MoveType.After,
				x => x.MatchStloc(out countLoc)
				)
			&& c.TryGotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnKillProcDamage))
				);
            if (!b1)
            {
				DebugBreakpoint(nameof(WillOWispChanges), 1);
				return;
            }

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentDamage, itemCount) =>
			{
				float newDamage = willowispDamageBase + (willowispDamageStack * (itemCount - 1));

				return newDamage;
			});

			bool b2 =
			c.TryGotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.DelayBlast>(nameof(RoR2.DelayBlast.radius))
				);
			if (!b2)
			{
				DebugBreakpoint(nameof(WillOWispChanges), 2);
				return;
			}

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentRadius, itemCount) =>
			{
				float newRadius = willowispBaseRange + willowispStackRange * itemCount;

				return newRadius;
			});
		}
		#endregion

		#region shatterspleen
		private static float spleenProcCoeff = 0f; //1.0f
		public static void ChangeShatterspleen()
        {
			LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BleedOnHitAndExplode.BleedOnHitAndExplodeDelay_prefab, (delayBlast) =>
			{
				if (delayBlast.TryGetComponent<DelayBlast>(out DelayBlast blast))
					blast.procCoefficient = spleenProcCoeff;
			});
		}
		#endregion

		#region fireworks
		private static float fireworkProcCoeff = 0.2f;//0.33f
		public static void ChangeFireworks()
		{
			LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Firework.FireworkProjectile_prefab, (projectile) =>
			{
				if (projectile.TryGetComponent(out ProjectileController pc))
					pc.procCoefficient = fireworkProcCoeff;
			});
		}
		#endregion

		#region ceremonial dagger cagger
		public static float ceremonialDaggerLifetime = 4f;//10f
		public static void ChangeCeremonialDagger()
		{
			LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Dagger.DaggerProjectile_prefab, (projectile) =>
			{
				if (projectile.TryGetComponent(out ProjectileSimple ps))
					ps.lifetime = ceremonialDaggerLifetime;
			});
		}
		#endregion

		#region voidsent flame
		private static float voidsentProcCoeff = 1f; //1.0f
		private static float voidsentDamageBase = 6f; //3.5f
		private static float voidsentDamageStack = 4f; //2.8f
		private static float voidsentBaseRange = 24f; //12m
		private static float voidsentStackRange = 0f; //2.4m
		private static float voidsentBaseChance = 33f; //100f
		private static float voidsentStackChance = 0f;
		private static void ChangeVoidsent()
		{
			LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_ExplodeOnDeathVoid.ExplodeOnDeathVoidExplosion_prefab, (delayBlast) =>
			{
				if (delayBlast.TryGetComponent<DelayBlast>(out DelayBlast blast))
				{
					blast.procCoefficient = voidsentProcCoeff;
					blast.damageType.damageType = DamageType.Stun1s;
				}
			});
			IL.RoR2.HealthComponent.TakeDamageProcess += VoidsentFlameChanges;
			LanguageAPI.Add("ITEM_EXPLODEONDEATHVOID_PICKUP",
				$"Chance to detonate full health enemies on hit. <style=cIsVoid>Corrupts all Will-o'-the-wisps</style>.");
			LanguageAPI.Add("ITEM_EXPLODEONDEATHVOID_DESC",
				$"Upon hitting an enemy at or above <style=cIsDamage>100% health</style>, " +
				$"has a {voidsentBaseChance}% chance to " +
				$"<style=cIsDamage>detonate</style> them in a <style=cIsDamage>{voidsentBaseRange}m</style> radius " +
				$"for <style=cIsDamage>{Tools.ConvertDecimal(voidsentDamageBase)}</style> " +
				$"<style=cStack>(+{Tools.ConvertDecimal(voidsentDamageStack)} per stack)</style> base damage. " +
				$"<style=cIsVoid>Corrupts all Will-o'-the-wisps</style>.");
		}
		private static void VoidsentFlameChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			int countLoc = -1;
			c.GotoNext(MoveType.Before,
				x => x.MatchLdsfld("RoR2.DLC1Content/Items", "ExplodeOnDeathVoid")
				);
			c.GotoNext(MoveType.Before,
				x => x.MatchStloc(out countLoc)
				);
			c.EmitDelegate<Func<int, int>>((itemCountIn) =>
			{
				if (itemCountIn > 0 && Util.CheckRoll(voidsentBaseChance + voidsentStackChance * (itemCountIn - 1)))
						return itemCountIn;
				return 0;
			});

			//return;
			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnKillProcDamage))
				);

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentDamage, itemCount) =>
			{
				float newDamage = voidsentBaseRange + voidsentDamageStack * (itemCount - 1);

				return newDamage;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.DelayBlast>(nameof(RoR2.DelayBlast.radius))
				);

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentRadius, itemCount) =>
			{
				float newRadius = voidsentBaseRange + voidsentStackRange * (itemCount - 1);

				return newRadius;
			});
		}
		#endregion

		#region gasoline
		private static float gasBaseDamage = 0.5f; //1.5f
		private static float gasStackDamage = 0; //0f
		private static float gasBaseBurnDamage = 1f; //1.5f
		private static float gasStackBurnDamage = 1f; //0.75f

		public static void ChangeGasoline()
		{
			IL.RoR2.GlobalEventManager.ProcIgniteOnKill += GasChanges;
			LanguageAPI.Add("ITEM_IGNITEONKILL_DESC",
				$"Killing an enemy <style=cIsDamage>ignites</style> all enemies within " +
				$"<style=cIsDamage>12m</style> <style=cStack>(+4m per stack)</style> " +
				$"for <style=cIsDamage>{Tools.ConvertDecimal(gasBaseDamage)}</style> base damage. " +
				$"Additionally, enemies <style=cIsDamage>burn</style> " +
				$"for <style=cIsDamage>{100 * (gasBaseBurnDamage)}%</style> " +
				$"<style=cStack>(+{100 * (gasStackBurnDamage)}% per stack)</style> base damage.");
		}

		private static void GasChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			bool b = c.TryGotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.InflictDotInfo>(nameof(RoR2.InflictDotInfo.totalDamage))
				)
			&& c.TryGotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.BlastAttack>(nameof(RoR2.BlastAttack.baseDamage))
				);
            if (!b)
            {
				DebugBreakpoint(nameof(GasChanges));
				return;
            }
			c.Index = 0;
			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.InflictDotInfo>(nameof(RoR2.InflictDotInfo.totalDamage))
				);
			c.Index--;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, DamageReport, int, float>>((currentDamage, damageReport, itemCount) =>
			{
				float newBurnDamage = currentDamage;

				newBurnDamage = (gasBaseBurnDamage + gasStackBurnDamage * (itemCount - 1)) * damageReport.attackerBody.damage;

				return newBurnDamage;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.BlastAttack>(nameof(RoR2.BlastAttack.baseDamage))
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, DamageReport, int, float>>((currentDamage, damageReport, itemCount) =>
			{
				float newHitDamage = currentDamage;

				newHitDamage = (gasBaseDamage + gasStackDamage * itemCount) * damageReport.attackerBody.damage;

				return newHitDamage;
			});
		}

		private static void GasChangesOld(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.After,
				x => x.MatchLdcR4(1.5f),
				x => x.MatchLdcR4(1.5f),
				x => x.MatchLdarg(1),
				x => x.MatchConvR4(),
				x => x.MatchMul(),
				x => x.MatchAdd()
				);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, int, float>>((currentDuration, itemCount) =>
			{
				float newDuration = currentDuration;

				newDuration = gasBaseBurnDamage + gasStackBurnDamage * (itemCount - 1);

				return newDuration;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.BlastAttack>(nameof(RoR2.BlastAttack.baseDamage))
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, DamageReport, int, float>>((currentDamage, damageReport, itemCount) =>
			{
				float newDamage = currentDamage;

				newDamage = (gasBaseDamage + gasStackDamage * itemCount) * damageReport.attackerBody.damage;

				return newDamage;
			});
		}
		#endregion
	}
}
