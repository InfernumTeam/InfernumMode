using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class FireballLineTelegraph : ModProjectile
    {
        public ref float TelegraphDelay => ref projectile.ai[0];
        public ref float PulseFlash => ref projectile.localAI[0];

        public Vector2 InitialDestination;
        public Vector2 Destination;
        public Vector2 Velocity;
        public const float TelegraphTotalTime = 25f;
        public const float TelegraphFadeTime = 15f;
        public const float TelegraphWidth = 1900f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 22;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.extraUpdates = 1;
            projectile.timeLeft = 960;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(Destination);
            writer.WriteVector2(Velocity);
            writer.WriteVector2(InitialDestination);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Destination = reader.ReadVector2();
            Velocity = reader.ReadVector2();
            InitialDestination = reader.ReadVector2();
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > 12)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.frame = 0;
            TelegraphDelay++;

            if (Main.netMode != NetmodeID.MultiplayerClient && TelegraphDelay >= 38f)
            {
                Vector2 fireballShootVelocity = projectile.SafeDirectionTo(Destination, Vector2.UnitY) * 7f;

                int fireball = Utilities.NewProjectileBetter(projectile.Center, fireballShootVelocity, ProjectileID.CultistBossFireBall, 195, 0f);
                if (Main.projectile.IndexInRange(fireball))
                    Main.projectile[fireball].tileCollide = false;
                projectile.Kill();
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D laserTelegraph = ModContent.GetTexture("InfernumMode/ExtraTextures/Line");

            float yScale = 5f;
            if (TelegraphDelay < TelegraphFadeTime)
                yScale = MathHelper.Lerp(0f, yScale, TelegraphDelay / 15f);
            if (TelegraphDelay > 38f - TelegraphFadeTime)
                yScale = MathHelper.Lerp(yScale, 0f, (TelegraphDelay - (38f - TelegraphFadeTime)) / 15f);

            Vector2 scaleInner = new Vector2(TelegraphWidth / laserTelegraph.Width, yScale);
            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 1.5f);

            Color colorOuter = Color.Lerp(Color.Orange, Color.Yellow, TelegraphDelay / 38f * 0.4f);
            Vector2 direction = projectile.SafeDirectionTo(Destination);
            spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorOuter, direction.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);
            return false;
        }
    }
}