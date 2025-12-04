using EntityStates;
using EntityStates.Captain.Weapon;
using RoR2;
using SurvivorTweaks.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SurvivorTweaks.States.Captain
{
    public class FireWormhole : BaseSkillState
    {
        public const float minDistance = 2;
        public Vector3 startPos;
        public Vector3 endpointPos;

        public float baseDuration => PocketWormholeSkill.baseExitDuration;
        float duration;

        public override void OnEnter()
        {
            duration = baseDuration / base.attackSpeedStat;
            base.OnEnter();
            Fire();
        }

        private void Fire()
        {
            if (!this.rigidbody)
            {
                activatorSkillSlot.AddOneStock();
                return;
            }
            Util.PlaySound(FireTazer.attackString, base.gameObject);
            base.AddRecoil(-1f * FireTazer.recoilAmplitude, -1.5f * FireTazer.recoilAmplitude, -0.25f * FireTazer.recoilAmplitude, 0.25f * FireTazer.recoilAmplitude);
            base.characterBody.AddSpreadBloom(FireTazer.bloom);
            Ray aimRay = base.GetAimRay();
            if (FireTazer.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(FireTazer.muzzleflashEffectPrefab, base.gameObject, FireTazer.targetMuzzle, false);
            }
            base.PlayAnimation("Gesture, Additive", "FireCaptainShotgun");
            base.PlayAnimation("Gesture, Override", "FireCaptainShotgun");

            if (NetworkServer.active)
            {
                FireZipline();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge > this.duration)
                outer.SetNextStateToMain();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.startPos);
            writer.Write(this.endpointPos);
        }
        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            this.startPos = reader.ReadVector3();
            this.endpointPos = reader.ReadVector3();
        }

        void FireZipline()
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Zipline"));
            ZiplineController component2 = gameObject.GetComponent<ZiplineController>();
            component2.SetPointAPosition(startPos);
            component2.SetPointBPosition(endpointPos);
            gameObject.AddComponent<DestroyOnTimer>().duration = PocketWormholeSkill.maxTunnelDuration;
            NetworkServer.Spawn(gameObject);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
