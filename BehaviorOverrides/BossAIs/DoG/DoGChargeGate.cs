using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using Terraria.ID;

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
        public const float TelegraphTotalTime = 180f;
        public const float TelegraphFadeTime = 30f;
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
            projectile.timeLeft = 280;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (projectile.localAI[1] == 0f)
            {
                Filters.Scene.Activate("Infernum:DoGPortal", projectile.Center).GetShader().UseColor(10, 2, 10).UseTargetPosition(projectile.Center);
                projectile.localAI[1] = 1f;
            }
            float fade = Utils.InverseLerp(280f, 235f, projectile.timeLeft, true);
            if (projectile.timeLeft <= 45f)
                fade = Utils.InverseLerp(0f, 45f, projectile.timeLeft, true);

            Filters.Scene["Infernum:DoGPortal"].GetShader().UseProgress(fade);
            Filters.Scene["Infernum:DoGPortal"].GetShader().UseColor(Color.Cyan);
            Filters.Scene["Infernum:DoGPortal"].GetShader().UseSecondaryColor(Color.Fuchsia);

            if (Main.netMode != NetmodeID.Server)
            {
                Filters.Scene["Infernum:DoGPortal"].GetShader().UseProgress(fade);
                Filters.Scene["Infernum:DoGPortal"].GetShader().UseColor(Color.Cyan);
                Filters.Scene["Infernum:DoGPortal"].GetShader().UseSecondaryColor(Color.Fuchsia);
                Filters.Scene["Infernum:DoGPortal"].GetShader().UseImage(ModContent.GetTexture("InfernumMode/ExtraTextures/VoronoiShapes"));
            }

            TelegraphDelay++;
            if (TelegraphDelay > TelegraphTotalTime)
                projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);

            if (TelegraphDelay < 135f)
                Destination = Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (NoTelegraph)
                return false;

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
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Filters.Scene["Infernum:DoGPortal"].IsActive())
                Filters.Scene["Infernum:DoGPortal"].Deactivate();
        }   
    }
}
