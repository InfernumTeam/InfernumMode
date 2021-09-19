using CalamityMod.NPCs;
using CalamityMod.NPCs.Yharon;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Yharon
{
    public class DetonatingFlameBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DetonatingFlare>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults;

        #region AI

        public override bool PreAI(NPC npc)
        {
            npc.alpha -= 3;
            Player player = Main.player[npc.target];
            if (npc.ai[0] == 0f)
            {
                float speed = 10f;
                if (npc.localAI[3] == 0f)
                {
                    switch (Main.rand.Next(3))
                    {
                        case 0:
                            speed = 18f;
                            break;
                        case 1:
                            speed = 16f;
                            break;
                        case 2:
                            speed = 21f;
                            break;
                    }
                    npc.localAI[3] = 1f;
                }
                CalamityAI.DungeonSpiritAI(npc, InfernumMode.CalamityMod, speed, 0f);
                npc.spriteDirection = 1;
                if (Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(player.Center)) > 0.85f && npc.ai[1] > 260f + Main.rand.NextFloat(20, 100))
                {
                    npc.ai[0] = 1f;
                    npc.velocity = npc.SafeDirectionTo(player.Center) * npc.velocity.Length();
                    npc.rotation = npc.velocity.ToRotation();
                    npc.noTileCollide = false;
                    npc.Hitbox.Inflate(45, 45);
                    npc.netUpdate = true;
                }
                npc.ai[1]++;
            }
            if (npc.ai[0] == 1f && npc.velocity.Length() < 31f)
            {
                npc.velocity *= 1.065f;
                if (npc.Hitbox.Intersects(player.Hitbox) || npc.collideX || npc.collideY)
                {
                    Main.PlaySound(SoundID.Item14, npc.Center);

                    Vector2 center = npc.Center;
                    npc.width = 200;
                    npc.height = 200;
                    npc.Center = center;

                    int direction = npc.Center.X - player.Center.X < 0 ? -1 : 1;
                    if (npc.Hitbox.Intersects(player.Hitbox))
                        player.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), npc.damage, direction);

                    npc.value = 0f;
                    npc.extraValue = 0f;
                    npc.life = 0;
                    npc.StrikeNPCNoInteraction(9999, 1f, 1);

                    int size = 50;
                    Vector2 offset = new Vector2(size / -2f);

                    for (int i = 0; i < 45; i++)
                    {
                        int dust = Dust.NewDust(npc.Center - offset, size, size, DustID.Fire, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, Main.rand.NextFloat(1f, 2f));
                        Main.dust[dust].velocity *= 1.4f;
                    }
                    for (int i = 0; i < 15; i++)
                    {
                        int dust = Dust.NewDust(npc.Center - offset, size, size, 31, 0f, 0f, 100, default, 1.7f);
                        Main.dust[dust].velocity *= 1.4f;
                    }
                    for (int i = 0; i < 27; i++)
                    {
                        int dust = Dust.NewDust(npc.Center - offset, size, size, 6, 0f, 0f, 100, default, 2.4f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 5f;
                        dust = Dust.NewDust(npc.Center - offset, size, size, 6, 0f, 0f, 100, default, 1.6f);
                        Main.dust[dust].velocity *= 3f;
                    }
                }
            }
            return false;
        }

        #endregion

        #region Set Defaults

        public override void SetDefaults(NPC npc)
        {
            npc.damage = 400;
            npc.lifeMax = 19000;
            npc.lifeMax /= 2; // Incorporate expert mode.
        }
        #endregion
    }
}