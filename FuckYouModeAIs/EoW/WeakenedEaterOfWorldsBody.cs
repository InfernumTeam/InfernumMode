using CalamityMod.NPCs.HiveMind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EoW
{
	public class WeakenedEaterOfWorldsBody : ModNPC
    {
        public ref float TotalDamageTaken => ref npc.ai[3];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eater of Worlds");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.width = npc.height = 40;
            npc.netAlways = true;
            npc.damage = 11;
            npc.defense = -20;
            npc.lifeMax = EoWAIClass.TotalLifeAcrossWorm;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.knockBackResist = 0f;
            npc.behindTiles = true;
            npc.value = 300f;
            npc.scale = 1f;
            npc.aiStyle = -1;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
            npc.dontCountMe = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(npc.dontTakeDamage);

        public override void ReceiveExtraAI(BinaryReader reader) => npc.dontTakeDamage = reader.ReadBoolean();

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TotalDamageTaken > 200f && !npc.dontTakeDamage)
            {
                for (int i = 0; i < 3; i++)
                {
                    int blob = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<DarkHeart>());
                    if (Main.npc.IndexInRange(blob))
                    {
                        Main.npc[blob].Center += Main.rand.NextVector2Circular(40f, 40f);
                        Main.npc[blob].velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
                        Main.npc[blob].life = Main.npc[blob].lifeMax = 420;
                        Main.npc[blob].netUpdate = true;
                    }
                }
                npc.dontTakeDamage = true;
                TotalDamageTaken = 0f;
                npc.netUpdate = true;
            }

            EoWAIClass.EoWSegmentAI(npc);
            npc.takenDamageMultiplier = 2f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (npc.dontTakeDamage)
                return true;

            Texture2D texture = Main.npcTexture[npc.type];
            
            for (int i = 0; i < 12; i++)
            {
                drawColor = Color.YellowGreen * 0.25f;
                drawColor.A = 0;

                Vector2 drawPosition = npc.Center + (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 4f).ToRotationVector2() * 7f - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                spriteBatch.Draw(texture, drawPosition, npc.frame, drawColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }
            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frame.Y = frameHeight * npc.dontTakeDamage.ToInt();
        }

        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            TotalDamageTaken += (float)(damage * (crit ? 2D : 1D));
            npc.netUpdate = true;
            return base.StrikeNPC(ref damage, defense, ref knockback, hitDirection, ref crit);
        }

        public override bool CheckActive() => false;

        public override bool PreNPCLoot() => false;
    }
}
