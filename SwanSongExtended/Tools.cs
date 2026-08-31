using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using UnityEngine.Events;
using SwanSongExtended.Elites;

namespace SwanSongExtended
{
    public static class Extensions
    {
        /// <summary>
        /// if true, this interactable can trigger fireworks, squolyps, and your custom on-interaction effect
        /// </summary>
        /// <param name="disallowPickups">set to false to allow non-duplicated items to trigger your thing</param>
        /// <returns></returns>
        public static bool InteractableIsPermittedForSpawn(this GameObject interactableObject, bool disallowPickups = true)
        {
            if (interactableObject == null)
                return false;

            if (interactableObject.TryGetComponent(out InteractionProcFilter interactionProcFilter))
            {
                return interactionProcFilter.shouldAllowOnInteractionBeginProc;
            }
            if (interactableObject.TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                return purchaseInteraction.disableSpawnOnInteraction == false;
            }
            if (interactableObject.TryGetComponent(out DelusionChestController delusionController))
            {
                if (interactableObject.TryGetComponent(out PickupPickerController pickupPickerController) && pickupPickerController.enabled)
                {
                    return false;
                }
                return true;
            }
            if ((bool)interactableObject.TryGetComponent(out GenericPickupController pickupController))
            {
                return disallowPickups || pickupController.Duplicated;
            }
            if ((bool)interactableObject.GetComponent<VehicleSeat>())
            {
                return false;
            }
            if ((bool)interactableObject.GetComponent<NetworkUIPromptController>())
            {
                return false;
            }
            if (interactableObject.TryGetComponent(out PowerPedestal powerPedestal))
            {
                return powerPedestal.CanTriggerFireworks;
            }
            if (interactableObject.TryGetComponent(out AccessCodesNodeController accessNodeController))
            {
                return accessNodeController.CheckInteractionOrder();
            }
            return true;
        }
        public static GameObject FixItemModel(this GameObject prefab)
        {
            if (prefab == null)
                prefab = Resources.Load<GameObject>("prefabs/NullModel");

            ModelPanelParameters parameters = prefab.AddComponent<ModelPanelParameters>();

            parameters.minDistance = 1;
            parameters.maxDistance = 15;

            Transform t = prefab.transform.Find("FocusPos");
            if (t == null)
            {
                GameObject focusPoint = new GameObject("FocusPos");
                t = focusPoint.transform;
                t.parent = prefab.transform;
                t.localPosition = Vector3.zero;
            }
            parameters.focusPointTransform = t;

            Transform c = prefab.transform.Find("CameraPos");
            if (c == null)
            {
                GameObject cameraPos = new GameObject("CameraPos");
                c = cameraPos.transform;
                c.parent = prefab.transform;
                c.SetPositionAndRotation(t.position + Vector3.forward * -7 + Vector3.right * -1, c.rotation);
            }
            parameters.cameraPositionTransform = c;

            return prefab;
        }
        /// <summary>
        /// go my hardcoded bullshit!!
        /// </summary>
        public static bool IsStormElite(this CharacterBody body)
        {
            if (SurgingAspect.instance != null && body.HasBuff(SurgingAspect.instance.EliteBuffDef))
                return true;
            if (WhirlwindAspect.instance != null && body.HasBuff(WhirlwindAspect.instance.EliteBuffDef))
                return true;
            if (DeuteriumAspect.instance != null && body.HasBuff(DeuteriumAspect.instance.EliteBuffDef))
                return true;
            return false;
        }
        public static string AsPercent(this float d)
        {
            return (d * 100f).ToString() + "%";
        }
        public static void AddPersistentListener(this CombatDirector.OnSpawnedServer unityEvent, UnityAction<GameObject> action)
        {
            unityEvent.m_PersistentCalls.AddListener(new PersistentCall
            {
                m_Target = action.Target as UnityEngine.Object,
                m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
                m_MethodName = action.Method.Name,
                m_CallState = UnityEventCallState.RuntimeOnly,
                m_Mode = PersistentListenerMode.EventDefined,
            });
        }
        public static void AddPersistentListener(this HoldoutZoneController.HoldoutZoneControllerChargedUnityEvent unityEvent, UnityAction<HoldoutZoneController> action)
        {
            unityEvent.m_PersistentCalls.AddListener(new PersistentCall
            {
                m_Target = action.Target as UnityEngine.Object,
                m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
                m_MethodName = action.Method.Name,
                m_CallState = UnityEventCallState.RuntimeOnly,
                m_Mode = PersistentListenerMode.EventDefined,
            });
        }
        public static void AddPersistentListener(this UnityEvent<Interactor> unityEvent, UnityAction<Interactor> action)
        {
            unityEvent.m_PersistentCalls.AddListener(new PersistentCall
            {
                m_Target = action.Target as UnityEngine.Object,
                m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
                m_MethodName = action.Method.Name,
                m_CallState = UnityEventCallState.RuntimeOnly,
                m_Mode = PersistentListenerMode.EventDefined,
            });
        }
        public static void AddPersistentListener(this UnityEvent<CharacterMaster> unityEvent, UnityAction<CharacterMaster> action)
        {
            unityEvent.m_PersistentCalls.AddListener(new PersistentCall
            {
                m_Target = action.Target as UnityEngine.Object,
                m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
                m_MethodName = action.Method.Name,
                m_CallState = UnityEventCallState.RuntimeOnly,
                m_Mode = PersistentListenerMode.EventDefined,
            });
        }
        public static void AddPersistentListener(this UnityEvent<GameObject> unityEvent, UnityAction<GameObject> action)
        {
            unityEvent.m_PersistentCalls.AddListener(new PersistentCall
            {
                m_Target = action.Target as UnityEngine.Object,
                m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
                m_MethodName = action.Method.Name,
                m_CallState = UnityEventCallState.RuntimeOnly,
                m_Mode = PersistentListenerMode.EventDefined,
            });
        }
    }
    public static class Tools
    {
        #region AssetBundle
        /// <summary>
        /// Loads an embedded asset bundle
        /// </summary>
        /// <param name="resourceBytes">The bytes returned by Properties.Resources.ASSETNAME</param>
        /// <returns>The loaded bundle</returns>
        public static AssetBundle LoadAssetBundle(Byte[] resourceBytes)
        {
            if (resourceBytes == null) throw new ArgumentNullException(nameof(resourceBytes));
            return AssetBundle.LoadFromMemory(resourceBytes);
        }

        /// <summary>
        /// A simple helper to generate a unique mod prefix for you.
        /// </summary>
        /// <param name="plugin">A reference to your plugin. (this.GetModPrefix)</param>
        /// <param name="bundleName">A unique name for the bundle (Unique within your mod)</param>
        /// <returns>The generated prefix</returns>
        public static string modPrefix = String.Format("@{0}+{1}", "ArtificerExtended", "artiskillicons");

        public static String GetModPrefix(this BepInEx.BaseUnityPlugin plugin, String bundleName)
        {
            return String.Format("@{0}+{1}", plugin.Info.Metadata.Name, bundleName);
        }
        #endregion

        internal static bool isLoaded(string modguid)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos)
            {
                string key = keyValuePair.Key;
                PluginInfo value = keyValuePair.Value;
                bool flag = key == modguid;
                if (flag)
                {
                    return true;
                }
            }
            return false;
        }
        internal static string ConvertDecimal(float d)
        {
            return (d * 100f).ToString() + "%";
        }
        public static float GetAmbientLevelScalar(float scalar)
        {
            if (!Run.instance)
                return 1;
            return 1 + scalar * Run.instance.ambientLevel;
        }
        public static bool IsEasyMode(DifficultyIndex minDifficultyIndex = DifficultyIndex.Easy)
        {
            if (Run.instance == null)
                return true;
            return Run.instance.selectedDifficulty <= minDifficultyIndex;
        }

        #region Materials + Etc
        internal static void GetMaterial(GameObject model, string childObject, Color color, ref Material material, float scaleMultiplier = 1, bool replaceAll = false)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Renderer smr = renderer;

                if (string.Equals(renderer.name, childObject))
                {
                    if (color == Color.clear)
                    {
                        UnityEngine.GameObject.Destroy(renderer);
                        return;
                    }

                    if (material == null)
                    {
                        material = new Material(renderer.material);
                        material.mainTexture = renderer.material.mainTexture;
                        material.shader = renderer.material.shader;
                        material.color = color;
                    }
                    renderer.material = material;
                    renderer.transform.localScale *= scaleMultiplier;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugMaterial(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Renderer smr = renderer;
                Debug.Log("Material: " + smr.name.ToString());
            }
        }

        internal static void GetParticle(GameObject model, string childObject, Color color, float sizeMultiplier = 1, bool replaceAll = false)
        {
            ParticleSystem[] partSystems = model.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                var main = ps.main;
                var lifetime = ps.colorOverLifetime;
                var speed = ps.colorBySpeed;

                if (string.Equals(ps.name, childObject))
                {
                    main.startColor = color;
                    main.startSizeMultiplier *= sizeMultiplier;
                    lifetime.color = color;
                    speed.color = color;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugParticleSystem(GameObject model)
        {
            ParticleSystem[] partSystems = model.GetComponents<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                Debug.Log("Particle: " + ps.name.ToString());
            }
        }


        internal static void GetLight(GameObject model, string childObject, Color color, bool replaceAll = false)
        {
            Light[] lights = model.GetComponentsInChildren<Light>();

            foreach (Light li in lights)
            {
                Light l = li;
                if (string.Equals(l.name, childObject))
                {
                    l.color = color;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugLight(GameObject model)
        {
            Light[] lights = model.GetComponentsInChildren<Light>();

            foreach (Light li in lights)
            {
                Light l = li;
                Debug.Log("Light: " + l.name.ToString());
            }
        }
        #endregion

        #region Buffs?
        public static void ClearDotStacksForType(this DotController dotController, DotController.DotIndex dotIndex)
        {
            for (int i = dotController.dotStackList.Count - 1; i >= 0; i--)
            {
                if (dotController.dotStackList[i].dotIndex == dotIndex)
                {
                    dotController.RemoveDotStackAtServer(i);
                }
            }
        }


        #endregion
        public static void ApplyCooldownScale(GenericSkill skillSlot, float cooldownScale)
        {
            if (skillSlot != null)
                skillSlot.cooldownScale *= cooldownScale;
        }
        /// <summary>
        /// i have no idea what kind of series this is but its like
        /// 1x = up to 100%,
        /// 2x = up to 300%,
        /// 3x = up to 600%,
        /// 4x = up to 1000%,
        /// 5x = up to 1500%,
        /// 6x = up to 2100%,
        /// etc
        /// </summary>
        public static int CountOverspillTriangular(float totalValue, float incrementor = 1f)
        {
            int count = 0;
            while (totalValue > 0)
            {
                count++;
                totalValue -= incrementor * count;
            }
            return count;
        }
        /// <summary>
        /// with default values:
        /// 1x = up to 200% totalValue,
        /// 2x = up to 300%,
        /// 3x = up to 500%,
        /// 4x = up to 800%,
        /// 5x = up to 1300%,
        /// 6x = up to 2100%,
        /// etc
        /// </summary>
        public static int CountOverspillFibonacci(float totalValue, float thresholdScale = 1f, int startingIndex = 1)
        {
            int lastIncrementor = 1;
            int currentIncrementor = 1;
            int count = 0;
            while (totalValue > currentIncrementor * thresholdScale)
            {
                currentIncrementor += lastIncrementor;
                lastIncrementor = currentIncrementor - lastIncrementor;

                if (count <= startingIndex)
                    continue;
                count++;
            }
            return count;
        }
    }
}
