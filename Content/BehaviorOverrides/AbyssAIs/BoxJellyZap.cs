using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class BoxJellyZap : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Jelly Zap");
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 96;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.timeLeft = 180;
            Projectile.Opacity = 0;
        }
        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            Projectile.Center = owner.Center;
            if (owner.active is false)
            {
                Projectile.Kill();
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }

            // Fade in.
            if (Projectile.timeLeft > 20)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.05f, 0, 1);
            else
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.05f, 0, 1);

            Lighting.AddLight(Projectile.Center, 0f, Projectile.Opacity * 0.7f, Projectile.Opacity * 0.7f);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }
    }
}
