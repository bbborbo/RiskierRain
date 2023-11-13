using EntityStates;
using RiskierRainContent.Equipment;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Enemies.VoidDreamers
{
	class DreamersSimulateState : BaseState
	{

		public override  void OnEnter()
        {
			base.OnEnter();
			SearchEnemies();
			SummonSimulatedElites();
			outer.SetNextStateToMain();
		}
		private void SummonSimulatedElites()
		{
			Vector3 searchOrigin = base.GetAimRay().origin;
			RaycastHit raycastHit;
			if (base.inputBank && base.inputBank.GetAimRaycast(float.PositiveInfinity, out raycastHit))
			{
				searchOrigin = raycastHit.point;
			}
			if (this.enemySearch != null)
			{
				this.enemySearch.searchOrigin = searchOrigin;
				this.enemySearch.RefreshCandidates();
				HurtBox hurtBox = this.enemySearch.GetResults().FirstOrDefault<HurtBox>();
				Transform transform = (hurtBox && hurtBox.healthComponent) ? hurtBox.healthComponent.body.coreTransform : base.characterBody.coreTransform;
				if (transform)
				{
					DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(ChooseRandomMonster(), new DirectorPlacementRule
					{
						placementMode = DirectorPlacementRule.PlacementMode.Approximate,
						minDistance = 3f,
						maxDistance = 20f,
						spawnOnTarget = transform
					}, RoR2Application.rng);
					directorSpawnRequest.summonerBodyObject = base.gameObject;
					DirectorSpawnRequest directorSpawnRequest2 = directorSpawnRequest;
					directorSpawnRequest2.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest2.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult spawnResult)
					{
						if (spawnResult.success && spawnResult.spawnedInstance && base.characterBody)
						{
							Inventory component = spawnResult.spawnedInstance.GetComponent<Inventory>();
							if (component)
							{
								component.SetEquipmentIndex(SimulatedAspect.instance.EliteEquipmentDef.equipmentIndex);
								component.GiveItem(RoR2Content.Items.UseAmbientLevel);
								component.GiveItem(RoR2Content.Items.HealthDecay, 30);
							}
						}
					}));
					DirectorCore instance = DirectorCore.instance;
					if (instance == null)
					{
						return;
					}
					GameObject guy = instance.TrySpawnObject(directorSpawnRequest);
					//BanishToVoidDimension(guy);
				}
			}
		}
		private void BanishToVoidDimension(GameObject victim)
        {
			HealthComponent hc = victim.GetComponent<HealthComponent>();
			if (hc == null)
            {
				Debug.Log("whoops!!");
				return;
            }
			hc.TakeDamage(new DamageInfo
			{
				damage = 1,
				crit = false,
				inflictor = base.characterBody.gameObject,
				attacker = base.characterBody.gameObject,
				//procChainMask = ProcChainMask.
				procCoefficient = 1,
				damageType = DamageType.VoidDeath
			});
        }
		private void SearchEnemies()
        {
			if (NetworkServer.active)
			{
				this.enemySearch = new BullseyeSearch();
				this.enemySearch.filterByDistinctEntity = false;
				this.enemySearch.filterByLoS = false;
				this.enemySearch.maxDistanceFilter = float.PositiveInfinity;
				this.enemySearch.minDistanceFilter = 0f;
				this.enemySearch.minAngleFilter = 0f;
				this.enemySearch.maxAngleFilter = 180f;
				this.enemySearch.teamMaskFilter = TeamMask.GetEnemyTeams(base.GetTeam());
				this.enemySearch.sortMode = BullseyeSearch.SortMode.Distance;
				this.enemySearch.viewer = base.characterBody;
			}
		}
		private SpawnCard ChooseRandomMonster()
        {
			WeightedSelection<DirectorCard> cardPool = ClassicStageInfo.instance.monsterSelection;
			rng = new Xoroshiro128Plus(Run.instance.spawnRng.nextUlong);
			DirectorCard card = cardPool.Evaluate(rng.nextNormalizedFloat);
			return card.spawnCard;
        }

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Pain;
		}

		BullseyeSearch enemySearch;
		private Xoroshiro128Plus rng;

	}
}
