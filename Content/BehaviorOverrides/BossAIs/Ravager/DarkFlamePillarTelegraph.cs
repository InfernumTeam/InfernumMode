using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class DarkFlamePillarTelegraph : ModProjectile
    {
        public ref float Countdown => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, 3600f - Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Countdown, true);
            Projectile.scale = Projectile.Opacity * 5f;
            Countdown--;

            if (Countdown <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 top = Projectile.Center - Vector2.UnitY * 1500f;
            Vector2 bottom = Projectile.Center + Vector2.UnitY * 1500f;
            Color color = Color.SkyBlue * Projectile.Opacity;
            Main.spriteBatch.DrawLineBetter(top, bottom, color, Projectile.scale);
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }
    }
}
