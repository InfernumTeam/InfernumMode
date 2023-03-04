using InfernumMode.Common.Graphics;
using InfernumMode.Content.BehaviorOverrides.BossAIs.WallOfFlesh;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyAimedDeathrayTelegraph : FireBeamTelegraph, IScreenCullDrawer
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 72;
        }

        public override void AI()
        {
            // Determine an initial target.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] == 0f)
            {
                TargetIndex = Player.FindClosest(Projectile.Center, 1, 1);
                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }

            Projectile.scale = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true) * Utils.GetLerpValue(85f, 75f, Projectile.timeLeft, true);
            Projectile.scale = MathHelper.SmoothStep(0.04f, 4f, Projectile.scale);

            // Try to aim at the target.
            if (Projectile.timeLeft > 32f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center), 0.09f);

            Projectile.Center = Owner.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 70f;

            if (!Owner.active)
                Projectile.Kill();

            if (Projectile.timeLeft > 30)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(40f, 40f), 264);
                    fire.color = Color.Orange;
                    fire.velocity = (Projectile.Center - fire.position) * 0.08f;
                    fire.fadeIn = 0.5f;
                    fire.noGravity = true;
                    fire.noLight = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override void Kill(int timeLeft)
        {
            if (Target.Center.Y > (Main.maxTilesY - 300f) * 16f)
                SoundEngine.PlaySound(SoundID.Item74, Target.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 beamDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Utilities.NewProjectileBetter(Projectile.Center, beamDirection, ModContent.ProjectileType<HolyAimedDeathray>(), 220, 0f, -1, 0f, Owner.whoAmI);
        }

        public void CullDraw(SpriteBatch spriteBatch)
        {
            Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i <= 4; i++)
            {
                float lineWidth = MathHelper.SmoothStep(0.25f, 1f, i / 4f) * Projectile.scale;
                Color lineColor = Color.Lerp(Color.White, Color.Orange, MathHelper.Lerp(0.15f, 1f, i / 4f));
                lineColor.A = 0;

                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + aimDirection * 8200f, lineColor, lineWidth);
            }
        }
    }
}
