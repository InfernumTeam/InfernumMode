using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PairedSoul : ModProjectile
    {
        public Projectile Twin => Main.projectile[(int)Projectile.ai[1]];
        public Player Target => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss))
            {
                Projectile.Kill();
                return;
            }

            NPC polterghast = Main.npc[CalamityGlobalNPC.ghostBoss];
            Projectile.Opacity = Utils.GetLerpValue(150f, 142f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);

            float speedFactor = BossRushEvent.BossRushActive ? 1.67f : 1f;
            if (!Projectile.WithinRange(Twin.Center, 35f))
                Projectile.velocity = (Projectile.velocity * 39f + Projectile.SafeDirectionTo(Twin.Center) * speedFactor * 26f) / 40f;

            if (Projectile.timeLeft < 3)
            {
                Projectile.damage = 0;
                Projectile.velocity = (Projectile.velocity * 11f + Projectile.SafeDirectionTo(polterghast.Center) * speedFactor * 45f) / 12f;
                if (Projectile.Hitbox.Intersects(polterghast.Hitbox))
                {
                    polterghast.ai[2]--;
                    Projectile.Kill();
                }
                Projectile.timeLeft = 2;
            }
            else
                Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(Target.Center), 0.011f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speedFactor * 26f;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Polterghast/SoulLarge").Value;
            if (Projectile.whoAmI % 2 == 0)
                texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Polterghast/SoulLargeCyan").Value;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 2, texture);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.White;
            color.A = 0;
            return color * Projectile.Opacity;
        }

        public override void Kill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 64;
            Projectile.position.X = Projectile.position.X - Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
            Projectile.maxPenetrate = -1;
            Projectile.Damage();
        }
    }
}
