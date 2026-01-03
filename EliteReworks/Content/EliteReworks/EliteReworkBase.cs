using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FruityElites.EliteReworks
{
    public abstract class EliteReworkBase<T> : EliteReworkBase where T : EliteReworkBase<T>
    {
        public static T instance { get; private set; }

        public EliteReworkBase()
        {
            if (instance != null) throw new InvalidOperationException(
                $"Singleton class \"{typeof(T).Name}\" inheriting {EliteReworksPlugin.modName} {typeof(EliteReworkBase).Name} was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class EliteReworkBase : SharedBase
    {
        public override string ConfigName => "Elite Reworks : " + eliteName;
        public override AssetBundle assetBundle => EliteReworksPlugin.mainAssetBundle;
        public override string TOKEN_PREFIX => "";
        public override string TOKEN_IDENTIFIER => "";
        public abstract string eliteName { get; }
        public override void Init()
        {
            base.Init();
        }
        public override void Hooks()
        {

        }
        public override void Lang()
        {

        }
    }
}
