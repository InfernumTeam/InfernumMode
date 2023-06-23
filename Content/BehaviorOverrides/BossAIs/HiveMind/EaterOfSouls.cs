using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.HiveMind
{
    public class EaterOfSouls : ModProjectile
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.EaterofSouls}";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eater of Souls");
            Main.projFrames[Type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 32;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 490;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI() => Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.MediumPurple;
            drawColor.A = 0;
            drawColor *= 0.5f;

            Utilities.DrawAfterimagesCentered(Projectile, drawColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return true;
        }

        // This is mainly for multiplayer, to ensure that walls don't spawn on top of players and cheaply hit them.
        public override bool? CanDamage() => Projectile.timeLeft < 460 ? null : false;
    }
}
