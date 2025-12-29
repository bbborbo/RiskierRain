using FruityElites.Modules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace FruityElites.EliteReworks
{
    class BlazingReworks : EliteReworkBase<BlazingReworks>
    {
        [AutoConfig("Fire Trail Damage Per Second", "Scales with ambient level", 80f)]
        public static float fireTrailDPS = 80f; //1.5f
        [AutoConfig("Fire Trail Base Radius", "Vanilla is 3.0", 6f)]
        public static float fireTrailBaseRadius = 6f; //3f
        [AutoConfig("Fire Trail Lifetime", "Might not work, vanilla is 3.0", 100f)]
        public static float fireTrailLifetime = 100f; //3f
        public override string eliteName => "Blazing";

        public override void Hooks()
        {
            On.RoR2.CharacterBody.UpdateFireTrail += BlazingFireTrailChanges;
        }
        private void BlazingFireTrailChanges(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {
            orig(self);
            return;

            if (self.fireTrail)
            {
                self.fireTrail.radius = fireTrailBaseRadius * self.radius;
                self.fireTrail.damagePerSecond = (1 + 0.2f * self.level) * fireTrailDPS;
                //self.fireTrail.pointLifetime = fireTrailLifetime;
            }
        }
    }
}
