using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class ProvArenaPlatform : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(string.Empty);
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.lifeMax = 500;
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontCountMe = true;
            NPC.width = 100;
            NPC.height = 24;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0;
            NPC.Opacity = 0f;
            NPC.netAlways = true;
        }

        public override void AI()
        {
            // Die if Providence is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss))
            {
                NPC.active = false;
                return;
            }

            // Die if the platform has left Providence's arena.
            if (!Main.npc[CalamityGlobalNPC.holyBoss].Infernum().arenaRectangle.Intersects(NPC.Hitbox))
            {
                NPC.active = false;
                return;
            }

            // Fade in.
            NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.1f, 0f, 1f);

            NPC.gfxOffY = -6;

            float offsetFromPreviousPosition = NPC.position.Y - NPC.oldPosition.Y;
            foreach (Player player in Main.player)
            {
                if (!player.active || player.dead || player.GoingDownWithGrapple || Collision.SolidCollision(player.position, player.width, player.height) || player.controlDown)
                    continue;

                Rectangle playerRect = new((int)player.position.X, (int)player.position.Y + (player.height), player.width, 1);

                int effectiveNPCHitboxHeight = Math.Min((int)player.velocity.Y, 0) + (int)Math.Abs(offsetFromPreviousPosition) + 14;
                if (playerRect.Intersects(new Rectangle((int)NPC.position.X, (int)NPC.position.Y, NPC.width, effectiveNPCHitboxHeight)) && player.position.Y <= NPC.position.Y)
                {
                    if (!player.justJumped && player.velocity.Y >= 0 && !Collision.SolidCollision(player.position + player.velocity, player.width, player.height))
                    {
                        player.velocity.Y = 0;
                        player.position.Y = NPC.position.Y - player.height + 4;
                        player.position += NPC.velocity;

                        if (Math.Abs(player.velocity.X) < 0.01f)
                        {
                            player.legFrame.Y = 0;
                            player.legFrameCounter = 0;
                        }
                        player.wingFrame = 0;
                        player.wingFrameCounter = 0;
                        player.bodyFrame.Y = 0;
                        player.bodyFrameCounter = 0;
                    }
                }
            }
        }

        // Ensure that platforms are fullbright, for visual clarity.
        public override Color? GetAlpha(Color drawColor) => Color.White * NPC.Opacity;

        public override bool CheckActive() => false;

        public override bool? CanBeHitByItem(Player player, Item item) => false;

        public override bool? CanBeHitByProjectile(Projectile projectile) => false;
    }
}
