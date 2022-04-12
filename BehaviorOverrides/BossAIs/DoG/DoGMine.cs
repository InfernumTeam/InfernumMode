using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGMine : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cosmic Mine");

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.alpha = 100;
            CooldownSlot = 1;
        }

        public override void AI() => Projectile.alpha = Utils.Clamp(Projectile.alpha - 10, 0, 255);

        public override Color? GetAlpha(Color drawColor) => new Color(200, Main.DiscoG, 255, Projectile.alpha);

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.Find<ModBuff>("GodSlayerInferno").Type, 300);
            target.AddBuff(BuffID.Darkness, 300, true);
        }

        public override void Kill(int timeLeft)
        {
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 14);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Cardinal Directions
            for (float i = 0; i < 4f; i += 1f)
            {
                float angle = MathHelper.TwoPi / 4f * i;
                Projectile.NewProjectile(new InfernumSource(), Projectile.Center, (angle + MathHelper.PiOver2).ToRotationVector2() * 6f, InfernumMode.CalamityMod.Find<ModProjectile>("CosmicFlameBurst").Type, 80, 0f);
            }
        }
    }
}
