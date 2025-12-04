using EntityStates;
using EntityStates.LaserTurbine;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using SwanSongExtended.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public static GameObject laserTurbineControllerPrefab;
        static float minSpin => Components.LaserTurbineController.minSpin;
        static float maxSpin => Components.LaserTurbineController.maxSpin;
        static float spinPerKill => Components.LaserTurbineController.spinGeneratedOnKill;
        static float spinDecayRate => Components.LaserTurbineController.spinDecayPerSecondAfterRefresh;
        public void DeworkResonanceDisc()
        {
            Addressables.LoadAssetAsync<GameObject>("bfba6e51566cdb5419002a0035f60af7").Completed += (ctx) => ReplaceLaserTurbineController(ctx.Result);
            On.EntityStates.LaserTurbine.LaserTurbineBaseState.OnEnter += TurbineState_OnEnter;
            On.EntityStates.EntityState.FixedUpdate += TurbineState_FixedUpdate;
            On.EntityStates.EntityState.OnExit += RechargeTurbine_OnExit;
            string damageDesc = "launches itself toward a target for <style=cIsDamage>300%</style> base damage <style=cStack>(+300% per stack)</style>, " +
                "piercing all enemies it doesn't kill, and then explodes for " +
                "<style=cIsDamage>1000%</style> base damage <style=cStack>(+1000% per stack)</style>. " +
                "Then returns to the user, striking all enemies along the way for " +
                "<style=cIsDamage>300%</style> base damage <style=cStack>(+300% per stack)</style>.";

            bool numberphileMode = true;
            string pickupDesc = "Obtain a Resonance Disc charged by killing enemies. Fires automatically when fully charged.";
            string fullDesc = "Killing enemies charges the Resonance Disc. The disc " + damageDesc;

            string numberphileDesc = $"Gain a Resonance Disc that spins " +
                $"{DamageColor(ConvertDecimal(spinPerKill / minSpin) + " faster")} after killing enemies, " +
                $"up to {DamageColor(((maxSpin / minSpin) - 1).ToString())} times. " +
                $"While spinning, the Resonance Disc continuously " +
                $"slows down to a minimum of {DamageColor(ConvertDecimal(minSpin * 10) + " Spin")} " +
                $"at a rate of {DamageColor($"-{ConvertDecimal(spinDecayRate)} current Spin per second per second")}, " +
                $"converting {DamageColor($"{ConvertDecimal(spinDecayRate / minSpin)} of lost Spin")} into {UtilityColor("Charge")}. " +
                Environment.NewLine +
                $"When the Resonance Disc reaches {UtilityColor("100% Charge")}, it consumes all {UtilityColor("Charge")}. " +
                $"The disc then " + damageDesc;
            LanguageAPI.Add("ITEM_LASERTURBINE_PICKUP", numberphileMode ? numberphileDesc : pickupDesc);
            LanguageAPI.Add("ITEM_LASERTURBINE_DESC", numberphileMode ? numberphileDesc : fullDesc);


            #region slop
            Hook q = new Hook(
              typeof(RoR2.LaserTurbineController).GetMethod(nameof(RoR2.LaserTurbineController.Awake), (BindingFlags)(-1)),
              typeof(SwanSongPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            Hook w = new Hook(
              typeof(RoR2.LaserTurbineController).GetMethod(nameof(RoR2.LaserTurbineController.Update), (BindingFlags)(-1)),
              typeof(SwanSongPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            Hook e = new Hook(
              typeof(RoR2.LaserTurbineController).GetMethod(nameof(RoR2.LaserTurbineController.FixedUpdate), (BindingFlags)(-1)),
              typeof(SwanSongPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            Hook r = new Hook(
              typeof(RoR2.LaserTurbineController).GetMethod(nameof(RoR2.LaserTurbineController.OnEnable), (BindingFlags)(-1)),
              typeof(SwanSongPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            Hook t = new Hook(
              typeof(RoR2.LaserTurbineController).GetMethod(nameof(RoR2.LaserTurbineController.OnDisable), (BindingFlags)(-1)),
              typeof(SwanSongPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            Hook y = new Hook(
              typeof(RoR2.LaserTurbineController).GetMethod(nameof(RoR2.LaserTurbineController.ExpendCharge), (BindingFlags)(-1)),
              typeof(SwanSongPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            #endregion
        }

        public delegate void orig_idc(RoR2.LaserTurbineController self);
        static void ReflectOnThatThang(orig_idc orig, RoR2.LaserTurbineController self) { }

        private readonly Dictionary<EntityState, ResDiscContext> resDiscStateContext = new Dictionary<EntityState, ResDiscContext>();
        private void TurbineState_OnEnter(On.EntityStates.LaserTurbine.LaserTurbineBaseState.orig_OnEnter orig, LaserTurbineBaseState self)
        {
            if (self is FireMainBeamState || self is RechargeState)
            {
                GameObject obj = self.outer.gameObject;
                SwanSongExtended.Components.LaserTurbineController turbineController
                    = obj.GetComponent<SwanSongExtended.Components.LaserTurbineController>();
                ResDiscContext context = new ResDiscContext(turbineController);
                this.resDiscStateContext[self] = context;

                if (self is FireMainBeamState)
                {
                    if (NetworkServer.active)
                    {
                        context.laserTurbineController.ExpendCharge();
                    }
                    context.laserTurbineController.showTurbineDisplay = false;
                }
            }
            orig(self);
        }

        private void TurbineState_FixedUpdate(On.EntityStates.EntityState.orig_FixedUpdate orig, EntityState self)
        {
            if(resDiscStateContext.ContainsKey(self))
            {
                ResDiscContext context = resDiscStateContext[self];
                if (self is RechargeState)
                {
                    if (self.isAuthority && context.laserTurbineController.charge >= 1f)
                    {
                        self.outer.SetNextState(new ReadyState());
                    }
                    return;
                }
            }
            orig(self);
        }

        private void RechargeTurbine_OnExit(On.EntityStates.EntityState.orig_OnExit orig, EntityState self)
        {
            if(resDiscStateContext.ContainsKey(self))
            {
                ResDiscContext context = resDiscStateContext[self];

                if (self is FireMainBeamState)
                {
                    context.laserTurbineController.showTurbineDisplay = true;
                }
                resDiscStateContext.Remove(self);
            }
            orig(self);
        }

        private void ReplaceLaserTurbineController(GameObject result)
        {
            laserTurbineControllerPrefab = result;

            RoR2.LaserTurbineController oldController = result.GetComponent<RoR2.LaserTurbineController>();
            if (oldController)
            {
                Components.LaserTurbineController turbineController = result.AddComponent<SwanSongExtended.Components.LaserTurbineController>();
                turbineController.chargeIndicator = oldController.chargeIndicator;
                turbineController.spinIndicator = oldController.spinIndicator;
                turbineController.turbineDisplayRoot = oldController.turbineDisplayRoot;
                turbineController.chargeIndicator = oldController.chargeIndicator;

                //UnityEngine.Object.Destroy(oldController);
            }
        }
        private class ResDiscContext
        {
            public Components.LaserTurbineController laserTurbineController;

            public ResDiscContext(Components.LaserTurbineController passive)
            {
                this.laserTurbineController = passive;
            }
        }
    }
}
