using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
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
            // DisplayName.SetDefault("Explosion");
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
                Projectile.velocity.Y += 0.25f;

            // Rotate.
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Collide with tiles after enough time has passed.
            Projectile.tileCollide = Projectile.timeLeft < 420;

            Tile tileAtPosition = Framing.GetTileSafely((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16);
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (((TileID.Sets.Platforms[tileAtPosition.TileType] && tileAtPosition.HasTile && Projectile.tileCollide) || Projectile.WithinRange(closestPlayer.Center, 60f)) && Projectile.timeLeft < 580)
                Projectile.Kill();

            Lighting.AddLight(Projectile.Center, Vector3.One * 0.85f);
        }

        // Explode on death.
        public override void OnKill(int timeLeft)
        {
            Projectile.ExpandHitboxBy(84);
            Projectile.damage = 50;
            Projectile.Damage();

            // Create particles and sounds at the explosion point.
            for (int i = 0; i < 5; i++)
            {
                Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Red;
                CloudParticle fireCloud = new(Projectile.Center, Main.rand.NextVector2Circular(4f, 4f), fireColor, Color.DarkGray, 30, Main.rand.NextFloat(1.67f, 1.85f));
                GeneralParticleHandler.SpawnParticle(fireCloud);
            }
            Utils.PoofOfSmoke(Projectile.Center);
            SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerBombExplodeSound with { MaxInstances = 1 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.Red with { A = 0 } * 0.4f, Color.White, Projectile.Opacity * 4f);
            return false;
        }
    }
}
