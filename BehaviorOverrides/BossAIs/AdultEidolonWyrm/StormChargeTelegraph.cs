using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class StormChargeTelegraph : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public int Lifetime => (int)projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 8;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 900;
        }

        public override void AI()
        {
            // Disappear if the wyrm is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.adultEidolonWyrmHead))
            {
                projectile.Kill();
                return;
            }

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            NPC wyrm = Main.npc[CalamityGlobalNPC.adultEidolonWyrmHead];

            projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 4f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            projectile.scale = (projectile.Opacity + (float)Math.Cos(Time / 4f) * 0.4f) * 12f;
            if (projectile.scale < 0f)
                projectile.scale = 0f;

            // Attempt to aim at the target.
            float aimInterpolant = 1f - (float)Math.Pow(Utils.InverseLerp(0f, Lifetime * 0.84f, Time, true), 3D);
            projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.SafeDirectionTo(target.Center), aimInterpolant).SafeNormalize(Vector2.UnitY);
            wyrm.Infernum().ExtraAI[0] = projectile.velocity.ToRotation();

            // Ensure that the wyrm's direction is synced prior to charging.
            if (Time == Lifetime - 8f)
            {
                projectile.netUpdate = true;
                wyrm.netUpdate = true;
            }

            // Increment the timer and disappear once it is complete.
            Time++;
            if (Time >= Lifetime)
                projectile.Kill();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Color innerColor = Color.Lerp(Color.Cyan, Color.White, 0.6f) * projectile.Opacity * 0.6f;
            innerColor.A = 72;
            Color outerColor = Color.Lerp(Color.Navy, Color.DarkSlateBlue, (float)Math.Sin(Main.GlobalTime * 10f) * 0.5f + 0.5f) * projectile.Opacity * 0.9f;
            outerColor.A = (byte)MathHelper.Lerp(255f, 192f, projectile.Opacity);

            Vector2 start = projectile.Center;
            Vector2 end = start + projectile.velocity * 4500f;

            spriteBatch.DrawLineBetter(start, end, outerColor, projectile.scale);
            spriteBatch.DrawLineBetter(start, end, innerColor, projectile.scale * 0.5f);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
