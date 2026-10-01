using System;
using System.Collections;
using System.Collections.Generic;
using Echo.Core;
using Echo.Player;
using UnityEngine;

namespace Echo.Tests
{
    /// <summary>
    /// Builds a player with the same physics setup as the Player prefab, plus level blocks,
    /// so movement can be tested with real physics but without input devices.
    /// </summary>
    sealed class PlayerRig : IDisposable
    {
        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        public GameObject Root { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public PlayerDash Dash { get; private set; }
        public EnergySystem Energy { get; private set; }
        public PlayerTuning Tuning { get; private set; }

        public float FeetY => Root.GetComponent<BoxCollider2D>().bounds.min.y;

        public static PlayerRig Create(Vector2 position)
        {
            var rig = new PlayerRig();
            rig.Root = new GameObject("TestPlayer") { layer = LayerMask.NameToLayer(Layers.Player) };
            rig.Root.transform.position = position;
            rig.created.Add(rig.Root);

            rig.Body = rig.Root.AddComponent<Rigidbody2D>();
            var collider = rig.Root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 1.3f);
            collider.edgeRadius = 0.05f;
            var material = new PhysicsMaterial2D
            {
                friction = 0f,
                frictionCombine = PhysicsMaterialCombine2D.Minimum,
            };
            collider.sharedMaterial = material;
            rig.created.Add(material);

            rig.Energy = rig.Root.AddComponent<EnergySystem>();
            rig.Movement = rig.Root.AddComponent<PlayerMovement>();
            rig.Tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            rig.created.Add(rig.Tuning);
            rig.Movement.Tuning = rig.Tuning;
            rig.Dash = rig.Root.AddComponent<PlayerDash>();
            return rig;
        }

        public GameObject Block(float xMin, float yMin, float xMax, float yMax, string layer = Layers.Ground)
        {
            var block = new GameObject("TestBlock") { layer = LayerMask.NameToLayer(layer) };
            block.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            block.AddComponent<BoxCollider2D>().size = new Vector2(xMax - xMin, yMax - yMin);
            created.Add(block);
            return block;
        }

        public T Track<T>(T obj) where T : UnityEngine.Object
        {
            created.Add(obj);
            return obj;
        }

        public void Dispose()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }
            created.Clear();
        }

        /// <summary>Steps physics until the condition holds or the timeout passes.</summary>
        public static IEnumerator Until(Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return new WaitForFixedUpdate();
        }

        public static IEnumerator Seconds(float duration)
        {
            float end = Time.time + duration;
            while (Time.time < end)
                yield return new WaitForFixedUpdate();
        }
    }
}
