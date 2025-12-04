using RainrotSharedUtils.Shelters;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils.Shelters
{
    public class MockShelterComponent : ShelterProviderBehavior
    {
        public float scaleMultiplier = 1;
        Vector3 baseScale;
        public GameObject areaIndicatorReference;
        public float startingRadius;
        public float endRadius = 10f;
        public float durationBeforeShrink = 1f;
        public float durationForMaxShrink = 15f;
        float age = 0;
        bool hasStartedShrink = false;

        void FixedUpdate()
        {
            if (age >= durationForMaxShrink)
                return;
            age += Time.fixedDeltaTime;
            if(age > durationBeforeShrink)
            {
                if (!hasStartedShrink)
                {
                    hasStartedShrink = true;
                    //if the starting scale is say [20,20,20] but the radius is 30, then baseScale will be [0.66,0.66,0.66], which will maintain consistent scaling
                    if(areaIndicatorReference)
                        baseScale = areaIndicatorReference.transform.localScale / startingRadius;
                }
                if (age >= durationForMaxShrink)
                {
                    SetSize(endRadius);
                    return;
                }
                float delta = (age - durationBeforeShrink) / durationForMaxShrink;
                float radius = Mathf.Lerp(startingRadius, endRadius, delta);
                SetSize(radius);
            }
        }

        private void SetSize(float radius)
        {
            this.fallbackRadius = radius;
            if (areaIndicatorReference == null)
                return;
            areaIndicatorReference.transform.localScale = radius * baseScale * scaleMultiplier;
        }
    }
}
