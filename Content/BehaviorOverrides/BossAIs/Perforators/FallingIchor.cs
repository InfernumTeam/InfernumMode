using CalamityMod;
using CalamityMod.NPCs.Perforator;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class FallingIchor : ModProjectile
    {
        internal const float Gravity = 0.25f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 420;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.tileCollide = Projectile.timeLeft < 300;

            bool smallWormIsPresent = NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadSmall>());
            if (smallWormIsPresent)
                Projectile.tileCollide = false;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            bool shouldDie = Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
            shouldDie &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16).TileType];
            if (shouldDie && Projectile.tileCollide)
                Projectile.Kill();

            // Release blood idly.
            Dust blood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default, 0.5f);
            blood.velocity = Vector2.Zero;
            blood.noGravity = true;

            Projectile.velocity.Y += Gravity;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(246, 195, 80, Projectile.alpha);
    }
}
