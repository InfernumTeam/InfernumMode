using CalamityMod.Projectiles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class YharonBoom : ModProjectile
    {
        public float Radius
        {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }
        public float MaxRadius
        {
            get => projectile.ai[1];
            set => projectile.ai[1] = value;
        }
        public bool ShouldDeleteProjectiles => projectile.localAI[1] != 0f;
        public const int Lifetime = 120;

        public static readonly int[] YharonProjectiles = new int[]
        {
            ProjectileID.CultistBossFireBall, // Not technically a Yharon projectile, but it is used within the fight.
            ModContent.ProjectileType<Flarenado>(),
            ModContent.ProjectileType<Infernado>(),
            ModContent.ProjectileType<Infernado2>(),
            ModContent.ProjectileType<Flare>(),
            ModContent.ProjectileType<BigFlare>(),
            ModContent.ProjectileType<BigFlare2>(),
            ModContent.ProjectileType<FlareBomb>(),
            ModContent.ProjectileType<FlareDust>(),
            ModContent.ProjectileType<FlareDust2>()
        };

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Boom");
        }

        public override void SetDefaults()
        {
            projectile.width = 72;
            projectile.height = 72;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = Lifetime;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
            projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                MaxRadius = Main.rand.NextFloat(2000f, 4000f);
                projectile.localAI[0] = 1f;
            }
            Main.LocalPlayer.Infernum().CurrentScreenShakePower = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / Lifetime) * 10f;

            Lighting.AddLight(projectile.Center, 0.2f, 0.1f, 0f);
            Radius = MathHelper.Lerp(Radius, MaxRadius, 0.15f);
            projectile.scale = MathHelper.Lerp(1.2f, 5f, Utils.InverseLerp(Lifetime, 0f, projectile.timeLeft, true));
            CalamityGlobalProjectile.ExpandHitboxBy(projectile, (int)(Radius * projectile.scale), (int)(Radius * projectile.scale));

            if (ShouldDeleteProjectiles)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active &&
                        YharonProjectiles.ToList().Contains(Main.projectile[i].type))
                    {
                        Main.projectile[i].Kill();
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            float pulseCompletionRatio = Utils.InverseLerp(Lifetime, 0f, projectile.timeLeft, true);
            Vector2 scale = new Vector2(1.5f, 1f);
            DrawData drawData = new DrawData(ModContent.GetTexture("Terraria/Misc/Perlin"),
                projectile.Center - Main.screenPosition + projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, projectile.width, projectile.height),
                new Color(new Vector4(1f - (float)Math.Sqrt(pulseCompletionRatio))) * 0.7f * projectile.Opacity,
                projectile.rotation,
                projectile.Size,
                scale,
                SpriteEffects.None, 0);

            Color pulseColor = Color.Lerp(Color.Yellow, Color.Red, MathHelper.Clamp(pulseCompletionRatio * 1.75f, 0f, 1f));
            GameShaders.Misc["ForceField"].UseColor(pulseColor);
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
