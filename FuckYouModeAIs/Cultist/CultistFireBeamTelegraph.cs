using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Cultist
{
	public class CultistFireBeamTelegraph : ModProjectile
    {
        public ref float TargetIndex => ref projectile.ai[0];
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
                projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.DirectionTo(Target.Center), 0.15f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 aimDirection = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Utils.DrawLine(spriteBatch, projectile.Center, projectile.Center + aimDirection * FireBeam.LaserLength, Color.Orange, Color.OrangeRed, projectile.scale);
            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool ShouldUpdatePosition() => false;

		public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Zombie, Target.Center, 104);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float aimDirection = (MathHelper.WrapAngle(projectile.AngleTo(Target.Center) - projectile.velocity.ToRotation()) > 0f).ToDirectionInt();
            Vector2 beamDirection = projectile.velocity.SafeNormalize(Vector2.UnitY);

            int beam = Utilities.NewProjectileBetter(projectile.Center, beamDirection, ModContent.ProjectileType<FireBeam>(), 205, 0f);
            if (Main.projectile.IndexInRange(beam))
                Main.projectile[beam].ai[1] = aimDirection * 0.028f;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
