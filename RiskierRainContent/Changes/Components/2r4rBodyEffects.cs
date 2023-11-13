using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Changes.Components
{
    public class _2r4rBodyEffects : MonoBehaviour
    {
        public CharacterBody body;
        private TemporaryVisualEffect happiestMaskHauntEffect;


        void Update()
        {
            UpdateSingleTemporaryVisualEffect(ref happiestMaskHauntEffect,
                Assets.hauntEffectPrefab, body.radius,
                body.HasBuff(Assets.hauntDebuff), "");
        }

        private void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, GameObject obj, float effectRadius, bool active, string childLocatorOverride = "")
        {
            if (active && obj != null)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(obj, body.corePosition, Quaternion.identity);
                tempEffect = gameObject.GetComponent<TemporaryVisualEffect>();
                tempEffect.parentTransform = body.coreTransform;
                tempEffect.visualState = TemporaryVisualEffect.VisualState.Enter;
                tempEffect.healthComponent = body.healthComponent;
                tempEffect.radius = effectRadius;
                LocalCameraEffect component = gameObject.GetComponent<LocalCameraEffect>();
                if (component)
                {
                    component.targetCharacter = base.gameObject;
                }
                if (!string.IsNullOrEmpty(childLocatorOverride))
                {
                    ModelLocator modelLocator = body.modelLocator;
                    ChildLocator childLocator;
                    if (modelLocator == null)
                    {
                        childLocator = null;
                    }
                    else
                    {
                        Transform modelTransform = modelLocator.modelTransform;
                        childLocator = ((modelTransform != null) ? modelTransform.GetComponent<ChildLocator>() : null);
                    }
                    ChildLocator childLocator2 = childLocator;
                    if (childLocator2)
                    {
                        Transform transform = childLocator2.FindChild(childLocatorOverride);
                        if (transform)
                        {
                            tempEffect.parentTransform = transform;
                            return;
                        }
                    }
                }
            }
            else if (tempEffect)
            {
                tempEffect.visualState = TemporaryVisualEffect.VisualState.Exit;
            }
        }

        private void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, GameObject obj, float effectRadius, int count, string childLocatorOverride = "")
        {
            UpdateSingleTemporaryVisualEffect(ref tempEffect, obj, effectRadius, count > 0, childLocatorOverride);
        }
    }
}
