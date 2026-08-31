using EntityStates.AI;
using EntityStates.AI.Walker;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using SwanSongExtended.Elites;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.States.AI
{
	/// <summary>
	/// converge is a variation on entitystates.ai.walker.combat which is mostly the same except i have full control over where the enemy moves >:3
	/// mwahahahahahahhhhhhhhhflowers
	/// </summary>
    public class Converge : BaseAIState
	{
		public static float leaderStopDistance = 5f;
		public static float followerStrafeDistanceMin = 6f;
		public static float followerStrafeDistanceMax = StormsCore.cycloneRadius * 0.75f;
		float unreachableTimestamp = float.PositiveInfinity;
		//this method is entirely new!
		private Vector3? GetDesiredConvergePosition(out AISkillDriver.MovementType movementType)
		{
			movementType = AISkillDriver.MovementType.FollowMoveTarget;
			Vector3 defaultPos = GetDefaultConvergePosition();

			//if no cyclone: leader does whatever it wants. followers follow leader
			if (CycloneController.instance.defaultConvergePosition == null)
			{
				if (body.HasBuff(StormsCore.CycloneLeader))
                {
					return null;
                }

				return defaultPos;
			}

			//if cyclone...
			//leader runs to, then stops near center of cyclone
			Vector3 cyclonePos = CycloneController.instance.defaultConvergePosition.Value;
			float sqrDistance = ((Vector2)cyclonePos - (Vector2)body.footPosition).sqrMagnitude;
			if (body.HasBuff(StormsCore.CycloneLeader))
			{
				if (sqrDistance <= leaderStopDistance * leaderStopDistance)
				{
					movementType = AISkillDriver.MovementType.Stop;
				}
				return cyclonePos;
			}

			//followers run to, then strafe near center of cyclone
			if(sqrDistance <= followerStrafeDistanceMin)
            {
				movementType = AISkillDriver.MovementType.FleeMoveTarget;
            }
			else if (sqrDistance <= followerStrafeDistanceMax)
            {
				movementType = AISkillDriver.MovementType.CircleMoveTargetCCW;
            }
			return defaultPos;
        }

		public Vector3 GetDefaultConvergePosition()
        {
			return body.isFlying ? CycloneController.convergePositionAir : CycloneController.convergePositionGround;
		}

		//this method is a modification of a vanilla method
		protected void UpdateAI(float deltaTime)
		{
			//vanilla: resetting internal driver values so no actions persist from the last update
			BaseAI.SkillDriverEvaluation skillDriverEvaluation = base.ai.skillDriverEvaluation;
			this.dominantSkillDriver = skillDriverEvaluation.dominantSkillDriver;
			this.currentSkillSlot = SkillSlot.None;
			this.currentSkillMeetsActivationConditions = false;
			this.bodyInputs.moveVector = Vector3.zero;
			AISkillDriver.MovementType movementType = AISkillDriver.MovementType.Stop; 
			float moveInputScale = 1f;
			bool skillRequiresLosToMoveTarget = false;
			bool skillRequiresLosToAimTarget = false;
			bool skillRequiresAimConfirmation = false;

			//v: return if this other stuff is false. if this stuff is false the character does nothing! when would that ever happen though, i dunno!
			if (!base.body || !base.bodyInputBank)
			{
				return;
			}

			//v: initialize internal driver values
			if (this.dominantSkillDriver)
			{
				movementType = this.dominantSkillDriver.movementType;
				this.currentSkillSlot = this.dominantSkillDriver.skillSlot;
				skillRequiresLosToMoveTarget = this.dominantSkillDriver.activationRequiresTargetLoS;
				skillRequiresLosToAimTarget = this.dominantSkillDriver.activationRequiresAimTargetLoS;
				skillRequiresAimConfirmation = this.dominantSkillDriver.activationRequiresAimConfirmation;
				moveInputScale = this.dominantSkillDriver.moveInputScale;
				base.ai.aimVectorDampTimeOverride = this.dominantSkillDriver.aimVectorDampTimeOverride;
				base.ai.aimVectorMaxSpeedOverride = this.dominantSkillDriver.aimVectorMaxSpeedOverride;
			}
			else
			{
				base.ai.aimVectorDampTimeOverride = -1f;
				base.ai.aimVectorMaxSpeedOverride = -1f;
			}

			//v: more initialization stuff
			Vector3 currentPosition = base.bodyTransform.position;
			Vector3 aimOrigin = base.bodyInputBank.aimOrigin;
			BroadNavigationSystem.Agent broadNavigationAgent = base.ai.broadNavigationAgent;
			BroadNavigationSystem.AgentOutput output = broadNavigationAgent.output;
			BaseAI.Target target = skillDriverEvaluation.target;
			BaseAI.Target aimTarget = skillDriverEvaluation.aimTarget;

			bool hasTarget = (target != null) ? target.gameObject : null;
			bool hasAimTarget = (aimTarget != null) ? aimTarget.gameObject : null;

			///b: finally new stuff - force the monster to pathfind inside the cyclone
			/// ADDITIONALLY, continue to pathfind inside the cyclone until its target ALSO enters the cyclone
			///    due to cyclone protection mechanics, i think if any character enters the cyclone and begins attacking, it will cause the victim to shift target
			/// so basically, if a howling elite is inside the cyclone and being attacked by player-allied character, 
			///    it will use movement patterns according to its skill drivers like normal
			///    (this is basically the main difference between converge and combat)
			bool isConverging = false;
			bool isLeader = this.body.HasBuff(StormsCore.CycloneLeader);
			Vector3? convergeLocation = null;
			if (this.body.HasBuff(StormsCore.CycloneProtection) == false 
				|| isLeader
				|| (hasAimTarget ? 
					(aimTarget.characterBody.HasBuff(StormsCore.CycloneProtection) == false) 
					: false)
				)
			{
				isConverging = true;

				convergeLocation = GetDesiredConvergePosition(out movementType);
                if (isLeader)
                {
					base.ai.aimVectorDampTimeOverride = WhirlwindAspect.squallAimDamping;//templar: 0.1
					base.ai.aimVectorMaxSpeedOverride = WhirlwindAspect.squallAimMaxSpeed;//templar: 60f
                }
			}

			if (hasTarget || isConverging)
			{
				if (this.fallbackNodeStartAge + this.fallbackNodeDuration < base.fixedAge)
				{
					//b: another difference: this overrides the goal position when pathfinding
					if (isConverging && convergeLocation != null)
						base.ai.SetGoalPosition(convergeLocation);
					else
						base.ai.SetGoalPosition(target);
				}
				Vector3 targetPosition = currentPosition;
				if(hasTarget)
					target.GetBullseyePosition(out targetPosition);
				Vector3 nextPosition = currentPosition;
				bool allowWalkOffCliff = true;

				Vector3 desiredPosition = output.nextPosition ?? this.myBodyFootPosition;
				if (this.dominantSkillDriver && this.dominantSkillDriver.ignoreNodeGraph)
					desiredPosition = targetPosition;

				Vector3 desiredForwardDirection = (desiredPosition - this.myBodyFootPosition).normalized * 10f;
				Vector3 desiredStrafeDirection = Vector3.Cross(Vector3.up, desiredForwardDirection);

                //v: I LOVE IF-ELSE CHAINS <3
				//b: jk i changed it to a switch statement because FUUUCCCKKKK
                #region ugly ass if else chain
                switch (movementType)
                {
					case AISkillDriver.MovementType.ChaseMoveTarget:
						nextPosition = desiredPosition + (currentPosition - this.myBodyFootPosition);
						break;
					case AISkillDriver.MovementType.FleeMoveTarget:
						nextPosition -= desiredForwardDirection;
						if (isConverging)
							allowWalkOffCliff = false;
						break;
					case AISkillDriver.MovementType.StrafeMovetarget:
						if (this.strafeTimer <= 0f)
						{
							if (this.strafeDirection == 0f)
							{
								this.strafeDirection = ((UnityEngine.Random.Range(0, 1) == 0) ? -1f : 1f);
							}
							this.strafeTimer = strafeDuration;
						}
						nextPosition += desiredStrafeDirection * this.strafeDirection;
						allowWalkOffCliff = false;
						break;
					case AISkillDriver.MovementType.FollowMoveTarget:
						nextPosition = desiredPosition + base.ai.followPattern.targetOffset;
						break;
					case AISkillDriver.MovementType.CircleMoveTargetCW:
						nextPosition += desiredStrafeDirection * -1f;
						allowWalkOffCliff = false;
						break;
					case AISkillDriver.MovementType.CircleMoveTargetCCW:
						nextPosition += desiredStrafeDirection;
						allowWalkOffCliff = false;
						break;
				}
                #endregion

				//v: set navigator stuff
                base.ai.localNavigator.targetPosition = nextPosition;
				base.ai.localNavigator.allowWalkOffCliff = allowWalkOffCliff;
				base.ai.localNavigator.Update(deltaTime);
				if (base.ai.localNavigator.wasObstructedLastUpdate)
				{
					this.strafeDirection *= -1f;
				}
				this.bodyInputs.moveVector = base.ai.localNavigator.moveVector;
				this.bodyInputs.moveVector = this.bodyInputs.moveVector * moveInputScale;

				//v: run checks for skill activation conditions
				//if the skill does not require aim confirmation, or if the skill requires aim confirmation and has it
				if (!skillRequiresAimConfirmation || base.ai.hasAimConfirmation)
				{
					bool currentSkillMeetsActivationConditions = true;
					if (skillDriverEvaluation.target == skillDriverEvaluation.aimTarget && (skillRequiresLosToMoveTarget && skillRequiresLosToAimTarget))
					{
						skillRequiresLosToAimTarget = false;
					}
					if (currentSkillMeetsActivationConditions && skillRequiresLosToMoveTarget)
					{
						currentSkillMeetsActivationConditions = skillDriverEvaluation.target.TestLOSNow();
					}
					if (currentSkillMeetsActivationConditions && skillRequiresLosToAimTarget)
					{
						currentSkillMeetsActivationConditions = skillDriverEvaluation.aimTarget.TestLOSNow();
					}
					if (currentSkillMeetsActivationConditions)
					{
						this.currentSkillMeetsActivationConditions = true;
					}
				}
			}
			if (output.lastPathUpdate > this.lastPathUpdate && !output.targetReachable && this.fallbackNodeStartAge + this.fallbackNodeDuration < base.fixedAge)
			{
                if (isConverging)
                {
					//shitty fix
					TeleportHelper.TeleportBody(this.body, GetDefaultConvergePosition(), false);
                }
                else
				{
					broadNavigationAgent.goalPosition = base.PickRandomNearbyReachablePosition();
					broadNavigationAgent.InvalidatePath();
				}
			}
			this.lastPathUpdate = output.lastPathUpdate;
		}

        //this method is a modification of a vanilla method - almost identical
        public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.ai && base.body)
			{
				float deltaTime = base.GetDeltaTime();
				this.aiUpdateTimer -= deltaTime;
				this.strafeTimer -= deltaTime;
				this.UpdateFootPosition();
				if (this.aiUpdateTimer <= 0f)
				{
					this.aiUpdateTimer = BaseAIState.cvAIUpdateInterval.value / 2;
					this.UpdateAI(BaseAIState.cvAIUpdateInterval.value / 2);

					//b: this is the ONLY modification to this method. should be obvious!
                    if (WhirlwindAspect.instance.IsElite(this.body) == false || CycloneController.GetShouldConverge() == false)
                    {
						if (!this.dominantSkillDriver)
						{
							this.outer.SetNextState(new LookBusy());
						}
                        else
                        {
							this.outer.SetNextState(new Combat());
                        }
					}
				}
				this.UpdateBark();
				ai.UpdateBodyAim(Time.fixedDeltaTime);
			}
		}

		#region EVERYTHING WITHIN REGION IS IDENTICAL TO VANILLA CODE FROM combat AND I DO NOT CLAIM OWNERSHIP NOR RESPONSIBILITY OVER

		public override BaseAI.BodyInputs GenerateBodyInputs(in BaseAI.BodyInputs previousBodyInputs)
		{
			bool pressSkill = false;
			bool pressSkill2 = false;
			bool pressSkill3 = false;
			bool pressSkill4 = false;
			if (base.bodyInputBank)
			{
				AISkillDriver.ButtonPressType buttonPressType = AISkillDriver.ButtonPressType.Abstain;
				if (this.dominantSkillDriver)
				{
					buttonPressType = this.dominantSkillDriver.buttonPressType;
				}
				bool flag = false;
				switch (this.currentSkillSlot)
				{
					case SkillSlot.Primary:
						flag = previousBodyInputs.pressSkill1;
						break;
					case SkillSlot.Secondary:
						flag = previousBodyInputs.pressSkill2;
						break;
					case SkillSlot.Utility:
						flag = previousBodyInputs.pressSkill3;
						break;
					case SkillSlot.Special:
						flag = previousBodyInputs.pressSkill4;
						break;
				}
				bool flag2 = this.currentSkillMeetsActivationConditions;
				switch (buttonPressType)
				{
					case AISkillDriver.ButtonPressType.Abstain:
						flag2 = false;
						break;
					case AISkillDriver.ButtonPressType.TapContinuous:
						flag2 &= !flag;
						break;
				}
				switch (this.currentSkillSlot)
				{
					case SkillSlot.Primary:
						pressSkill = flag2;
						break;
					case SkillSlot.Secondary:
						pressSkill2 = flag2;
						break;
					case SkillSlot.Utility:
						pressSkill3 = flag2;
						break;
					case SkillSlot.Special:
						pressSkill4 = flag2;
						break;
				}
			}
			this.bodyInputs.pressSkill1 = pressSkill;
			this.bodyInputs.pressSkill2 = pressSkill2;
			this.bodyInputs.pressSkill3 = pressSkill3;
			this.bodyInputs.pressSkill4 = pressSkill4;
			this.bodyInputs.pressSprint = false;
			this.bodyInputs.pressActivateEquipment = false;
			this.bodyInputs.desiredAimDirection = Vector3.zero;
			if (this.dominantSkillDriver)
			{
				this.bodyInputs.pressSprint = this.dominantSkillDriver.shouldSprint;
				this.bodyInputs.pressActivateEquipment = (this.dominantSkillDriver.shouldFireEquipment && !previousBodyInputs.pressActivateEquipment);
				AISkillDriver.AimType aimType = this.dominantSkillDriver.aimType;
				BaseAI.Target aimTarget = base.ai.skillDriverEvaluation.aimTarget;
				if (aimType == AISkillDriver.AimType.MoveDirection)
				{
					base.AimInDirection(ref this.bodyInputs, this.bodyInputs.moveVector);
				}
				if (aimTarget != null)
				{
					base.AimAt(ref this.bodyInputs, aimTarget);
				}
			}
			base.ModifyInputsForJumpIfNeccessary(ref this.bodyInputs);
			return this.bodyInputs;
		}
		public override void OnEnter()
		{
			base.OnEnter();
			this.activeSoundTimer = UnityEngine.Random.Range(3f, 8f);
			if (base.ai)
			{
				this.lastPathUpdate = base.ai.broadNavigationAgent.output.lastPathUpdate;
				base.ai.broadNavigationAgent.InvalidatePath();
			}
			this.fallbackNodeStartAge = float.NegativeInfinity;
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		protected void UpdateFootPosition()
		{
			this.myBodyFootPosition = base.body.temporaryPathfindingFootpositionDoNotUseWillBePatchedOut;
			BroadNavigationSystem.Agent broadNavigationAgent = this.ai.broadNavigationAgent;
			broadNavigationAgent.currentPosition = new Vector3?(this.myBodyFootPosition);
		}

		protected void UpdateBark()
		{
			this.activeSoundTimer -= base.GetDeltaTime();
			if (this.activeSoundTimer <= 0f)
			{
				this.activeSoundTimer = UnityEngine.Random.Range(3f, 8f);
				base.body.CallRpcBark();
			}
		}

		private float strafeDirection;
		private const float strafeDuration = 0.25f;
		private float strafeTimer;
		private float activeSoundTimer;
		private float aiUpdateTimer;
		public float timeChasing;
		private const float minUpdateInterval = 0.16666667f;
		private const float maxUpdateInterval = 0.2f;
		private AISkillDriver dominantSkillDriver;
		protected bool currentSkillMeetsActivationConditions;
		protected SkillSlot currentSkillSlot = SkillSlot.None;
		protected Vector3 myBodyFootPosition;
		private float lastPathUpdate;
		private float fallbackNodeStartAge;
		private readonly float fallbackNodeDuration = 4f;
		#endregion
	}
}
