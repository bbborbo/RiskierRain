using RainrotSharedUtils.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RainrotSharedUtils
{
    public static class Hooks
    {
        public static void DoHooks()
        {
            On.RoR2.IcicleAuraController.Awake += AuraControllerFix;
            On.RoR2.BuffWard.BuffTeam += ApplyDotWard;
        }

        private static void AuraControllerFix(On.RoR2.IcicleAuraController.orig_Awake orig, IcicleAuraController self)
        {
            orig(self);
            if(self.buffWard && self.buffWard is DotWard dotWard)
            {
                dotWard.ownerObject = self.cachedOwnerInfo.gameObject;
                dotWard.ownerBody = self.cachedOwnerInfo.characterBody;
            }
        }

        #region dot ward
        private static void ApplyDotWard(On.RoR2.BuffWard.orig_BuffTeam orig, RoR2.BuffWard self, IEnumerable<RoR2.TeamComponent> recipients, float radiusSqr, Vector3 currentPosition)
        {
            if (!(self is DotWard dotWard))
            {
                orig(self, recipients, radiusSqr, currentPosition);
                return;
            }

            if (!NetworkServer.active)
            {
                return;
            }
            if (dotWard.dotIndex == DotController.DotIndex.None)
            {
                return;
            }

            GameObject owner = dotWard.ownerObject;
            CharacterBody body = dotWard.ownerBody;
            Inventory inv = dotWard.ownerInventory;

            foreach (TeamComponent teamComponent in recipients)
            {
                Vector3 vector = teamComponent.transform.position - currentPosition;
                if (self.shape == BuffWard.BuffWardShape.VerticalTube)
                {
                    vector.y = 0f;
                }
                if (vector.sqrMagnitude <= radiusSqr)
                {
                    CharacterBody component = teamComponent.GetComponent<CharacterBody>();
                    if (component && (!self.requireGrounded || !component.characterMotor || component.characterMotor.isGrounded))
                    {
                        InflictDotInfo inflictDotInfo = new InflictDotInfo
                        {
                            attackerObject = owner,
                            victimObject = component.gameObject,
                            totalDamage = new float?(dotWard.damageCoefficient * body.damage),
                            damageMultiplier = 1f,
                            dotIndex = dotWard.dotIndex,
                            maxStacksFromAttacker = null
                        };

                        if (inv != null)
                            StrengthenBurnUtils.CheckDotForUpgrade(inv, ref inflictDotInfo);

                        DotController.InflictDot(ref inflictDotInfo);
                    }
                }
            }
        }
        #endregion
    }
}
