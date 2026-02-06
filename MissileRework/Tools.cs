using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using UnityEngine.Events;

namespace MissileRework
{
    public static class Extensions
    {
        public static string AsPercent(this float d)
        {
            return (d * 100f).ToString() + "%";
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
    }
}
