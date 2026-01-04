using System.Collections.Generic;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneFlameOrb : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FireDrawer;

        public NPC Owner => Main.npc.IndexInRange((int)Projectile.ai[1]) && Main.npc[(int)Projectile.ai[1]].active ? Main.npc[(int)Projectile.ai[1]] : null;

        public static int LaserCount => 5;

        public float TelegraphInterpolant => Utils.GetLerpValue(20f, LaserReleaseDelay, Time, true);

        public float Radius => Owner.Infernum().ExtraAI[0] * (1f - Owner.Infernum().ExtraAI[1]);

        public ref float Time => ref Projectile.ai[0];

        public const int OverloadBeamLifetime = 300;

        public const int LaserReleaseDelay = 125;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brimstone Flame Orb");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 164;
            Projectile.height = 164;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9000;
            Projectile.scale = 0.2f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Owner is null)
            {
                Projectile.Kill();
                return;
            }

            // Die after sufficiently shrunk.
            if (Owner.Infernum().ExtraAI[1] >= 1f)
            {
                Projectile.Kill();
                return;
            }

            // Release beams outward once ready.
            if (Time == LaserReleaseDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item163, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < LaserCount; i++)
                    {
                        Vector2 laserDirection = (TwoPi * i / LaserCount + 0.8f).ToRotationVector2();
                        Utilities.NewProjectileBetter(Projectile.Center, laserDirection, ModContent.ProjectileType<FlameOverloadBeam>(), SupremeCalamitasBehaviorOverride.FlameOverloadBeamDamage, 0f, -1, Owner.whoAmI);
                    }
                }
            }

            Time++;
        }

        public float OrbWidthFunction(float completionRatio) => SmoothStep(0f, Radius, Sin(Pi * completionRatio));

        public Color OrbColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.Yellow, Color.Red, Lerp(0.2f, 0.8f, Projectile.localAI[0] % 1f));
            if (CalamityGlobalNPC.SCal == CalamityGlobalNPC.SCalLament)
                c = Color.Lerp(c, Color.DeepSkyBlue, 0.65f);
            c = Color.Lerp(c, Color.White, completionRatio * 0.5f);
            c.A = 0;
            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null || !Owner.active)
                return false;

            // Draw telegraphs.
            if (TelegraphInterpolant is >= 0 and < 1)
            {
                float telegraphWidth = Lerp(1f, 6f, TelegraphInterpolant);
                for (int i = 0; i < LaserCount; i++)
                {
                    Vector2 laserDirection = (TwoPi * i / LaserCount + 0.8f).ToRotationVector2();
                    Vector2 start = Projectile.Center;
                    Vector2 end = Projectile.Center + laserDirection * 4200f;
                    Color telegraphColor = Color.Orange;
                    if (CalamityGlobalNPC.SCal == CalamityGlobalNPC.SCalLament)
                        telegraphColor = Color.Lerp(telegraphColor, Color.DeepSkyBlue, 0.65f);
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor * Pow(TelegraphInterpolant, 0.67f), telegraphWidth);
                }
            }
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            if (Owner is null || !Owner.active)
                return;

            FireDrawer ??= new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, InfernumEffectsRegistry.PrismaticRayVertexShader);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.05f);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            List<float> rotationPoints = [];
            List<Vector2> drawPoints = [];

            for (float offsetAngle = -PiOver2; offsetAngle <= PiOver2; offsetAngle += Pi / 30f)
            {
                Projectile.localAI[0] = Clamp((offsetAngle + PiOver2) / Pi, 0f, 1f);

                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + LumUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.02f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 8; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 7f));
                }

                FireDrawer.DrawPixelated(drawPoints, -Main.screenPosition, 39);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
