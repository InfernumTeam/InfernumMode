using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class FlyingSandShark : ModNPC
    {
        public ref float AttackState => ref npc.ai[0];
        public ref float AttackTimer => ref npc.ai[1];
        public ref float Variant => ref npc.ai[2];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Flying Sand Shark");
            NPCID.Sets.TrailingMode[npc.type] = 4;
        }

        public override void SetDefaults()
        {
            npc.aiStyle = -1;
            aiType = -1;
            npc.width = 44;
            npc.height = 44;
            npc.damage = 140;
            npc.defense = 100;
            npc.lifeMax = 1100;
            if (BossRushEvent.BossRushActive)
                npc.lifeMax = 100000;

            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
            npc.alpha = 255;
            npc.noGravity = true;
            npc.dontTakeDamage = true;
            npc.noTileCollide = true;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(npc.dontTakeDamage);
            writer.Write(npc.noGravity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            npc.dontTakeDamage = reader.ReadBoolean();
            npc.noGravity = reader.ReadBoolean();
        }

        public override void AI()
        {
            Main.npcFrameCount[npc.type] = 4;
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
            {
                npc.TargetClosest(false);
                npc.netUpdate = true;
            }

            // Handle directioning and rotation.
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation();
            if (npc.spriteDirection == -1f)
                npc.rotation += MathHelper.Pi;

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.04f, 0f, 1f);

            float idealSpeed = 14f;

            // Fly in a single direction for a while before arcing towards the nearest target.
            if (AttackState == 0f)
            {
                if (AttackTimer == 0f)
                {
                    npc.velocity = -Vector2.UnitY.RotatedByRandom(0.42f) * (idealSpeed - 5f);
                    Main.PlaySound(SoundID.NPCDeath19, npc.position);
                }

                AttackTimer++;
                if (AttackTimer >= 60f)
                {
                    if (!Collision.SolidCollision(npc.position, npc.width, npc.height) && AttackTimer >= 150f)
                    {
                        AttackState = 1f;
                        npc.netUpdate = true;
                    }

                    float oldSpeed = npc.velocity.Length();
                    npc.velocity = (npc.velocity * 14f + npc.SafeDirectionTo(Main.player[npc.target].Center) * oldSpeed) / 15f;
                    npc.velocity.Normalize();
                    npc.velocity *= oldSpeed;
                }
            }

            // Continue moving in a single direction before dying.
            else if (AttackState == 1f)
            {
                AttackTimer++;
                if (npc.velocity.Length() > idealSpeed)
                    npc.velocity *= 0.99f;

                npc.dontTakeDamage = false;

                if (AttackTimer >= 360f)
                {
                    if (npc.DeathSound != null)
                        Main.PlaySound(npc.DeathSound, npc.position);

                    npc.life = 0;
                    npc.HitEffect(0, 10.0);
                    npc.checkDead();
                    npc.active = false;
                    return;
                }

                if (AttackTimer >= 210f)
                {
                    npc.noGravity = false;
                    npc.velocity.Y += 0.3f;
                }
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => npc.alpha == 0;

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Rabies, 180, true);
            player.AddBuff(BuffID.Bleeding, 240, true);
        }

        public override bool CheckDead()
        {
            Main.PlaySound(SoundID.NPCDeath1, npc.position);

            npc.position.X = npc.position.X + (npc.width / 2);
            npc.position.Y = npc.position.Y + (npc.height / 2);
            npc.width = npc.height = 72;
            npc.position.X = npc.position.X - (npc.width / 2);
            npc.position.Y = npc.position.Y - (npc.height / 2);

            for (int i = 0; i < 30; i++)
            {
                Dust blood = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 100, default, 3f);
                blood.noGravity = true;
                blood.velocity.Y *= 10f;

                blood = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 100, default, 2f);
                blood.velocity.X *= 2f;
            }

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            if (Variant >= 4f)
                Variant = 0f;

            npc.frameCounter++;
            npc.frame.Width = 122;
            npc.frame.Height = 74;
            npc.frame.X = (int)(Variant * npc.frame.Width);
            npc.frame.Y = (int)(npc.frameCounter / 5) % Main.npcFrameCount[npc.type] * npc.frame.Height;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 origin = npc.frame.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
        }
    }
}