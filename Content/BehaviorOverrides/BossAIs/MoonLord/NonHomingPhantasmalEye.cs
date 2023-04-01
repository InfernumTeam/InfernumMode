using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class NonHomingPhantasmalEye : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float SpinSpeed => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PhantasmalEye}";

        public override void SetStaticDefaults()
        {
            // HOLY SHIT IS THAT A FARGO REFERENCE OH MY GOD I AM GOING TO CANCEL DOMINIC VON KARMA FOR THIS
            DisplayName.SetDefault("Phantasmal Eye");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.alpha = 225;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.velocity = Projectile.velocity.RotatedBy(SpinSpeed);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 0f, Projectile.Opacity * 0.4f, Projectile.Opacity * 0.4f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor) => true;

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.Nightwither, 0f, 0f);
        }
    }
}
