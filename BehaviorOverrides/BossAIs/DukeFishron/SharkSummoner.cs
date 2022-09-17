using CalamityMod.Events;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class SharkSummoner : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Summon Thing");
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 1020;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            Time++;

            Projectile.scale = (float)Math.Sin(Projectile.timeLeft / 30f * MathHelper.Pi) * 3f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 top = Projectile.Center - Vector2.UnitY * 3000f;
            Vector2 bottom = Projectile.Center + Vector2.UnitY * 3000f;
            Main.spriteBatch.DrawLineBetter(top, bottom, Color.Turquoise, Projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            if (Projectile.WithinRange(closestPlayer.Center, 200f))
                return;

            int shark = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y - 16, NPCID.Sharkron2);

            Main.npc[shark].velocity = Vector2.UnitY * -Projectile.ai[1];
            Main.npc[shark].life = Main.npc[shark].lifeMax = BossRushEvent.BossRushActive ? 11000 : 400;

            Main.npc[shark].noTileCollide = true;
            Main.npc[shark].direction = Projectile.direction;
            Main.npc[shark].spriteDirection = 1;
            Main.npc[shark].ai[0] = 1f;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            
        }
    }
}
