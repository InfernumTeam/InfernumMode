using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ThermonuclearDeathOrb : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;

        public NPC Owner => Main.npc.IndexInRange((int)Projectile.ai[1]) && Main.npc[(int)Projectile.ai[1]].active ? Main.npc[(int)Projectile.ai[1]] : null;

        public float Radius => Owner.Infernum().ExtraAI[2];

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Thermonuclear Death Orb");
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

            // Drift towards the nearest target.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.velocity.Length() > 0.02f)
            {
                float flySpeed = Projectile.Distance(target.Center) * 0.0064f + 5.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * flySpeed, 0.05f);
                Projectile.velocity = Projectile.velocity.ClampMagnitude(1f, 26f);
            }

            // Periodically release bursts of plasma bolts.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 105f == 104f)
            {
                for (int i = 0; i < 24; i++)
                {
                    Vector2 plasmaVelocity = (TwoPi * i / 24f).ToRotationVector2() * 5f;
                    Vector2 plasmaSpawnPosition = Projectile.Center + plasmaVelocity.SafeNormalize(Vector2.UnitY) * Radius * 0.5f;
                    Utilities.NewProjectileBetter(plasmaSpawnPosition, plasmaVelocity, ModContent.ProjectileType<SmallPlasmaSpark>(), DraedonBehaviorOverride.StrongerNormalShotDamage, 0f);
                }
            }

            Time++;
        }

        public float OrbWidthFunction(float completionRatio) => SmoothStep(0f, Radius, Sin(Pi * completionRatio));

        public Color OrbColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.Orange, Color.ForestGreen, Lerp(0.2f, 0.8f, Projectile.localAI[0] % 1f));
            c = Color.Lerp(c, Color.White, completionRatio * 0.5f);
            c.A = 0;
            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null || !Owner.active)
                return false;

            FireDrawer ??= new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, InfernumEffectsRegistry.PrismaticRayVertexShader);

            InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.25f);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            List<Vector2> drawPoints = [];

            Main.spriteBatch.EnterShaderRegion();
            for (float offsetAngle = -PiOver2; offsetAngle <= PiOver2; offsetAngle += Pi / 30f)
            {
                Projectile.localAI[0] = Clamp((offsetAngle + PiOver2) / Pi, 0f, 1f);

                drawPoints.Clear();

                float adjustedAngle = offsetAngle + LumUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.02f, 3, 185) * 2f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 8; i++)
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 7f));

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override bool? CanDamage() => Projectile.velocity.Length() > 0.02f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.64f);
    }
}
