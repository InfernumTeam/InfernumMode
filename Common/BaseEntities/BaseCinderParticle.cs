using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.BaseEntities
{
    public abstract class BaseCinderParticle : Particle
    {
        internal bool HasInitialized;

        public virtual int MinLifetime => 120;

        public virtual int MaxLifetime => 195;

        public virtual float MinRandomScale => 0.8f;

        public virtual float MaxRandomScale => 1.2f;

        public virtual int NumberOfFrames => 3;

        public override int FrameVariants => NumberOfFrames;

        public override void Update()
        {
            // Decide a variant to use on the first frame this projectile exists.
            if (!HasInitialized)
            {
                Initialize();
                Lifetime = 1;
                Variant = Main.rand.Next(FrameVariants);
                HasInitialized = true;
            }

            // Make a decision for the lifetime for the cinder if one has not yet been made.
            if (Lifetime <= 1)
                Lifetime = Main.rand.Next(MinLifetime, MaxLifetime);

            // Calculate scale of the cinder.
            else
            {
                Scale = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - 20f, Time, true);
                Scale *= MathHelper.Lerp(MinRandomScale, MaxRandomScale, ID % 6f / 6f);
            }

            // Fly up and down.
            if (Math.Abs(Velocity.X) > 4f && ID % 2 == 1)
                Velocity.Y += (float)Math.Sin(MathHelper.TwoPi * Time / 42f) * 0.0667f;

            if (Time >= Lifetime)
                Kill();

            Time++;
        }
        
        /// <summary>
        /// Initialize fields in here. 
        /// Runs on the first frame the cinder is alive.
        /// </summary>
        public virtual void Initialize() { }
    }
}
