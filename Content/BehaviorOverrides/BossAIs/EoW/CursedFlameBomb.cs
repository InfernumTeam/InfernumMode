using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EoW
{
    public class CursedFlameBomb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flame Bomb");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.tileCollide = Projectile.timeLeft < 120;
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * 0.3f;

            Lighting.AddLight(Projectile.Center, Vector3.One * 0.7f);
        }

        // Explode into smaller flames on death.
        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int burstCount = NPC.CountNPCS(NPCID.EaterofWorldsHead) >= 4 ? 4 : 5;
            float burstSpeed = Projectile.velocity.Length();
            float initialAngleOffset = Main.rand.NextFloat(TwoPi);
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 shootVelocity = (initialAngleOffset + TwoPi * i / burstCount).ToRotationVector2() * burstSpeed;
                Utilities.NewProjectileBetter(Projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<CursedBullet>(), EoWHeadBehaviorOverride.CursedCinderDamage, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.White, Color.MediumPurple, Utils.GetLerpValue(45f, 0f, Projectile.timeLeft, true)) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 1f;
    }
}
