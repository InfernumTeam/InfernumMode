using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CorruptionMimic
{
    public class CursedFlamePillar : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Cursed Flame Pillar");

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 440;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            if (Time >= 30f)
                Projectile.velocity = (Projectile.velocity * 1.025f).ClampMagnitude(5f, 30f);

            float dustCreationChance = Utils.GetLerpValue(0f, 30f, Time, true);
            for (int i = 0; i < 20; i++)
            {
                if (Main.rand.NextFloat() > dustCreationChance)
                    continue;

                Dust fire = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, 267);
                fire.color = Color.Lerp(Color.Yellow, Color.Lime, Main.rand.NextFloat(0.1f, 1f));
                fire.velocity = Projectile.velocity * 0.25f;
                fire.scale = 1.15f;
                fire.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool ShouldUpdatePosition() => Time >= 30f;

        public override bool? CanDamage() => Time >= 30f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
