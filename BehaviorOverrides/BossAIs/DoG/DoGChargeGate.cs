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
                Filters.Scene.Activate("Infernum:DistortionShader", projectile.Center).GetShader().UseColor(10, 2, 10).UseTargetPosition(projectile.Center);
                projectile.localAI[1] = 1f;
            }
            float fade = Utils.InverseLerp(280, 230, projectile.timeLeft, true);
            if (projectile.timeLeft <= 30)
                fade = Utils.InverseLerp(0, 50, projectile.timeLeft, true);

            Filters.Scene["Infernum:DistortionShader"].GetShader().UseProgress(0.42f * fade).UseOpacity(480f * fade);

            for (int i = 0; i < 10; i++)
			{
                Vector2 offsetDirection = Main.rand.NextVector2Unit();
                Dust cosmicFire = Dust.NewDustPerfect(projectile.Center + offsetDirection * Main.rand.NextFloat(20f, 300f), 173);
                cosmicFire.velocity = offsetDirection.RotatedBy(MathHelper.PiOver2) * 4f;
                cosmicFire.scale = 1.3f + Utils.InverseLerp(20f, 300f, projectile.Distance(cosmicFire.position)) * 0.45f;
                cosmicFire.noGravity = true;
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
            if (Main.netMode != NetmodeID.MultiplayerClient && Filters.Scene["Infernum:DistortionShader"].IsActive())
                Filters.Scene["Infernum:DistortionShader"].Deactivate();
        }   
    }
}
