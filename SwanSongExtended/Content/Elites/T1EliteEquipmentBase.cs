using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.EliteModule;

namespace SwanSongExtended.Elites
{
    public abstract class T1EliteEquipmentBase<T> : T1EliteEquipmentBase where T : T1EliteEquipmentBase<T>
    {
        public static T instance { get; private set; }

        public T1EliteEquipmentBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting EquipmentBoilerplate/Equipment was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class T1EliteEquipmentBase : EliteEquipmentBase
    {
        public override float EliteHealthModifier => 3f;

        public override float EliteDamageModifier => 1.5f;
        public override EliteTiers EliteTier { get; set; } = EliteTiers.Tier1;

        public EliteDef HonorEliteDef;

        protected override CustomEliteDef GetCustomElite()
        {
            HonorEliteDef = ScriptableObject.Instantiate(EliteDef);
            HonorEliteDef.healthBoostCoefficient = EliteDef.healthBoostCoefficient / 2;

            CustomEliteDef customElite = base.GetCustomElite();
            customElite.honorEliteDef = HonorEliteDef;
            return customElite;
        }
    }
}
