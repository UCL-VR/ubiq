using Ubiq.Avatars;
using UnityEngine;

namespace Ubiq
{
    /// <summary>
    /// Simple input of a single pose. This is useful for tutorials or toy
    /// examples, but as AvatarInput does not make it possible to expose
    /// multiple inputs of the same type, it is not expected that you use
    /// this to compose more complicated input setups. Create a new input
    /// interface customised for your needs instead.
    /// </summary>
    public interface IPoseInput : AvatarInput.IInput
    {
        /// <summary>
        /// Pose in world space.
        /// </summary>
        InputVar<Pose> pose { get; } 
    }
}