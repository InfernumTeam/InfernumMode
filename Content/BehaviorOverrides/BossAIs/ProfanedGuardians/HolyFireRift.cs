using CalamityMod.Particles.Metaballs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyFireRift : ModProjectile
    {
        #region Properties
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public bool SpearRift => Projectile.ai[0] == 1;

        public Vector2 RiftSize
        { 
            get;
            set;
        }

        public float BallSize
        {
            get;
            set;
        } = 85f;
        #endregion

        #region Overrides
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fire Rift");

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = CommanderSpearThrown.TelegraphTime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Die if the commander is non-existant.
            if (HolySineSpear.Commander is null)
            {
                Projectile.Kill();
                return;
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.45f);

            // Initialization.
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                if (SpearRift)
                {
                    RiftSize = new(HolySineSpear.Commander.width * 0.25f, HolySineSpear.Commander.height * 0.25f);
                    BallSize = 55f;
                }
                else
                    RiftSize = new(HolySineSpear.Commander.width * 0.5f, HolySineSpear.Commander.height * 0.5f);
            }

            if (!SpearRift)
                // Do not die naturally, the commander will manually kill these.
                Projectile.timeLeft = 240;

            // Spawn a bunch of metaballs.
            for (int i = 0; i < 3; i++)
                FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(Projectile.Center + 
                    Main.rand.NextVector2Circular(RiftSize.X, RiftSize.Y), Main.rand.NextFloat(BallSize * 0.75f, BallSize));
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            float scaleInterpolant = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, true) * (1f + 0.1f * 
                (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * (MathHelper.Pi * 2f) * 3f)) * 0.225f;

            Texture2D texture = InfernumTextureRegistry.Gleam.Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color baseColor = WayfinderSymbol.Colors[1];
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= scaleInterpolant;
            colorB *= scaleInterpolant;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new Vector2(0.5f, 2f) * Projectile.scale * scaleInterpolant;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float upRight = Projectile.rotation + MathHelper.PiOver4;
            float up = Projectile.rotation + MathHelper.PiOver2;
            float upLeft = Projectile.rotation + 3f * MathHelper.PiOver4;
            float left = Projectile.rotation + MathHelper.Pi;
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upLeft, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upRight, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upLeft, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upRight, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, up, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, left, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, up, origin, scale * 0.36f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, left, origin, scale * 0.36f, spriteEffects, 0);

            return false;
        }
        #endregion
    }
}
