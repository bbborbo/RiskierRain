using RoR2;
using SwanSongExtended.Elites;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Components
{
	[RequireComponent(typeof(DelayBlast))]
	public class CannonballController : NetworkBehaviour
	{
		public DelayBlast delayBlast { get; private set; }

		public Vector3 bouncePosition
		{
			get
			{
				return this._bouncePosition;
			}
			set
			{
				this.Network_bouncePosition = value;
			}
		}
		public float initialVelocityY
		{
			get
			{
				return this._initialVelocityY;
			}
			set
			{
				this.Network_initialVelocityY = value;
			}
		}

		private void Awake()
		{
			this.transform = base.transform;
			this.rb = base.GetComponent<Rigidbody>();
			this.delayBlast = base.GetComponent<DelayBlast>();
		}

		private void Start()
		{
			this.startPosition = transform.position;
			this.velocity = new Vector3(0, this.initialVelocityY * SurgingAspect.cannonballGravityCoefficient, 0);
			this.meshVisuals[0].SetActive(false);
			this.meshVisuals[Mathf.Clamp(3 - maxBounces,0,2)].SetActive(true);
		}

		private void FixedUpdate()
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			this.velocity.y = this.velocity.y + fixedDeltaTime * Physics.gravity.y * SurgingAspect.cannonballGravityCoefficient;
			Vector3 vector = transform.position;
			vector += this.velocity * fixedDeltaTime;
			if (vector.y < this.bouncePosition.y + this.radius)
			{
				this.velocity.y = Mathf.Max(this.velocity.y * -this.bounce, this.minimumBounceVelocity) * SurgingAspect.cannonballGravityCoefficient;
				this.velocity.x = 0f;
				this.velocity.z = 0f;
				vector.y = this.bouncePosition.y + this.radius;
				this.OnBounce();
			}
			this.rb.MovePosition(vector);
		}

		private void OnBounce()
		{
			this.meshVisuals[this.bounces].SetActive(false);
			Util.PlaySound(this.bounceSoundStrings[this.bounces], base.gameObject);

			SurgingAspect.FireRingAuthority(bouncePosition, transform.forward, delayBlast.attacker, delayBlast.baseDamage, delayBlast.crit);

			this.bounces++;
			if (this.bounces >= maxBounces)
			{
				this.OnFinalBounce();
				return;
			}
			this.meshVisuals[Mathf.Clamp(3 - maxBounces + bounces, 0, 2)].SetActive(true);
		}

		private void OnFinalBounce()
		{
			if (NetworkServer.active)
			{
				this.delayBlast.position = this.transform.position;
				this.delayBlast.Detonate();
			}
		}

		private void UNetVersion()
		{
		}

		public Vector3 Network_bouncePosition
		{
			get
			{
				return this._bouncePosition;
			}
			[param: In]
			set
			{
				base.SetSyncVar<Vector3>(value, ref this._bouncePosition, 1U);
			}
		}

		public float Network_initialVelocityY
		{
			get
			{
				return this._initialVelocityY;
			}
			[param: In]
			set
			{
				base.SetSyncVar<float>(value, ref this._initialVelocityY, 2U);
			}
		}

		public override bool OnSerialize(NetworkWriter writer, bool forceAll)
		{
			if (forceAll)
			{
				writer.Write(this._bouncePosition);
				writer.Write(this._initialVelocityY);
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
				writer.Write(this._bouncePosition);
			}
			if ((base.syncVarDirtyBits & 2U) != 0U)
			{
				if (!flag)
				{
					writer.WritePackedUInt32(base.syncVarDirtyBits);
					flag = true;
				}
				writer.Write(this._initialVelocityY);
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
				this._bouncePosition = reader.ReadVector3();
				this._initialVelocityY = reader.ReadSingle();
				return;
			}
			int num = (int)reader.ReadPackedUInt32();
			if ((num & 1) != 0)
			{
				this._bouncePosition = reader.ReadVector3();
			}
			if ((num & 2) != 0)
			{
				this._initialVelocityY = reader.ReadSingle();
			}
		}

		public override void PreStartClient()
		{
		}

		public float radius;

		public float bounce = 0.8f;

		public float minimumBounceVelocity;

		public GameObject[] meshVisuals;

		public string[] bounceSoundStrings;

		private new Transform transform;

		internal Rigidbody rb;

		[SyncVar]
		private Vector3 _bouncePosition;

		[SyncVar]
		private float _initialVelocityY;

		public int maxBounces = 1;

		internal Vector3 startPosition;

		private Vector3 velocity;

		private int bounces;
	}
}
