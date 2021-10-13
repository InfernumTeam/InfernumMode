using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class FireBeamTelegraph : ModProjectile
    {
        public ref float TargetIndex => ref projectile.ai[0];
        public NPC Owner => Main.npc[(int)projectile.ai[1]];
        public Player Target => Main.player[(int)TargetIndex];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 85;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Determine an initial target.
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.localAI[0] == 0f)
			{
                TargetIndex = Player.FindClosest(projectile.Center, 1, 1);
                projectile.localAI[0] = 1f;
                projectile.netUpdate = true;
			}

            projectile.scale = Utils.InverseLerp(0f, 10f, projectile.timeLeft, true) * Utils.InverseLerp(85f, 75f, projectile.timeLeft, true);
            projectile.scale = MathHelper.SmoothStep(0.04f, 4f, projectile.scale);

            // Try to aim at the target.
            if (projectile.timeLeft > 32f)
                projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.SafeDirectionTo(Target.Center), 0.09f);

            projectile.Center = Owner.Center + projectile.velocity.SafeNormalize(Vector2.UnitY) * 70f;

            if (!Owner.active)
                projectile.Kill();

            Owner.rotation = projectile.velocity.ToRotation();
            if (Owner.direction < 0)
                Owner.rotation += MathHelper.Pi;

            if (projectile.timeLeft > 30)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2CircularEdge(40f, 40f), 264);
                    fire.color = Color.Orange;
                    fire.velocity = (projectile.Center - fire.position) * 0.08f;
                    fire.fadeIn = 0.5f;
                    fire.noGravity = true;
                    fire.noLight = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 aimDirection = projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i <= 4; i++)
            {
                float lineWidth = MathHelper.SmoothStep(0.25f, 1f, i / 4f) * projectile.scale;
                Color lineColor = Color.Lerp(Color.White, Color.Orange, MathHelper.Lerp(0.15f, 1f, i / 4f));
                lineColor.A = 0;

                spriteBatch.DrawLineBetter(projectile.Center, projectile.Center + aimDirection * 2050f, lineColor, lineWidth);
            }
            return false;
        }

        public override bool ShouldUpdatePosition() => false;

		public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, Target.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 beamDirection = projectile.velocity.SafeNormalize(Vector2.UnitY);
            int beam = Utilities.NewProjectileBetter(projectile.Center, beamDirection, ModContent.ProjectileType<FireBeamWoF>(), 220, 0f);
            if (Main.projectile.IndexInRange(beam))
                Main.projectile[beam].ai[1] = Owner.whoAmI;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
