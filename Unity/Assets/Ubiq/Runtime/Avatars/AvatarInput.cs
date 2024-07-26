using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class AvatarInput
    {
        public interface IInput
        {
            /// <summary>
            /// Tiebreaker for inputs. Higher priority inputs of the type are
            /// returned first. If multiple inputs of the same type share the
            /// same priority, the input added later will be returned first.
            /// </summary>
            int priority { get; }
            /// <summary>
            /// Inputs which are not active will not be returned by the
            /// input system. 
            /// </summary>
            bool active { get; }
        }
        
        private Dictionary<Type, List<IInput>> inputsByType = new();
        
        /// <summary>
        /// Add an input of a given type. If the input is already present
        /// it will not be added again.
        /// </summary>
        /// <param name="input">The input to add.</param>
        public void Add<T>(T input) where T : IInput
        {
            if (!inputsByType.TryGetValue(typeof(T), out List<IInput> inputs))
            {
                inputs = new List<IInput>();
                inputsByType.Add(typeof(T),inputs);
            }
            
            if (!inputs.Contains(input))
            {
                inputs.Add(input);
            }
        }
        
        /// <summary>
        /// Remove an input. It is generally more efficient not to remove
        /// inputs if they are likely to be added again soon. Instead, set the
        /// active variable in IInput to false. This will let requests fall
        /// through to the next input, and helps to reduce memory allocations
        /// which can lead to garbage collection hitches.
        /// </summary>
        /// <param name="input">The input to remove.</param>
        public void Remove<T>(T input) where T : IInput
        {
            if (!inputsByType.TryGetValue(typeof(T),out List<IInput> inputs))
            {
                return;
            }
            
            var index = inputs.IndexOf(input);
            if (index < 0)
            {
                return;
            }
            
            inputs.RemoveAt(index);
            
            if (inputs.Count == 0)
            {
                inputsByType.Remove(typeof(T));    
            }
        }

        /// <summary>
        /// Attempt to find an input of a given type.
        /// </summary>
        /// <param name="input">The input, if found.</param>
        /// <returns>True if the input has been found.</returns>
        public bool TryGet<T>(out T input) where T : IInput
        {
            input = default;
            var found = false;

            if (inputsByType.TryGetValue(typeof(T),out List<IInput> inputs))
            {
                var priority = 0;
                for (var i = 0; i < inputs.Count; i++)
                {
                    if (!inputs[i].active)
                    {
                        continue;
                    }
                    
                    if (!found || inputs[i].priority >= priority)
                    {
                        input = (T)inputs[i];
                        priority = inputs[i].priority;
                        found = true;
                    }
                }
            }
            
            return found;
        }
    }
}