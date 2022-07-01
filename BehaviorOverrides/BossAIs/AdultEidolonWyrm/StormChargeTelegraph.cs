using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class StormChargeTelegraph : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public int Lifetime => (int)Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
        }

        public override void AI()
        {
            // Disappear if the wyrm is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.adultEidolonWyrmHead))
            {
                Projectile.Kill();
                return;
            }

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            NPC wyrm = Main.npc[CalamityGlobalNPC.adultEidolonWyrmHead];

            Projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 4f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.scale = (Projectile.Opacity + (float)Math.Cos(Time / 4f) * 0.4f) * 12f;
            if (Projectile.scale < 0f)
                Projectile.scale = 0f;

            // Attempt to aim at the target.
            float aimInterpolant = 1f - (float)Math.Pow(Utils.GetLerpValue(0f, Lifetime * 0.84f, Time, true), 3D);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center), aimInterpolant).SafeNormalize(Vector2.UnitY);
            wyrm.Infernum().ExtraAI[0] = Projectile.velocity.ToRotation();

            // Ensure that the wyrm's direction is synced prior to charging.
            if (Time == Lifetime - 8f)
            {
                Projectile.netUpdate = true;
                wyrm.netUpdate = true;
            }

            // Increment the timer and disappear once it is complete.
            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color innerColor = Color.Lerp(Color.Cyan, Color.White, 0.6f) * Projectile.Opacity * 0.6f;
            innerColor.A = 72;
            Color outerColor = Color.Lerp(Color.Navy, Color.DarkSlateBlue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f) * Projectile.Opacity * 0.9f;
            outerColor.A = (byte)MathHelper.Lerp(255f, 192f, Projectile.Opacity);

            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * 4500f;

            Main.spriteBatch.DrawLineBetter(start, end, outerColor, Projectile.scale);
            Main.spriteBatch.DrawLineBetter(start, end, innerColor, Projectile.scale * 0.5f);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
