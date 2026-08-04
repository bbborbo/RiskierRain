using HG;
using RoR2;
using SwanSongExtended.Elites;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Components
{
    [RequireComponent(typeof(NetworkedBodyAttachment))]
    public class HowlingRallyController : NetworkBehaviour
	{
		[SyncVar]
		public float radius = StormsCore.cycloneRadius + 10f;

		public float chargePerSecondPerTarget = 1;

		[Min(1E-45f)]
		public float ticksPerSecond = 2f;

		[SyncVar]
		public int maxTargets = 12;

		public TetherVfxOrigin tetherVfxOrigin;

		public GameObject activeVfx;

		protected new Transform transform;

		protected NetworkedBodyAttachment networkedBodyAttachment;
		protected CharacterBody attachedBody;

		protected SphereSearch sphereSearch;

		protected float timer;

		private bool isTetheredToAtLeastOneObject;

		public float Networkradius
		{
			get
			{
				return this.radius;
			}
			[param: In]
			set
			{
				base.SetSyncVar<float>(value, ref this.radius, 1U);
			}
		}

		public int NetworkmaxTargets
		{
			get
			{
				return this.maxTargets;
			}
			[param: In]
			set
			{
				base.SetSyncVar<int>(value, ref this.maxTargets, 2U);
			}
		}

		protected void Awake()
		{
			this.transform = base.transform;
			this.networkedBodyAttachment = base.GetComponent<NetworkedBodyAttachment>();
			this.sphereSearch = new SphereSearch();
			this.timer = 0f;
		}

		void Start()
		{
			this.attachedBody = networkedBodyAttachment.attachedBody;
		}

		protected void FixedUpdate()
		{
			if (!this.networkedBodyAttachment || !this.attachedBody || !this.networkedBodyAttachment.attachedBodyObject)
			{
				return;
			}

			if (attachedBody.HasBuff(StormsCore.CycloneLeader) == false)
				if (attachedBody.teamComponent.teamIndex != TeamIndex.Player)
				{
					SetTetheredTransforms(new List<Transform>());
					return;
				}

			this.timer -= Time.fixedDeltaTime;
			if (this.timer <= 0f)
			{
				this.timer += 1f / this.ticksPerSecond;
				this.Tick();
			}
		}

		protected void Tick()
		{
			float amount = this.chargePerSecondPerTarget / this.ticksPerSecond;
			ApplyTime(amount);

			List<Transform> tetheredTransforms = CollectionPool<Transform, List<Transform>>.RentCollection();
			List<HurtBox> potentialTargets = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
			this.SearchForTargets(potentialTargets);

			int targetsChecked = 0;
			while (targetsChecked < potentialTargets.Count && tetheredTransforms.Count < this.maxTargets)
			{
				HurtBox targetHurtBox = potentialTargets[targetsChecked];
				targetsChecked++;

				bool targetInvalid =
					this.attachedBody.healthComponent.alive == false
					|| GetAllyFilter(targetHurtBox) == false;
				if (targetInvalid)
					continue;

				HealthComponent healthComponent = targetHurtBox.healthComponent;
				if (targetHurtBox.healthComponent.body == this.attachedBody)
					continue;

				CharacterBody body = healthComponent.body;
				Transform item = ((body != null) ? body.coreTransform : null) ?? targetHurtBox.transform;
				tetheredTransforms.Add(item);
				if (NetworkServer.active)
				{
					ApplyCharge(amount);
				}
			}

			ReportContributorCount(tetheredTransforms.Count);
			SetTetheredTransforms(tetheredTransforms);

			CollectionPool<Transform, List<Transform>>.ReturnCollection(tetheredTransforms);
			CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(potentialTargets);
		}

		public bool GetAllyFilter(HurtBox alliedHurtbox)
        {
			return alliedHurtbox != null && (alliedHurtbox.teamIndex == TeamIndex.Player || alliedHurtbox.healthComponent.body.HasBuff(WhirlwindAspect.instance.EliteBuffDef));
        }

		private void SetTetheredTransforms(List<Transform> tetheredTransforms)
		{
			this.isTetheredToAtLeastOneObject = tetheredTransforms != null ? ((float)tetheredTransforms.Count > 0f) : false;
			if (this.tetherVfxOrigin)
			{
				this.tetherVfxOrigin.SetTetheredTransforms(tetheredTransforms);
			}
			if (this.activeVfx)
			{
				this.activeVfx.SetActive(this.isTetheredToAtLeastOneObject);
			}
		}

		private void ReportContributorCount(int count)
		{
			if (attachedBody.teamComponent.teamIndex == TeamIndex.Player)
			{
				//there should be something here :P
				return;
			}
			CycloneController.SetContributorCount(count);
		}
        private void ApplyTime(float delta)
        {
			if(attachedBody.teamComponent.teamIndex == TeamIndex.Player)
            {
				//there should be something here :P
				return;
            }
			CycloneController.AddSquallTime(delta);
            //healthComponent.Heal(amount, default(ProcChainMask), true);
        }
        private void ApplyCharge(float delta)
		{
			if (attachedBody.teamComponent.teamIndex == TeamIndex.Player)
			{
				//there should be something here :P
				return;
			}
			CycloneController.AddSquallCharge(delta);
			//healthComponent.Heal(amount, default(ProcChainMask), true);
		}

        protected void SearchForTargets(List<HurtBox> dest)
		{
			TeamMask none = TeamMask.none;
			none.AddTeam(this.attachedBody.teamComponent.teamIndex);
			this.sphereSearch.mask = LayerIndex.entityPrecise.mask;
			this.sphereSearch.origin = this.transform.position;
			this.sphereSearch.radius = this.radius + this.networkedBodyAttachment.attachedBody.radius;
			this.sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
			this.sphereSearch.RefreshCandidates();
			this.sphereSearch.FilterCandidatesByHurtBoxTeam(none);
			this.sphereSearch.OrderCandidatesByDistance();
			this.sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
			this.sphereSearch.GetHurtBoxes(dest);
			this.sphereSearch.ClearCandidates();
		}

		public override bool OnSerialize(NetworkWriter writer, bool forceAll)
		{
			if (forceAll)
			{
				writer.Write(this.radius);
				writer.WritePackedUInt32((uint)this.maxTargets);
				return true;
			}
			bool flag = false;
			if ((base.syncVarDirtyBits & 1U) != 0U)
			{
				if (!flag)
				{
					writer.WritePackedUInt32(base.syncVarDirtyBits);
					flag = true;
				}
				writer.Write(this.radius);
			}
			if ((base.syncVarDirtyBits & 2U) != 0U)
			{
				if (!flag)
				{
					writer.WritePackedUInt32(base.syncVarDirtyBits);
					flag = true;
				}
				writer.WritePackedUInt32((uint)this.maxTargets);
			}
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
			}
			return flag;
		}

		public override void OnDeserialize(NetworkReader reader, bool initialState)
		{
			if (initialState)
			{
				this.radius = reader.ReadSingle();
				this.maxTargets = (int)reader.ReadPackedUInt32();
				return;
			}
			int num = (int)reader.ReadPackedUInt32();
			if ((num & 1) != 0)
			{
				this.radius = reader.ReadSingle();
			}
			if ((num & 2) != 0)
			{
				this.maxTargets = (int)reader.ReadPackedUInt32();
			}
		}

		public override void PreStartClient()
		{
		}
	}
}
