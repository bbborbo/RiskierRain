using RoR2;
using SwanSongExtended.Modules;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.EliteModule;

namespace SwanSongExtended.Elites
{
    public abstract class StormEliteEquipmentBase<T> : StormEliteEquipmentBase where T : StormEliteEquipmentBase<T>
    {
        public static T instance { get; private set; }

        public StormEliteEquipmentBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting EquipmentBoilerplate/Equipment was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class StormEliteEquipmentBase : EliteEquipmentBase
    {
        public override float EliteHealthModifier => 0f;

        public override float EliteDamageModifier => 0f;
        public override EliteTiers EliteTier { get; set; } = EliteTiers.Tier1;

        public EliteDef HonorEliteDef;

        protected override void CreateEliteEquipment()
        {
            base.CreateEliteEquipment();
        }
        public bool IsStormEliteEmpowered(CharacterBody body)
        {
            return body.HasBuff(this.EliteBuffDef) && !body.HasBuff(StormsCore.StormEliteWeak); 
        }
    }
}
