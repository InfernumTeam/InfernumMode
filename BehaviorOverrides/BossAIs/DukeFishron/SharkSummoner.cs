using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class SharkSummoner : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Summon Thing");
        }

        public override void SetDefaults()
        {
            projectile.width = 40;
            projectile.height = 1020;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 30;
        }

        public override void AI()
        {
            Time++;

            projectile.scale = (float)Math.Sin(projectile.timeLeft / 30f * MathHelper.Pi) * 3f;
            if (projectile.scale > 1f)
                projectile.scale = 1f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 top = projectile.Center - Vector2.UnitY * 3000f;
            Vector2 bottom = projectile.Center + Vector2.UnitY * 3000f;
            spriteBatch.DrawLineBetter(top, bottom, Color.Turquoise, projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

            if (projectile.WithinRange(closestPlayer.Center, 200f))
                return;

            int shark = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y - 16, NPCID.Sharkron2);

            Main.npc[shark].velocity = Vector2.UnitY * -projectile.ai[1];
            Main.npc[shark].life = Main.npc[shark].lifeMax = BossRushEvent.BossRushActive ? 11000 : 400;

            Main.npc[shark].direction = projectile.direction;
            Main.npc[shark].spriteDirection = 1;
            Main.npc[shark].ai[0] = 1f;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
