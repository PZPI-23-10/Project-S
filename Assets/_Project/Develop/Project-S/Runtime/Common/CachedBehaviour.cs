using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Common
{
    public class CachedBehaviour : MonoBehaviour
    {
        private readonly Dictionary<Type, Component> _components = new();

        public new T GetComponent<T>() where T : Component
        {
            if (!_components.TryGetValue(typeof(T), out var component))
            {
                component = base.GetComponent<T>();

                if (component != null)
                    _components.Add(typeof(T), component);
            }

            return (T)component;
        }

        public new T GetComponentInChildren<T>(bool includeInactive = false) where T : Component
        {
            if (!_components.TryGetValue(typeof(T), out var component))
            {
                component = base.GetComponentInChildren<T>(includeInactive);

                if (component != null)
                    _components.Add(typeof(T), component);
            }

            return (T)component;
        }
    }
}