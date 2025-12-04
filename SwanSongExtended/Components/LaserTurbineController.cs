using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Components
{
    public class LaserTurbineController : NetworkBehaviour
	{
		public static float spinGeneratedOnKill = 0.025f; //0.025f
		public static float spinDecayPerSecondAfterRefresh = 0.0125f; //0.0125f
		public static float minSpin = 0.025f; //idk
		public static float maxSpin = 0.2f; //idk
		public static float visualSpinRate = 7200f;

		public float charge { get; private set; }
		public float spin { get; private set; }
		public Transform chargeIndicator;
		public Transform spinIndicator;
		public Transform turbineDisplayRoot;
		public bool showTurbineDisplay = true;
		public string spinRtpc;
		public float spinRtpcScale;
		private GenericOwnership genericOwnership;
		[SyncVar]
		private LaserTurbineController.SpinChargeState spinChargeState = LaserTurbineController.SpinChargeState.zero;
		public LaserTurbineController.SpinChargeState NetworkspinChargeState
		{
			get
			{
				return this.spinChargeState;
			}
			[param: In]
			set
			{
				base.SetSyncVar<LaserTurbineController.SpinChargeState>(value, ref this.spinChargeState, 1U);
			}
		}
		private CharacterBody cachedOwnerBody;

		public CharacterBody ownerBody
		{
			get
			{
				return this.cachedOwnerBody;
			}
		}

		private void Awake()
		{
			this.genericOwnership = base.GetComponent<GenericOwnership>();
			this.genericOwnership.onOwnerChanged += this.OnOwnerChanged;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			LaserTurbineController.SpinChargeState networkspinChargeState = this.spinChargeState;
			networkspinChargeState.initialSpin = minSpin;
			networkspinChargeState.snapshotTime = Run.FixedTimeStamp.now;
			this.NetworkspinChargeState = networkspinChargeState;
		}

		private void Update()
		{
			if (NetworkClient.active)
			{
				this.UpdateClient();
			}
		}

		private void FixedUpdate()
		{
			Run.FixedTimeStamp now = Run.FixedTimeStamp.now;
			this.spin = this.spinChargeState.CalcCurrentSpinValue(now, spinDecayPerSecondAfterRefresh, minSpin);
			this.charge = this.spinChargeState.CalcCurrentChargeValue(now, spinDecayPerSecondAfterRefresh, minSpin);
			if (this.turbineDisplayRoot)
			{
				this.turbineDisplayRoot.gameObject.SetActive(this.showTurbineDisplay);
			}
		}

		private void OnEnable()
		{
			if (NetworkServer.active)
			{
				GlobalEventManager.onCharacterDeathGlobal += this.OnCharacterDeathGlobalServer;
			}
		}

		private void OnDisable()
		{
			GlobalEventManager.onCharacterDeathGlobal -= this.OnCharacterDeathGlobalServer;
		}

		[Server]
		public void ExpendCharge()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.LaserTurbineController::ExpendCharge()' called on client");
				return;
			}
			Run.FixedTimeStamp now = Run.FixedTimeStamp.now;
			float num = this.spinChargeState.CalcCurrentSpinValue(now, spinDecayPerSecondAfterRefresh, minSpin);
			num += spinGeneratedOnKill;
			LaserTurbineController.SpinChargeState networkspinChargeState = new LaserTurbineController.SpinChargeState
			{
				initialSpin = num,
				initialCharge = 0f,
				snapshotTime = now
			};
			this.NetworkspinChargeState = networkspinChargeState;
		}
		private void OnCharacterDeathGlobalServer(DamageReport damageReport)
		{
			if (damageReport.attacker == this.genericOwnership.ownerObject && damageReport.attacker != null)
			{
				this.OnOwnerKilledOtherServer();
			}
		}
		private void OnOwnerKilledOtherServer()
		{
			Run.FixedTimeStamp now = Run.FixedTimeStamp.now;
			float num = this.spinChargeState.CalcCurrentSpinValue(now, spinDecayPerSecondAfterRefresh, minSpin);
			float initialCharge = this.spinChargeState.CalcCurrentChargeValue(now, spinDecayPerSecondAfterRefresh, minSpin);
			num = Mathf.Min(num + spinGeneratedOnKill, maxSpin);
			LaserTurbineController.SpinChargeState networkspinChargeState = new LaserTurbineController.SpinChargeState
			{
				initialSpin = num,
				initialCharge = initialCharge,
				snapshotTime = now
			};
			this.NetworkspinChargeState = networkspinChargeState;
		}
		private void OnOwnerChanged(GameObject newOwner)
		{
			this.cachedOwnerBody = (newOwner ? newOwner.GetComponent<CharacterBody>() : null);
		}
        #region network
        private void UNetVersion()
		{
		}

		[Client]
		private void UpdateClient()
		{
			if (!NetworkClient.active)
			{
				Debug.LogWarning("[Client] function 'System.Void RoR2.LaserTurbineController::UpdateClient()' called on server");
				return;
			}
			float num = HGMath.CircleAreaToRadius(this.charge * HGMath.CircleRadiusToArea(1f));
			this.chargeIndicator.localScale = new Vector3(num, num, num);
			Vector3 localEulerAngles = this.spinIndicator.localEulerAngles;
			localEulerAngles.y += this.spin * Time.deltaTime * visualSpinRate;
			this.spinIndicator.localEulerAngles = localEulerAngles;
			AkSoundEngine.SetRTPCValue(this.spinRtpc, this.spin * this.spinRtpcScale, base.gameObject);
		}
		public override bool OnSerialize(NetworkWriter writer, bool forceAll)
		{
			if (forceAll)
			{
				_WriteSpinChargeState_LaserTurbineController(writer, this.spinChargeState);
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
				_WriteSpinChargeState_LaserTurbineController(writer, this.spinChargeState);
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
				this.spinChargeState = _ReadSpinChargeState_LaserTurbineController(reader);
				return;
			}
			int num = (int)reader.ReadPackedUInt32();
			if ((num & 1) != 0)
			{
				this.spinChargeState = _ReadSpinChargeState_LaserTurbineController(reader);
			}
		}

		public static void _WriteSpinChargeState_LaserTurbineController(NetworkWriter writer, LaserTurbineController.SpinChargeState value)
		{
			writer.Write(value.initialCharge);
			writer.Write(value.initialSpin);
			GeneratedNetworkCode._WriteFixedTimeStamp_Run(writer, value.snapshotTime);
		}

		public static LaserTurbineController.SpinChargeState _ReadSpinChargeState_LaserTurbineController(NetworkReader reader)
		{
			return new LaserTurbineController.SpinChargeState
			{
				initialCharge = reader.ReadSingle(),
				initialSpin = reader.ReadSingle(),
				snapshotTime = GeneratedNetworkCode._ReadFixedTimeStamp_Run(reader)
			};
		}
        #endregion

        public struct SpinChargeState : IEquatable<SpinChargeState>
		{
			public static readonly SpinChargeState zero = new SpinChargeState
			{
				initialCharge = 0f,
				initialSpin = 0f,
				snapshotTime = Run.FixedTimeStamp.negativeInfinity
			};

			public float initialCharge;
			public float initialSpin;
			public Run.FixedTimeStamp snapshotTime;

			public float CalcCurrentSpinValue(Run.FixedTimeStamp currentTime, float spinDecayRate, float minSpin)
			{
				return Mathf.Max(this.initialSpin - spinDecayRate * (currentTime - this.snapshotTime), minSpin);
			}
			public float CalcCurrentChargeValue(Run.FixedTimeStamp currentTime, float spinDecayRate, float minSpin)
			{
				float deltaTime = currentTime - this.snapshotTime;
				float minCharge = minSpin * deltaTime;
				float deltaSpin = this.initialSpin - minSpin;
				float t = Mathf.Min(Trajectory.CalculateFlightDuration(deltaSpin, -spinDecayRate) * 0.5f, deltaTime);
				float chargeFromSpin = Trajectory.CalculatePositionYAtTime(0f, deltaSpin, t, -spinDecayRate);
				return Mathf.Min(this.initialCharge + minCharge + chargeFromSpin, 1f);
			}
			public bool Equals(SpinChargeState other)
			{
				return this.initialCharge.Equals(other.initialCharge) && this.initialSpin.Equals(other.initialSpin) && this.snapshotTime.Equals(other.snapshotTime);
			}
			public override bool Equals(object obj)
			{
				if (obj is SpinChargeState)
				{
					SpinChargeState other = (SpinChargeState)obj;
					return this.Equals(other);
				}
				return false;
			}
			public override int GetHashCode()
			{
				return (this.initialCharge.GetHashCode() * 397 ^ this.initialSpin.GetHashCode()) * 397 ^ this.snapshotTime.GetHashCode();
			}
		}
	}
}
