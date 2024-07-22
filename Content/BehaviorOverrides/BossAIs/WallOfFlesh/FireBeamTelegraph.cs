using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class FireBeamTelegraph : ModProjectile
    {
        public ref float TargetIndex => ref Projectile.ai[0];
        public NPC Owner => Main.npc[(int)Projectile.ai[1]];
        public Player Target => Main.player[(int)TargetIndex];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 85;
            Projectile.penetrate = -1;
            
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
            Projectile.scale = SmoothStep(0.04f, 4f, Projectile.scale);

            // Try to aim at the target.
            if (Projectile.timeLeft > 32f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center), 0.09f);

            Projectile.Center = Owner.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 70f;

            if (!Owner.active)
                Projectile.Kill();

            Owner.rotation = Projectile.velocity.ToRotation();
            if (Owner.direction < 0)
                Owner.rotation += Pi;

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

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 aimDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i <= 4; i++)
            {
                float lineWidth = SmoothStep(0.25f, 1f, i / 4f) * Projectile.scale;
                Color lineColor = Color.Lerp(Color.White, Color.Orange, Lerp(0.15f, 1f, i / 4f));
                lineColor.A = 0;

                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + aimDirection * 2050f, lineColor, lineWidth);
            }
            return false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnKill(int timeLeft)
        {
            if (Target.Center.Y > (Main.maxTilesY - 300f) * 16f)
                SoundEngine.PlaySound(SoundID.Item74, Target.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 beamDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Utilities.NewProjectileBetter(Projectile.Center, beamDirection, ModContent.ProjectileType<FireBeamWoF>(), WallOfFleshMouthBehaviorOverride.FireBeamDamage, 0f, -1, 0f, Owner.whoAmI);
        }
    }
}
