using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class Lightning : ModProjectile
    {
        public int telegraphTimer = 80;
        public Vector2 targetPosition;
        public ref float SpeedMultiplier => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning");
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = 88;
            Projectile.hostile = true;
            Projectile.alpha = byte.MaxValue;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 4;
            Projectile.timeLeft = 120 * (Projectile.extraUpdates + 1);
            telegraphTimer *= Projectile.extraUpdates;
        }

        public override void AI()
        {
            if (telegraphTimer == 79)
                targetPosition = Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center + Main.rand.NextVector2Circular(80f, 80f);

            if (Main.netMode != NetmodeID.MultiplayerClient && telegraphTimer == 1)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(targetPosition) * SpeedMultiplier * 9f;
                int lightning = Utilities.NewProjectileBetter(Projectile.Center, Projectile.velocity, ProjectileID.CultistBossLightningOrbArc, 350, 0f, Projectile.owner, Projectile.velocity.ToRotation(), Main.rand.Next(100));
                if (Main.projectile.IndexInRange(lightning))
                {
                    Main.projectile[lightning].ai[0] = Projectile.velocity.ToRotation();
                    Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    Main.projectile[lightning].tileCollide = false;
                }
                Projectile.Kill();
            }
            telegraphTimer--;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (var i = 0; i < Projectile.oldPos.Length && (Projectile.oldPos[i].X != 0.0 || Projectile.oldPos[i].Y != 0.0); i++)
            {
                projHitbox.X = (int)Projectile.oldPos[i].X;
                projHitbox.Y = (int)Projectile.oldPos[i].Y;
                if (projHitbox.Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

		public override void PostDraw(Color lightColor)
        {
            if (telegraphTimer < 79)
                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + Projectile.AngleTo(targetPosition).ToRotationVector2() * 5000f, Color.Cyan, 3f);
        }
    }
}
