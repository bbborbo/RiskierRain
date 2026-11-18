using EntityStates;
using RoR2;
using RoR2.Skills;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils.Components
{
    public class ElementCounter : MonoBehaviour
    {
        public static void RegisterSkillElement(SkillDef skillDef, CounterElement element)
        {
            if (SkillCatalog.skillsDefined.available)
            {
                skillIndexToElement.Add(skillDef.skillIndex, element);
                return;
            }
            if (skillToElementPreLoad == null)
                skillToElementPreLoad = new Dictionary<SkillDef, CounterElement>();

            if (skillToElementPreLoad.ContainsKey(skillDef))
                skillToElementPreLoad[skillDef] = element;
            else
                skillToElementPreLoad.Add(skillDef, element);
        }
        internal static Dictionary<SkillDef, CounterElement> skillToElementPreLoad { get; private set; }
        public static Dictionary<int, CounterElement> skillIndexToElement = new Dictionary<int, CounterElement>();
        /// <summary>
        /// key uses characterbody gameobject
        /// </summary>
        public static FixedConditionalWeakTable<GameObject, ElementCounter> instanceLookup = new FixedConditionalWeakTable<GameObject, ElementCounter>();
        public enum CounterElement
        {
            None = 0,
            Fire,
            Ice,
            Lightning,
            Earth, 
            Water,
            Wind,
            Robo,
            Plant
        }
        public enum Power
        {
            None = 0,
            Low,
            Medium,
            High,
            Extreme,
            Unfathomable
        }
        public static Dictionary<CounterElement, Power> localUserPowerMap = new Dictionary<CounterElement, Power>();
        public Dictionary<CounterElement, Power> powerMap = new Dictionary<CounterElement, Power>();
        public static Power GetPower(ref Dictionary<CounterElement, Power> map, CounterElement element)
        {
            if (map != null && map.ContainsKey(element))
            {
                return map[element];
            }
            return Power.None;
        }
        public static void SetPower(ref Dictionary<CounterElement, Power> map, CounterElement element, Power power)
        {
            if (map == null)
                return;

            if (map.ContainsKey(element))
                map[element] = power;
            else
                map.Add(element, power);
        }

        public CharacterBody body;
        public Power firePower 
        {
            get => GetPower(ref powerMap, CounterElement.Fire);
            set => SetPower(ref powerMap, CounterElement.Fire, value);
        }
        public Power icePower
        {
            get => GetPower(ref powerMap, CounterElement.Ice);
            set => SetPower(ref powerMap, CounterElement.Ice, value);
        }
        public Power lightningPower
        {
            get => GetPower(ref powerMap, CounterElement.Lightning);
            set => SetPower(ref powerMap, CounterElement.Lightning, value);
        }

        public static Power localUserFirePower { get; private set; } = Power.None;
        public static Power localUserIcePower { get; private set; } = Power.None;
        public static Power localUserLightningPower { get; private set; } = Power.None;

        SkillLocator loc = null;

        public void OnBodyStart(CharacterBody body)
        {
            this.body = body;
            if (body.skillLocator == null)
            {
                Debug.Log("Element Counter has no state assigned!");
                return;
            }
            this.loc = body.skillLocator;

            loc.primary.onSkillChanged += (s) => RecalculatePowers();
            loc.secondary.onSkillChanged += (s) => RecalculatePowers();
            loc.utility.onSkillChanged += (s) => RecalculatePowers();
            loc.special.onSkillChanged += (s) => RecalculatePowers();

            RecalculatePowers();
        }

        public void OnBodyEnd()
        {
            if (loc == null)
                return;

            loc.primary.onSkillChanged -= (s) => RecalculatePowers();
            loc.secondary.onSkillChanged -= (s) => RecalculatePowers();
            loc.utility.onSkillChanged -= (s) => RecalculatePowers();
            loc.special.onSkillChanged -= (s) => RecalculatePowers();
        }

        public void RecalculatePowers()
        {
            ResetPowers(ref powerMap);

            this.SetPowerFromSkill(loc.primary);
            this.SetPowerFromSkill(loc.secondary);
            this.SetPowerFromSkill(loc.utility);
            this.SetPowerFromSkill(loc.special);

            if (body && body.hasAuthority)
            {
                foreach(KeyValuePair<CounterElement, Power> kvp in powerMap)
                {
                    CounterElement elem = kvp.Key;
                    if (localUserPowerMap.ContainsKey(elem))
                        localUserPowerMap[elem] = kvp.Value;
                    else
                        localUserPowerMap.Add(elem, kvp.Value);
                }
            }

            string s = "";
            foreach (KeyValuePair<CounterElement, Power> kvp in powerMap)
            {
                s += $"{kvp.Key}: {kvp.Value}\n";
            }
            Debug.Log(s);
        }
        public static void ResetPowers(ref Dictionary<CounterElement, Power> map)
        {
            if (map == null || map.Count == 0)
                return;
            foreach (KeyValuePair<CounterElement, Power> kvp in map)
            {
                CounterElement elem = kvp.Key;
                map[elem] = Power.None;
            }
        }


        private void SetPowerFromSkill(GenericSkill skill)
        {
            int skillIndex = skill.baseSkill.skillIndex;
            if(skillIndexToElement.ContainsKey(skillIndex) == false)
            {
                skillIndexToElement.Add(skillIndex, DetectSkillElement(skill.baseSkill));
            }

            CounterElement elem = skillIndexToElement[skillIndex];
            if (elem != CounterElement.None)
            {
                Power p = GetPower(ref powerMap, elem);
                if (p < Power.Unfathomable)
                {
                    SetPower(ref powerMap, elem, p + 1);
                }
            }
        }

        private static CounterElement DetectSkillElement(SkillDef skill)
        {
            string token = skill.skillNameToken;

            if (token.Contains("_") == false)
                return CounterElement.None;

            string[] s = skill.skillNameToken.Split('_');
            String name = s.Length > 2 ? s[2].ToLower() : "";
            switch (name)
            {
                case "fire":
                    return CounterElement.Fire;
                case "ice":
                    return CounterElement.Ice;
                case "lightning":
                    return CounterElement.Lightning;
            }
            return CounterElement.None;
        }

        #region static methods
        public static Power GetPowerLevelFromBody(GameObject body, CounterElement element, ElementCounter powerComponent = null)
        {
            Power power = Power.None;

            if (body == null)
                return power;
            if (powerComponent == null)
            {
                powerComponent = GetPowerComponentFromBody(body);
            }

            if (powerComponent != null)
            {
                power = GetPower(ref powerComponent.powerMap, element);
            }

            return power;
        }

        /// <summary>
        /// Automatically called by GetPowerLevelFromBody of its powerComponent argument is null.
        /// </summary>
        public static ElementCounter GetPowerComponentFromBody(GameObject body)
        {
            ElementCounter powerComponent = null;
            if (instanceLookup.ContainsKey(body))
            {
                powerComponent = instanceLookup[body];
            }
            else
            {
                powerComponent = body.GetComponent<ElementCounter>();
                if (powerComponent != null)
                    instanceLookup[body] = powerComponent;
            }
            return powerComponent;
        }
        #endregion
    }
}