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
        public Player Target => Main.player[NPC.target];
        public ref float Time => ref NPC.ai[0];

        public const float InitialSpeed = 0.3f;
        public const float RedirectSpeed = 11f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bubble");
            Main.npcFrameCount[NPC.type] = 2;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = aiType = -1;
            NPC.damage = 70;
            NPC.width = NPC.height = 36;
            NPC.lifeMax = 420;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => NPC.life = 1300;

        public override void AI()
        {
            float redirectSpeed = RedirectSpeed * (BossRushEvent.BossRushActive ? 2f : 1f);
            if (Time < 45 && NPC.velocity.Length() < redirectSpeed)
                NPC.velocity *= (float)Math.Pow(redirectSpeed / InitialSpeed, 1f / 45f);
            else if (Time >= 45f)
                NPC.velocity = NPC.velocity.RotateTowards(NPC.AngleTo(Target.Center), MathHelper.ToRadians(2.4f));

            if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
            {
                NPC.active = false;
                NPC.netUpdate = true;
            }

            if (Time >= 180f)
            {
                NPC.velocity *= 0.96f;
                if (NPC.velocity.Length() < 0.5f)
                {
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
                NPC.scale = MathHelper.Lerp(1f, 1.6f, Utils.GetLerpValue(1.8f, 0.7f, NPC.velocity.Length(), true));
            }

            Time++;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = frameHeight * (NPC.whoAmI % 2);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.type == ModContent.ProjectileType<Corrocloud1>() || projectile.type == ModContent.ProjectileType<Corrocloud2>() || projectile.type == ModContent.ProjectileType<Corrocloud3>())
                damage = (int)(damage * 0.225);
        }

        public override bool CheckActive() => false;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            CooldownSlot = 1;
            return true;
        }
    }
}
