using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// One of the emitter's pooled spheres (the emitter plan, EM-2): a Rigidbody with a sphere
    /// collider, its size and when it was launched, and its collision callback, which tells the
    /// emitter what it struck. Made and reused by Emitter; never destroyed while the emitter lives.
    /// </summary>
    public class EmitterSphere : MonoBehaviour
    {
        private Emitter _emitter;

        /// <summary>The sphere's body.</summary>
        public Rigidbody Body { get; private set; }

        /// <summary>The sphere's diameter, in units.</summary>
        public float Size { get; private set; }

        /// <summary>When it was launched, in Unity's seconds.</summary>
        public double Born { get; private set; }

        /// <summary>Gives the sphere its emitter and its body, as the emitter makes it.</summary>
        public void Begin(Emitter emitter, Rigidbody body)
        {
            _emitter = emitter;
            Body = body;
        }

        /// <summary>Marks the sphere launched: its size and the time.</summary>
        public void Launch(float size, double time)
        {
            Size = size;
            Born = time;
        }

        //----------

        private void OnCollisionEnter(Collision collision)
        {
            if (_emitter != null)
            {
                _emitter.OnSphereHit(this, collision);
            }
        }
    }
}
