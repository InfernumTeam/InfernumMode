using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class NebulaTelegraphLine : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
        }

        public override void AI()
        {
            int cultistIndex = NPC.FindFirstNPC(NPCID.CultistBoss);
            if (cultistIndex < 0)
            {
                Projectile.Kill();
                return;
            }

            NPC cultist = Main.npc[cultistIndex];

            Projectile.Center = cultist.Center;
            Projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 2f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.Opacity *= Projectile.localAI[0];
            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphWidth = MathHelper.Lerp(0.8f, 5f, CalamityUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 4200f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.MediumPurple * Projectile.Opacity, telegraphWidth);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}