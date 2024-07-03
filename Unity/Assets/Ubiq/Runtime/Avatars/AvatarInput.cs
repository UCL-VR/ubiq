using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class AvatarInput
    {
        public interface IProvider
        {
            /// <summary>
            /// Tiebreaker for providers. Higher priority providers are returned
            /// first. If multiple providers share the same priority, the
            /// provider added later will be returned first.
            /// </summary>
            int priority { get; }
            /// <summary>
            /// Providers which are not enabled will not be returned by the
            /// input system. 
            /// </summary>
            bool isProviding { get; }
        }
        
        private Dictionary<Type, List<IProvider>> providersByType = new();
        
        /// <summary>
        /// Add a provider of a given type. If the provider is already present
        /// it will not be added again.
        /// </summary>
        /// <param name="provider">The provider to add.</param>
        public void AddProvider<T>(T provider) where T : IProvider
        {
            if (!providersByType.TryGetValue(typeof(T),
                    out List<IProvider> providers))
            {
                providers = new List<IProvider>();
                providersByType.Add(typeof(T),providers);
            }
            
            if (!providers.Contains(provider))
            {
                providers.Add(provider);
            }
        }
        
        /// <summary>
        /// Remove a hint provider for a node. It is generally more efficient
        /// not to remove hint providers if they are likely to be added again
        /// soon. Instead, set the isProviding variable in IProvider to false.
        /// This will let provider requests fall through to the next provider,
        /// and helps to reduce memory allocations which can lead to garbage
        /// collection hitches. 
        /// </summary>
        /// <param name="provider">The provider to remove.</param>
        public void RemoveProvider<T>(T provider) where T : IProvider
        {
            if (!providersByType.TryGetValue(typeof(T),out List<IProvider> providers))
            {
                return;
            }
            
            var index = providers.IndexOf(provider);
            if (index < 0)
            {
                return;
            }
            
            providers.RemoveAt(index);
            
            if (providers.Count == 0)
            {
                providersByType.Remove(typeof(T));    
            }
        }

        /// <summary>
        /// Attempt to find a hint provider of a given type.
        /// </summary>
        /// <param name="provider">The provider, if found.</param>
        /// <returns>True if the provider has been found.</returns>
        public bool TryGet<T>(out T provider) where T : IProvider
        {
            provider = default;
            var found = false;

            if (providersByType.TryGetValue(typeof(T),out List<IProvider> providers))
            {
                var priority = 0;
                for (var i = 0; i < providers.Count; i++)
                {
                    if (!providers[i].isProviding)
                    {
                        continue;
                    }
                    
                    if (!found || providers[i].priority >= priority)
                    {
                        provider = (T)providers[i];
                        priority = providers[i].priority;
                        found = true;
                    }
                }
            }
            
            return found;
        }
    }
}