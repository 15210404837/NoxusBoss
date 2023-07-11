﻿using System.Collections.Generic;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class NoxusPlayer : ModPlayer
    {
        private readonly Dictionary<string, object> localValues = new();

        public delegate void ResetEffectsDelegate(NoxusPlayer p);

        public static event ResetEffectsDelegate ResetEffectsEvent;

        private void VerifyValueExists<T>(string key) where T : struct
        {
            if (!localValues.TryGetValue(key, out object value) || value is not T)
                localValues[key] = default(T);
        }

        public T GetValue<T>(string key) where T : struct
        {
            VerifyValueExists<T>(key);
            return (T)localValues[key];
        }

        public void SetValue<T>(string key, T value) where T : struct
        {
            VerifyValueExists<T>(key);
            localValues[key] = value;
        }

        public override void ResetEffects()
        {
            ResetEffectsEvent?.Invoke(this);
        }
    }
}