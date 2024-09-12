using BepInEx;
using EntityStates;
using EntityStates.ClayBoss;
using EntityStates.Headstompers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace RiskierRainContent
{
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public static bool IsDoubleJump(CharacterMotor motor, CharacterBody body)
        {
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount + 1;

            if (timesJumped > baseJumpCount)
                return true;
            return false;
        }
        public static bool IsBaseJump(CharacterMotor motor, CharacterBody body)
        {
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount + 1;

            if (timesJumped <= baseJumpCount)
                return true;
            return false;
        }
        public static bool IsLastJump(CharacterMotor motor, CharacterBody body)
        {
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount + 1;

            if (timesJumped == maxJumpCount)
                return true;
            return false;
        }
        public static int fallBootsJumpCount = 3;
        public bool fallBootsSuperJumpLast = true;
        public float superJumpStrengthFirst = 1.2f; //2
        public float superJumpStrengthLast = 2f; //2

        public static int featherJumpCount = 1;
        public float hopooDamageBuffDuration = 0.5f;
        public static float hopooDamageIncreasePerBuff = 0.1f;

        public static int urnJumpCount = 2;
        public int urnBallCountBase = 3;
        public int urnBallCountStack = 0;
        public float urnBallYawSpread = 25f;
        public float urnBallDamageCoefficient = 2.5f;
        public float urnBallChance = 0.3f;

        public static int jarJumpCount = 1;


        void JumpReworks()
        {
            IL.RoR2.CharacterBody.RecalculateStats += JumpReworkJumpCount;
            On.EntityStates.GenericCharacterMain.ApplyJumpVelocity += DoJumpEvent;

            FeatherRework();
            StompersRework();
            MiredUrnRework();
        }

        #region hopoo feather
        float featherBaseDuration = 0.75f;
        float featherStackDuration = 0.5f;
        private void FeatherRework()
        {
            JumpStatHook.OnJumpEvent += FeatherOnJump;
            //On.RoR2.GlobalEventManager.OnCharacterHitGroundServer += FeatherOnLandServer;
            //GetStatCoefficients += FeatherDamageBoost;
            LanguageAPI.Add("ITEM_FEATHER_PICKUP", "Double jump. Jumping gives you a boost of movement speed.");
            LanguageAPI.Add("ITEM_FEATHER_DESC",
                $"Gain <style=cIsUtility>{featherJumpCount}</style> jumps. " +
                $"<style=cIsUtility>On jump</style>, increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>125%</style>, " +
                $"fading over <style=cIsUtility>{featherBaseDuration}</style> <style=cStack>(+{featherStackDuration} per stack)</style> seconds.");
        }

        private void FeatherDamageBoost(CharacterBody sender, StatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv)
            {
                int featherCount = inv.GetItemCount(RoR2Content.Items.Feather);
                int buffCountP = sender.GetBuffCount(CoreModules.Assets.hopooDamageBuff);
                int buffCountT = sender.GetBuffCount(CoreModules.Assets.hopooDamageBuffTemporary);
                if(featherCount > 0)
                {
                    if(buffCountT > buffCountP)
                    {
                        args.damageMultAdd += hopooDamageIncreasePerBuff * buffCountT * featherCount;
                    }
                    else
                    {
                        args.damageMultAdd += hopooDamageIncreasePerBuff * buffCountP * featherCount;
                    }
                }
            }
        }

        private void FeatherOnJump(CharacterMotor motor, ref float verticalBonus)
        {
            CharacterBody body = motor.body;
            if (body)
            {
                Inventory inv = body.inventory;
                if(inv)
                {
                    int count = inv.GetItemCount(RoR2Content.Items.Feather);
                    if(count > 0 && IsDoubleJump(motor, body))
                    {
                        int increments = 5;
                        float totalDuration = featherBaseDuration + (float)(count - 1) * featherStackDuration;
                        body.ClearTimedBuffs(DLC1Content.Buffs.KillMoveSpeed);
                        for (int l = 0; l < increments; l++)
                        {
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, totalDuration * (float)(l + 1) / (float)increments);
                        }
                        EffectData effectData = new EffectData();
                        effectData.origin = body.corePosition;
                        CharacterMotor characterMotor = body.characterMotor;
                        bool flag = false;
                        if (characterMotor)
                        {
                            Vector3 moveDirection = characterMotor.moveDirection;
                            if (moveDirection != Vector3.zero)
                            {
                                effectData.rotation = Util.QuaternionSafeLookRotation(moveDirection);
                                flag = true;
                            }
                        }
                        if (!flag)
                        {
                            effectData.rotation = body.transform.rotation;
                        }
                        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MoveSpeedOnKillActivate"), effectData, true);
                    }
                    //body.AddBuff(Assets.hopooDamageBuff);
                }
            }
        }

        private void FeatherOnLandServer(On.RoR2.GlobalEventManager.orig_OnCharacterHitGroundServer orig, GlobalEventManager self, CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            orig(self, characterBody, hitGroundInfo);
            int buffCount = characterBody.GetBuffCount(CoreModules.Assets.hopooDamageBuff);
            for (int i = 0; i < buffCount; i++)
            {
                characterBody.RemoveBuff(CoreModules.Assets.hopooDamageBuff);
                characterBody.AddTimedBuff(CoreModules.Assets.hopooDamageBuffTemporary, hopooDamageBuffDuration);
            }
        }

        private void FeatherOnLand(On.RoR2.GlobalEventManager.orig_OnCharacterHitGround orig, GlobalEventManager self, CharacterBody characterBody, Vector3 impactVelocity)
        {
            orig(self, characterBody, impactVelocity);
            int buffCount = characterBody.GetBuffCount(CoreModules.Assets.hopooDamageBuff);
            for (int i = 0; i < buffCount; i++)
            {
                characterBody.RemoveBuff(CoreModules.Assets.hopooDamageBuff);
                if(characterBody.hasAuthority)
                    characterBody.AddTimedBuff(CoreModules.Assets.hopooDamageBuffTemporary, hopooDamageBuffDuration);
            }
        }
        #endregion

        #region headset stompers fallboots
        private void StompersRework()
        {
            IL.EntityStates.Headstompers.HeadstompersIdle.FixedUpdateAuthority += HeadstompersJumpBoost;
            LanguageAPI.Add("ITEM_FALLBOOTS_PICKUP", "Quadruple jump. Hold 'Interact' to slam down to the ground.");
            LanguageAPI.Add("ITEM_FALLBOOTS_DESC",
                $"Gain <style=cIsUtility>{fallBootsJumpCount}</style> jumps. " +
                $"Creates a <style=cIsDamage>5m-100m</style> radius <style=cIsDamage>kinetic explosion</style> " +
                $"on hitting the ground, dealing " +
                $"<style=cIsDamage>1000%-10000%</style> base damage " +
                $"that scales up with <style=cIsDamage>fall distance</style>. " +
                $"Recharges in <style=cIsDamage>10</style> " +
                $"<style=cStack>(-50% per stack)</style> seconds.");
        }

        private void HeadstompersJumpBoost(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<BaseHeadstompersState>("get_isGrounded")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, HeadstompersIdle, bool>>((isGrounded, self) =>
            {
                bool shouldSuperJump = isGrounded;
                if (fallBootsSuperJumpLast)
                {
                    CharacterMotor motor = self.bodyMotor;
                    if (motor)
                    {
                        shouldSuperJump = IsLastJump(motor, self.body);
                    }
                }

                return shouldSuperJump;
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdflda<Vector3>("y"),
                x => x.MatchDup()
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, fallBootsSuperJumpLast ? superJumpStrengthLast : superJumpStrengthFirst);
        }
        #endregion

        #region mired urn
        private void MiredUrnRework()
        {
            JumpStatHook.OnJumpEvent += UrnOnJump;
            On.RoR2.Items.SiphonOnLowHealthItemBodyBehavior.OnEnable += VoidVanillaUrnBehavior;
            LanguageAPI.Add("ITEM_SIPHONONLOWHEALTH_PICKUP", "Triple jump. Jumping fires tar balls in front of you.");
            LanguageAPI.Add("ITEM_SIPHONONLOWHEALTH_DESC",
                $"Gain <style=cIsUtility>{urnJumpCount}</style> jumps. " +
                $"While in danger, jumping has a <style=cIsUtility>{Tools.ConvertDecimal(urnBallChance)} chance</style> to fire " +
                $"<style=cIsDamage>sentient tar pots</style> in a spread in front of you, dealing " +
                $"<style=cIsDamage>{urnBallCountBase}x{Tools.ConvertDecimal(urnBallDamageCoefficient)}</stack> damage " +
                $"and <style=cIsUtility>slowing</style> enemies hit.");
        }

        private void VoidVanillaUrnBehavior(On.RoR2.Items.SiphonOnLowHealthItemBodyBehavior.orig_OnEnable orig, RoR2.Items.SiphonOnLowHealthItemBodyBehavior self)
        {
            self.DestroyAttachment();
            Destroy(self);
        }

        private void UrnOnJump(CharacterMotor motor, ref float verticalBonus)
        {
            CharacterBody body = motor.body;
            if (body.outOfDanger)
                return;

            int itemCount = 0;
            Inventory inv = body.inventory;
            if (inv)
            {
                itemCount = body.inventory.GetItemCount(RoR2Content.Items.SiphonOnLowHealth);
            }
            if(Util.CheckRoll((1 - Mathf.Pow(1 - urnBallChance, itemCount)) * 100, body.master))
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
                    if (RiskierRainContent.IsMissileArtifactEnabled())
                    {
                        float totalYaw = urnBallYawSpread * 2 / (urnBallCountBase + 1);
                        float totalSpread = (urnBallCountBase - 1) * urnBallYawSpread;
                        float halfSpread = totalSpread / 2;

                        for (int i = 0; i < urnBallCountBase; i++)
                        {
                            //float currentSpread = Mathf.FloorToInt(i - (urnBallCountBase - 1) / 2f) / (urnBallCountBase - 1) * totalSpread;
                            //float currentSpread = Mathf.Lerp(0, totalSpread, i / (urnBallCountBase - 1)) - halfSpread;
                            float currentSpread = (i / (urnBallCountBase - 1)) * totalSpread - halfSpread;
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
                CoreModules.Assets.miredUrnTarball, origin, Util.QuaternionSafeLookRotation(forward), 
                body.gameObject, body.damage * urnBallDamageCoefficient, 0f, 
                Util.CheckRoll(body.crit, body.master), DamageColorIndex.Default, null, -1f);
        }
        #endregion

        private void JumpReworkJumpCount(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int featherCountLoc = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Feather"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount)),
                x => x.MatchStloc(out featherCountLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpCount)),
                x => x.MatchLdloc(featherCountLoc)
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, CharacterBody, int>>((featherCount, self) =>
            {
                int jumpCount = 0;
                Inventory inv = self.inventory;
                if(inv != null)
                {
                    if (featherCount > 0)
                    {
                        jumpCount += featherJumpCount;
                    }
                    if(inv.GetItemCount(RoR2Content.Items.SiphonOnLowHealth) > 0)
                    {
                        jumpCount += urnJumpCount;
                    }
                    if(inv.GetItemCount(RoR2Content.Items.FallBoots) > 0)
                    {
                        jumpCount += fallBootsJumpCount;
                    }
                    jumpCount += JumpStatHook.InvokeStatHook(self);
                }

                return jumpCount;
            });
        }

        private void DoJumpEvent(On.EntityStates.GenericCharacterMain.orig_ApplyJumpVelocity orig, 
            CharacterMotor characterMotor, CharacterBody characterBody, float horizontalBonus, float verticalBonus, bool vault)
        {
            //OnJumpEvent?.Invoke(characterMotor, ref verticalBonus);
            JumpStatHook.InvokeJumpHook(characterMotor, ref verticalBonus);
            orig(characterMotor, characterBody, horizontalBonus, verticalBonus, vault);
        }
    }

    public class JumpStatHook
    {
        public delegate void JumpStatHandler(CharacterBody sender, ref int jumpCount);
        public static event JumpStatHandler JumpStatCoefficient;

        public static int InvokeStatHook(CharacterBody self)
        {
            int jumpCount = 0;
            JumpStatCoefficient?.Invoke(self, ref jumpCount);
            return jumpCount;
        }

        public delegate void OnJumpHandler(CharacterMotor sender, ref float verticalBonus);
        public static event OnJumpHandler OnJumpEvent;
        public static float InvokeJumpHook(CharacterMotor self, ref float verticalBonus)
        {
            OnJumpEvent?.Invoke(self, ref verticalBonus);
            return verticalBonus;
        }
    }
}
