using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class RealityCrack : ModProjectile
    {
        public PrimitiveTrailCopy CrackDrawer = null;
        public ref float Time => ref projectile.ai[0];
        public ref float Lifetime => ref projectile.ai[1];
        public ref float CrackLength => ref projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Reality Crack");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
        }

        public override void AI()
        {
            if (projectile.localAI[1] == 0f)
            {
                projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                CrackLength = Main.rand.NextFloat(100f, 220f);
                Lifetime = Main.rand.Next(60, 120);
                projectile.localAI[1] = 1f;

                projectile.netUpdate = true;
            }

            if (Lifetime <= 0f)
                return;

            projectile.scale = Utils.InverseLerp(0f, 25f, Time, true) * Utils.InverseLerp(Lifetime, Lifetime - 25f, Time, true);
            projectile.Opacity = projectile.scale;

            // Explode before disappearing.
            if (Time == Lifetime - 25f)
            {
                Main.PlaySound(SoundID.DD2_ExplosiveTrapExplode, projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<EssenceExplosion>(), 270, 0f);
            }

            Time++;
        }

        public float PrimitiveWidthFunction(float completionRatio)
        {
            float headCutoff = 0.27f;
            float width = 8f;
            if (completionRatio <= headCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.InverseLerp(0f, headCutoff, completionRatio, true));
            if (completionRatio >= 1f - headCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.InverseLerp(1f, 1f - headCutoff, completionRatio, true));
            return width * projectile.scale + 0.1f;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Pink, Color.White, 0.75f) * projectile.Opacity;
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (CrackDrawer is null)
                CrackDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, GameShaders.Misc["Infernum:BrainPsychic"]);

            GameShaders.Misc["Infernum:BrainPsychic"].UseImage("Images/Misc/Perlin");
            Vector2[] drawPositions = new Vector2[]
            {
                projectile.Center - projectile.rotation.ToRotationVector2() * CrackLength * 0.5f,
                projectile.Center + projectile.rotation.ToRotationVector2() * CrackLength * 0.5f
            };
            drawPositions = new Vector2[]
            {
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0.5f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 1f),
            };

            CrackDrawer.Draw(drawPositions, projectile.Size * 0.5f - Main.screenPosition, 43);

            // This state reset is necessary to ensure that the backbuffer is flushed immediately and the
            // trail is drawn before anything else. Not doing this may cause problems with vertex/index buffers down the line.
            spriteBatch.ResetBlendState();
            return false;
        }
    }
}
