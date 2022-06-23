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
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public float MaxRadius
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        public bool ShouldDeleteProjectiles => Projectile.localAI[1] != 0f;
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
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                MaxRadius = Main.rand.NextFloat(2000f, 4000f);
                Projectile.localAI[0] = 1f;
            }
            Main.LocalPlayer.Infernum().CurrentScreenShakePower = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / Lifetime) * 10f;

            Lighting.AddLight(Projectile.Center, 0.2f, 0.1f, 0f);
            Radius = MathHelper.Lerp(Radius, MaxRadius, 0.15f);
            Projectile.scale = MathHelper.Lerp(1.2f, 5f, Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));
            CalamityGlobalProjectile.ExpandHitboxBy(Projectile, (int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));

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

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            float pulseCompletionRatio = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            Vector2 scale = new(1.5f, 1f);
            DrawData drawData = new(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value,
                Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, Projectile.width, Projectile.height),
                new Color(new Vector4(1f - (float)Math.Sqrt(pulseCompletionRatio))) * 0.7f * Projectile.Opacity,
                Projectile.rotation,
                Projectile.Size,
                scale,
                SpriteEffects.None, 0);

            Color pulseColor = Color.Lerp(Color.Yellow, Color.Red, MathHelper.Clamp(pulseCompletionRatio * 1.75f, 0f, 1f));
            GameShaders.Misc["ForceField"].UseColor(pulseColor);
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
