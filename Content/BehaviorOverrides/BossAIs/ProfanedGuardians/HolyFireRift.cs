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
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Rift");
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
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

            // Spawn a bunch of metaballs.
            for (int i = 0; i < 3; i++)
                FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(HolySineSpear.Commander.Center + 
                    Main.rand.NextVector2Circular(HolySineSpear.Commander.width * 0.5f, HolySineSpear.Commander.height * 0.5f), Main.rand.NextFloat(52f, 85f));

            // Do not die naturally, the commander will manually kill these.
            Projectile.timeLeft = 240;
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
    }
}
