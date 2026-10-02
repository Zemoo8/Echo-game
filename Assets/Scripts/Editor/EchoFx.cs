using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Small builder for ECHO's code-made particle systems. Every effect is 2D: shapes emit in
    /// the XY plane, effects play once (no loop) unless <see cref="Looping"/> is called, and
    /// particles live in world space so they stay behind as the player moves on.
    /// </summary>
    internal sealed class EchoFx
    {
        public readonly ParticleSystem System;
        readonly ParticleSystemRenderer renderer;

        public EchoFx(string name, Transform parent, Material material, string sortingLayer = "VFX", int order = 0)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);

            System = go.AddComponent<ParticleSystem>();
            System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = System.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.startLifetime = 0.5f;
            main.maxParticles = 64;

            var emission = System.emission;
            emission.rateOverTime = 0f;

            var shape = System.shape;
            shape.enabled = false;

            renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        public Transform Transform => System.transform;

        public EchoFx Burst(int count, float time = 0f)
        {
            var emission = System.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
            var main = System.main;
            main.maxParticles = Mathf.Max(count, 1);
            return this;
        }

        public EchoFx Looping(float perSecond, int maxParticles)
        {
            var main = System.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.maxParticles = maxParticles;
            var emission = System.emission;
            emission.rateOverTime = perSecond;
            return this;
        }

        public EchoFx Life(float min, float max)
        {
            var main = System.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(min, max);
            main.duration = Mathf.Max(0.05f, max);
            return this;
        }

        public EchoFx Speed(float min, float max)
        {
            var main = System.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(min, max);
            return this;
        }

        public EchoFx Size(float min, float max)
        {
            var main = System.main;
            main.startSize = new ParticleSystem.MinMaxCurve(min, max);
            return this;
        }

        public EchoFx Colors(Color a, Color b)
        {
            var main = System.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            return this;
        }

        public EchoFx Gravity(float modifier)
        {
            var main = System.main;
            main.gravityModifier = modifier;
            return this;
        }

        /// <summary>Emits from a circle in the XY plane, spraying through <paramref name="arc"/> degrees starting at <paramref name="startAngle"/> (0 = +X, counter-clockwise).</summary>
        public EchoFx Arc(float radius, float arc, float startAngle = 0f, float flatten = 1f)
        {
            var shape = System.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(0.0001f, radius);
            shape.arc = arc;
            shape.rotation = new Vector3(0f, 0f, startAngle);
            shape.scale = new Vector3(1f, flatten, 1f);
            return this;
        }

        /// <summary>Emits from inside a rectangle in the XY plane (direction comes from <see cref="Velocity"/>).</summary>
        public EchoFx Box(Vector2 size)
        {
            var shape = System.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x, size.y, 0f);
            return this;
        }

        /// <summary>Random velocity in local space, so mirrored effects mirror their motion too.</summary>
        public EchoFx Velocity(Vector2 min, Vector2 max)
        {
            var velocity = System.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(min.x, max.x);
            velocity.y = new ParticleSystem.MinMaxCurve(min.y, max.y);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            return this;
        }

        public EchoFx Drag(float amount)
        {
            // Plain air drag: the speed limit itself is never reached.
            var limit = System.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1000f;
            limit.dampen = 0f;
            limit.drag = amount;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
            return this;
        }

        public EchoFx Shrink() => SizeCurve(AnimationCurve.Linear(0f, 1f, 1f, 0f));

        public EchoFx Grow(float from) => SizeCurve(new AnimationCurve(new Keyframe(0f, from, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f)));

        EchoFx SizeCurve(AnimationCurve curve)
        {
            var size = System.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
            return this;
        }

        /// <summary>Fades alpha to zero over each particle's life (after <paramref name="hold"/> of it at full).</summary>
        public EchoFx Fade(float hold = 0f)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, hold), new GradientAlphaKey(0f, 1f) });
            var color = System.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            return this;
        }

        /// <summary>Fades in, holds, fades out (ambient particles that should never pop).</summary>
        public EchoFx FadeInOut()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var color = System.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            return this;
        }

        /// <summary>Stretches particles along their motion (speed lines, sparks).</summary>
        public EchoFx Stretch(float length)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = length;
            renderer.velocityScale = 0f;
            return this;
        }
    }
}
