using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class DarkEnergyBolt : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy TrailDrawer;

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy Bolt");
            Main.projFrames[Type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
            // Die if the owner is not present or is dead.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.voidBoss))
            {
                Projectile.Kill();
                return;
            }

            NPC ceaselessVoid = Main.npc[CalamityGlobalNPC.voidBoss];

            float distanceToVoid = Projectile.Distance(ceaselessVoid.Center);
            Projectile.scale = Utils.GetLerpValue(0f, 240f, distanceToVoid, true);
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * 0.007f;

            if (distanceToVoid < 360f)
                Projectile.velocity = (Projectile.velocity * 29f + Projectile.SafeDirectionTo(ceaselessVoid.Center) * 14.5f) / 30f;

            if (distanceToVoid < Main.rand.NextFloat(64f, 90f))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
                Projectile.Kill();
            }

            Time++;
        }

        internal float WidthFunction(float completionRatio)
        {
            float arrowheadCutoff = 0.33f;
            float width = Projectile.width;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.GetLerpValue(0f, arrowheadCutoff, completionRatio, true));
            return width * Projectile.scale + 1f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color shaderColor1 = Color.Lerp(Color.Black, Color.Purple, 0.35f);
            Color shaderColor2 = Color.Lerp(Color.Black, Color.Cyan, 0.7f);

            float endFadeRatio = 0.9f;

            float endFadeTerm = Utils.GetLerpValue(0f, endFadeRatio * 0.5f, completionRatio, true) * 3.2f;
            float sinusoidalTime = completionRatio * 2.7f - Main.GlobalTimeWrappedHourly * 2.3f + endFadeTerm;
            float startingInterpolant = (float)Math.Cos(sinusoidalTime) * 0.5f + 0.5f;

            float colorLerpFactor = 0.6f;
            Color startingColor = Color.Lerp(shaderColor1, shaderColor2, startingInterpolant * colorLerpFactor) * Projectile.Opacity;
            return Color.Lerp(startingColor, Color.Transparent, MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spritebatch)
        {
            TrailDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.TwinsFlameTrailVertexShader);

            InfernumEffectsRegistry.TwinsFlameTrailVertexShader.UseImage1("Images/Misc/Perlin");
            TrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 40);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 17f, targetHitbox);
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.BlueCosmilite, 0f, 0f);
        }
    }
}
