using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Shaders;
using CalamityMod.NPCs.DevourerofGods;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGChargeGate : ModProjectile
    {
        public Vector2 Destination;
        public float TelegraphDelay
        {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }
        public bool NoTelegraph => projectile.localAI[0] == 1f;
        public ref float TelegraphTotalTime => ref projectile.ai[1];
        public ref float Lifetime => ref projectile.localAI[1];
        public const float TelegraphFadeTime = 18f;
        public const float TelegraphWidth = 6400f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Portal");
        }

        public override void SetDefaults()
        {
            projectile.width = 580;
            projectile.height = 580;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.timeLeft = 600;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (TelegraphTotalTime == 0f)
                TelegraphTotalTime = 75f;
            if (Lifetime == 0f)
                Lifetime = 225f;

            if (!NoTelegraph && !NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
            {
                projectile.Kill();
                return;
            }

            if (projectile.timeLeft < 600f - Lifetime)
                projectile.Kill();

            TelegraphDelay++;
            if (TelegraphDelay > TelegraphTotalTime)
                projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);

            if (TelegraphDelay < TelegraphTotalTime * 0.8f)
            {
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                Vector2 idealDestination = target.Center + target.velocity * new Vector2(30f, 20f);
                if (Destination == Vector2.Zero)
                    Destination = idealDestination;
                Destination = Vector2.Lerp(Destination, idealDestination, 0.1f).MoveTowards(idealDestination, 5f);
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float fade = Utils.InverseLerp(600f, 565f, projectile.timeLeft, true);
            if (projectile.timeLeft <= 600f - Lifetime + 45f)
                fade = Utils.InverseLerp(600f - Lifetime, 600f - Lifetime + 45f, projectile.timeLeft, true);

            Texture2D noiseTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/VoronoiShapes");
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin2 = noiseTexture.Size() * 0.5f;
            if (NoTelegraph)
            {
                spriteBatch.EnterShaderRegion();

                GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(fade);
                GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Cyan);
                GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Fuchsia);
                GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

                spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin2, 3.5f, SpriteEffects.None, 0f);
                spriteBatch.ExitShaderRegion();
                return false;
            }

            Texture2D laserTelegraph = ModContent.GetTexture("CalamityMod/ExtraTextures/LaserWallTelegraphBeam");
            float yScale = 2f;
            if (TelegraphDelay < TelegraphFadeTime)
            {
                yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / 15f);
            }
            if (TelegraphDelay > TelegraphTotalTime - TelegraphFadeTime)
            {
                yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphTotalTime - TelegraphFadeTime)) / 15f);
            }
            Vector2 scaleInner = new Vector2(TelegraphWidth / laserTelegraph.Width, yScale);
            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 1.9f);

            Color colorOuter = Color.Lerp(Color.Cyan, Color.Purple, TelegraphDelay / TelegraphTotalTime * 4f % 1f); // Iterate through purple and cyan once and then flash.
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.85f);

            colorOuter *= 0.7f;
            colorInner *= 0.7f;

            spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorInner, projectile.AngleTo(Destination), origin, scaleInner, SpriteEffects.None, 0f);
            spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorOuter, projectile.AngleTo(Destination), origin, scaleOuter, SpriteEffects.None, 0f);

            spriteBatch.EnterShaderRegion();

            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(fade);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Cyan);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Fuchsia);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, 0f, origin2, 2.7f, SpriteEffects.None, 0f);
            spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
