using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class RealityCrack : ModProjectile
    {
        public PrimitiveTrailCopy CrackDrawer = null;
        public ref float Time => ref Projectile.ai[0];
        public ref float Lifetime => ref Projectile.ai[1];
        public ref float CrackLength => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Reality Crack");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                CrackLength = Main.rand.NextFloat(100f, 220f);
                Lifetime = Main.rand.Next(60, 120);
                Projectile.localAI[1] = 1f;

                Projectile.netUpdate = true;
            }

            if (Lifetime <= 0f)
                return;

            Projectile.scale = Utils.GetLerpValue(0f, 25f, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - 25f, Time, true);
            Projectile.Opacity = Projectile.scale;

            // Explode before disappearing.
            if (Time == Lifetime - 25f)
            {
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<EssenceExplosion>(), 270, 0f);
            }

            Time++;
        }

        public float PrimitiveWidthFunction(float completionRatio)
        {
            float headCutoff = 0.27f;
            float width = 8f;
            if (completionRatio <= headCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.GetLerpValue(0f, headCutoff, completionRatio, true));
            if (completionRatio >= 1f - headCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.GetLerpValue(1f, 1f - headCutoff, completionRatio, true));
            return width * Projectile.scale + 0.1f;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Pink, Color.White, 0.75f) * Projectile.Opacity;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (CrackDrawer is null)
                CrackDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, GameShaders.Misc["Infernum:BrainPsychic"]);

            GameShaders.Misc["Infernum:BrainPsychic"].UseImage("Images/Misc/Perlin");
            Vector2[] drawPositions = new Vector2[]
            {
                Projectile.Center - Projectile.rotation.ToRotationVector2() * CrackLength * 0.5f,
                Projectile.Center + Projectile.rotation.ToRotationVector2() * CrackLength * 0.5f
            };
            drawPositions = new Vector2[]
            {
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 0.5f),
                Vector2.Lerp(drawPositions.First(), drawPositions.Last(), 1f),
            };

            CrackDrawer.Draw(drawPositions, Projectile.Size * 0.5f - Main.screenPosition, 43);

            // This state reset is necessary to ensure that the backbuffer is flushed immediately and the
            // trail is drawn before anything else. Not doing this may cause problems with vertex/index buffers down the line.
            spriteBatch.ResetBlendState();
            return false;
        }
    }
}
