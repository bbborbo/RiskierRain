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
using static MoreStats.OnJump;

namespace JumpRework
{
    public partial class JumpReworkPlugin
    {
        public static int urnJumpCount = 2;
        public int urnBallCountBase = 3;
        public int urnBallCountStack = 0;
        public float urnBallYawSpread = 25f;
        public float urnBallDamageCoefficient = 2.5f;
        public float urnBallChance = 0.3f;
        private void MiredUrnRework()
        {
            OnJumpEvent += UrnOnJump;
            //JumpStatHook.OnJumpEvent += UrnOnJump;
            On.RoR2.Items.SiphonOnLowHealthItemBodyBehavior.OnEnable += VoidVanillaUrnBehavior;
            LanguageAPI.Add("ITEM_SIPHONONLOWHEALTH_PICKUP", "Triple jump. Jumping fires tar balls in front of you.");
            LanguageAPI.Add("ITEM_SIPHONONLOWHEALTH_DESC",
                $"Gain <style=cIsUtility>{urnJumpCount}</style> jumps. " +
                $"While in danger, jumping has a <style=cIsUtility>{Tools.ConvertDecimal(urnBallChance)} chance</style> to fire " +
                $"<style=cIsDamage>sentient tar pots</style> in front of you, dealing " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(urnBallDamageCoefficient)}</style> damage " +
                $"<style=cStack>(+{Tools.ConvertDecimal(urnBallDamageCoefficient)} per stack)</style> " +
                $"and <style=cIsUtility>slowing</style> enemies hit.");
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

        private void UrnOnJump(CharacterMotor motor, CharacterBody body, ref float verticalBonus)
        {
            if (body.outOfDanger)
                return;

            int itemCount = 0;
            Inventory inv = body.inventory;
            if (inv)
            {
                itemCount = body.inventory.GetItemCount(RoR2Content.Items.SiphonOnLowHealth);
            }
            if (Util.CheckRoll((1 - Mathf.Pow(1 - urnBallChance, itemCount)) * 100, body.master))
            {
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
                if (body.hasAuthority)
                {
                    if (JumpReworkPlugin.IsMissileArtifactEnabled())
                    {
                        int n = 3;
                        float totalYaw = urnBallYawSpread * 2 / (n + 1);
                        float totalSpread = (n - 1) * urnBallYawSpread;
                        float halfSpread = totalSpread / 2;

                        for (int i = 0; i < n; i++)
                        {
                            //float currentSpread = Mathf.FloorToInt(i - (urnBallCountBase - 1) / 2f) / (urnBallCountBase - 1) * totalSpread;
                            //float currentSpread = Mathf.Lerp(0, totalSpread, i / (urnBallCountBase - 1)) - halfSpread;
                            float currentSpread = (i / (n - 1)) * totalSpread - halfSpread;
                            float bonusYaw = (urnBallYawSpread * i) - (totalYaw * 2f);

                            Vector3 forward = Util.ApplySpread(aimRay.direction, 0, 0, 1, 0, bonusYaw, 0);
                            //Vector3 fwd = Vector3.ProjectOnPlane(forward, Vector3.up);

                            FireTarballProjectile(body, aimRay.origin, forward);
                        }
                    }
                    else
                    {
                        FireTarballProjectile(body, aimRay.origin, aimRay.direction);
                    }
                }
                body.AddSpreadBloom(FireTarball.spreadBloomValue);
            }
        }

        private void FireTarballProjectile(CharacterBody body, Vector3 origin, Vector3 forward)
        {
            ProjectileManager.instance.FireProjectile(
                miredUrnTarball, origin, Util.QuaternionSafeLookRotation(forward),
                body.gameObject, body.damage * urnBallDamageCoefficient, 0f,
                Util.CheckRoll(body.crit, body.master), DamageColorIndex.Default, null, -1f);
        }
    }
}
