using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Destroyer
{
    public class DestroyerBomb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 660;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Projectile.frameCounter++;

            // Flick with red right before death as a telegraph.
            if (Projectile.frameCounter % 4 == 3)
                Projectile.frame = Projectile.frame == 0 ? Projectile.timeLeft < 60 ? 2 : 1 : 0;

            if (Projectile.velocity.Y < 17f)
                Projectile.velocity.Y += 0.2f;

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Projectile.tileCollide = Projectile.timeLeft < 540;

            Tile tileAtPosition = CalamityUtils.ParanoidTileRetrieval((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16);
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (TileID.Sets.Platforms[tileAtPosition.TileType] && tileAtPosition.HasTile && Projectile.tileCollide || Projectile.WithinRange(closestPlayer.Center, 60f) && Projectile.timeLeft < 580)
                Projectile.Kill();

            Lighting.AddLight(Projectile.Center, Vector3.One * 0.85f);
        }

        // Explode on death.
        public override void Kill(int timeLeft)
        {
            Projectile.ExpandHitboxBy(84);
            Projectile.damage = 50;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            Utils.PoofOfSmoke(Projectile.Center);
        }


    }
}
