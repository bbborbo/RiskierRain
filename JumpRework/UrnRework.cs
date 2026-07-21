using EntityStates.ClayBoss;
using EntityStates.Headstompers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
using static MoreStats.JumpAPI;
using RoR2.Items;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace JumpRework
{
    public partial class JumpReworkPlugin
    {
        public static int urnBallInterval => UrnBallInterval.Value;
        public static int urnBallCountBase = 3;
        public static int urnBallCountStack = 0;
        public static float urnBallYawSpread = 25f;
        private void MiredUrnRework()
        {
            //JumpStatHook.OnJumpEvent += UrnOnJump;
            On.RoR2.Items.SiphonOnLowHealthItemBodyBehavior.OnEnable += VoidVanillaUrnBehavior;
            LanguageAPI.Add("ITEM_SIPHONONLOWHEALTH_PICKUP", "Triple jump. Jumping fires tar balls in front of you.");
            LanguageAPI.Add("ITEM_SIPHONONLOWHEALTH_DESC",
                $"Gain <style=cIsUtility>{UrnJumpCount.Value}</style> jumps. " +
                $"After every <style=cIsUtility>{urnBallInterval}</style> air jumps, " +
                $"roll a <style=cIsDamage>sentient tar pot</style>, dealing " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(UrnBallDamageCoefficient.Value)}</style> base damage " +
                $"and <style=cIsUtility>tarring</style> enemies hit.");
        }

        public static GameObject miredUrnTarball;
        private void CreateMiredUrnTarball()
        {
            miredUrnTarball = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClayBoss/TarSeeker.prefab").WaitForCompletion().InstantiateClone("MiredUrnTarball", true);

            ProjectileImpactExplosion pie = miredUrnTarball.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.lifetime = 2;
            }

            R2API.ContentAddition.AddProjectile(miredUrnTarball);
        }

        private void VoidVanillaUrnBehavior(On.RoR2.Items.SiphonOnLowHealthItemBodyBehavior.orig_OnEnable orig, RoR2.Items.SiphonOnLowHealthItemBodyBehavior self)
        {
            self.DestroyAttachment();
            Destroy(self);
        }
    }
    public class UrnOnJumpBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => RoR2Content.Items.SiphonOnLowHealth;


        void OnEnable()
        {
            OnJumpEvent += UrnJumpSynergy;
        }

        void OnDisable()
        {
            OnJumpEvent -= UrnJumpSynergy;
        }

        private void UrnJumpSynergy(CharacterMotor sender, JumpSynergyInfo jumpSynergyInfo)
        {
            if(IsThisJumpUrnJump(sender, jumpSynergyInfo))
            {
                UrnOnJump();
            }
        }

        private int jumpCount = 0;
        private bool IsThisJumpUrnJump(CharacterMotor sender, JumpSynergyInfo info)
        {
            if (IsBaseJump(sender))
                return false;
            jumpCount++;

            if(jumpCount % JumpReworkPlugin.urnBallInterval == 0)
            {
                jumpCount = 0;
                return true;
            }
            return false;
        }

        private void UrnOnJump()
        {
            //if (body.outOfDanger)
            //    return;

            Util.PlaySound(FireTarball.attackSoundString, body.gameObject);
            Ray aimRay = body.inputBank.GetAimRay();
            /*if (this.modelTransform)
            {
                ChildLocator component = this.modelTransform.GetComponent<ChildLocator>();
                if (component)
                {
                    Transform transform = component.FindChild(targetMuzzle);
                    if (transform)
                    {
                        this.aimRay.origin = transform.position;
                    }
                }
            }*/
            //base.AddRecoil(-1f * FireTarball.recoilAmplitude, -2f * FireTarball.recoilAmplitude, -1f * FireTarball.recoilAmplitude, 1f * FireTarball.recoilAmplitude);
            if (FireTarball.effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(FireTarball.effectPrefab, body.gameObject, "", false);
            }

            if (JumpReworkPlugin.IsMissileArtifactEnabled())
            {
                int n = 3;
                float totalYaw = JumpReworkPlugin.urnBallYawSpread * 2 / (n + 1);
                float totalSpread = (n - 1) * JumpReworkPlugin.urnBallYawSpread;
                float halfSpread = totalSpread / 2;

                for (int i = 0; i < n; i++)
                {
                    //float currentSpread = Mathf.FloorToInt(i - (urnBallCountBase - 1) / 2f) / (urnBallCountBase - 1) * totalSpread;
                    //float currentSpread = Mathf.Lerp(0, totalSpread, i / (urnBallCountBase - 1)) - halfSpread;
                    float currentSpread = (i / (n - 1)) * totalSpread - halfSpread;
                    float bonusYaw = (JumpReworkPlugin.urnBallYawSpread * i) - (totalYaw * 2f);

                    Vector3 forward = Util.ApplySpread(aimRay.direction, 0, 0, 1, 0, bonusYaw, 0);
                    //Vector3 fwd = Vector3.ProjectOnPlane(forward, Vector3.up);

                    FireTarballProjectile(body, aimRay.origin, forward);
                }
            }
            else
            {
                FireTarballProjectile(body, aimRay.origin, aimRay.direction);
            }

            body.AddSpreadBloom(FireTarball.spreadBloomValue);
        }
        private void FireTarballProjectile(CharacterBody body, Vector3 origin, Vector3 forward)
        {
            ProjectileManager.instance.FireProjectile(
                JumpReworkPlugin.miredUrnTarball, origin, Util.QuaternionSafeLookRotation(forward),
                body.gameObject, body.damage * JumpReworkPlugin.UrnBallDamageCoefficient.Value, 0f,
                Util.CheckRoll(body.crit, body.master), DamageColorIndex.Default, null, -1f);
        }
    }
}
