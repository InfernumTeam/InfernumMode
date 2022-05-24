using CalamityMod.Events;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class RedirectingBubble : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float Time => ref npc.ai[0];

        public const float InitialSpeed = 0.3f;
        public const float RedirectSpeed = 11f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bubble");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.damage = 70;
            npc.width = npc.height = 36;
            npc.lifeMax = 200;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => npc.life = 1300;

        public override void AI()
        {
            float redirectSpeed = RedirectSpeed * (BossRushEvent.BossRushActive ? 2f : 1f);
            if (Time < 45 && npc.velocity.Length() < redirectSpeed)
                npc.velocity *= (float)Math.Pow(redirectSpeed / InitialSpeed, 1f / 45f);
            else if (Time >= 45f)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), MathHelper.ToRadians(2.4f));

            if (Collision.SolidCollision(npc.position, npc.width, npc.height) || npc.WithinRange(Target.Center, 40f))
            {
                npc.active = false;
                npc.netUpdate = true;
            }

            if (Collision.WetCollision(npc.position, npc.width, npc.height))
            {
                npc.life -= 4;
                if (npc.life <= 0f)
                    npc.active = false;
            }

            if (Time >= 180f)
            {
                npc.velocity *= 0.96f;
                if (npc.velocity.Length() < 0.5f)
                {
                    npc.active = false;
                    npc.netUpdate = true;
                }
                npc.scale = MathHelper.Lerp(1f, 1.6f, Utils.InverseLerp(1.8f, 0.7f, npc.velocity.Length(), true));
            }

            Time++;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frame.Y = frameHeight * (npc.whoAmI % 2);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.type == ModContent.ProjectileType<Corrocloud1>() || projectile.type == ModContent.ProjectileType<Corrocloud2>() || projectile.type == ModContent.ProjectileType<Corrocloud3>())
                damage = (int)(damage * 0.225);
        }

        public override bool CheckActive() => false;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = 1;
            return true;
        }
    }
}
