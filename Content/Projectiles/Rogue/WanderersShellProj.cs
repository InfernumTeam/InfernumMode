using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class WanderersShellProj : ModProjectile
    {
        public bool HasBounced
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public const int Lifetime = 600;

        public override string Texture => "InfernumMode/Content/Items/Weapons/Rogue/WanderersShell";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Wanderer's Shell");

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.minion = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.DamageType = RogueDamageClass.Instance;
        }

        public override void AI()
        {
            // Fade out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 90f, Projectile.timeLeft, true);

            // SPEEN.
            Projectile.rotation += Projectile.velocity.X * 0.024f;

            // Home in on targets if the shell hasn't bounced yet.
            if (!HasBounced)
                CalamityUtils.HomeInOnNPC(Projectile, true, 800f, 10f, 20f);

            // If it has bounced, adhere to gravity.
            else
            {
                Projectile.velocity.X *= 0.95f;
                Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.54f, -8f, 15.9f);
                Projectile.tileCollide = true;
            }
        }

        // Prevent death by tile collision.
        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Bounce off of targets.
            if (!HasBounced)
            {
                HasBounced = true;
                Projectile.velocity = Projectile.SafeDirectionTo(target.Center, -Vector2.UnitY) * 10f;
                Projectile.netUpdate = true;
            }
        }
    }
}
