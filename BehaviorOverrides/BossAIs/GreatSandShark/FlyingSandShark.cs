using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class FlyingSandShark : ModNPC
    {
        public ref float AttackState => ref NPC.ai[0];
        public ref float AttackTimer => ref NPC.ai[1];
        public ref float Variant => ref NPC.ai[2];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Flying Sand Shark");
            NPCID.Sets.TrailingMode[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            aiType = -1;
            NPC.width = 44;
            NPC.height = 44;
            NPC.damage = 140;
            NPC.defense = 100;
            NPC.lifeMax = 1100;
            if (BossRushEvent.BossRushActive)
                NPC.lifeMax = 100000;

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.alpha = 255;
            NPC.noGravity = true;
            NPC.dontTakeDamage = true;
            NPC.noTileCollide = true;
            for (int k = 0; k < NPC.buffImmune.Length; k++)
                NPC.buffImmune[k] = true;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.dontTakeDamage);
            writer.Write(NPC.noGravity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.dontTakeDamage = reader.ReadBoolean();
            NPC.noGravity = reader.ReadBoolean();
        }

        public override void AI()
        {
            Main.npcFrameCount[NPC.type] = 4;
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead)
            {
                NPC.TargetClosest(false);
                NPC.netUpdate = true;
            }

            // Handle directioning and rotation.
            NPC.spriteDirection = (NPC.velocity.X > 0f).ToDirectionInt();
            NPC.rotation = NPC.velocity.ToRotation();
            if (NPC.spriteDirection == -1f)
                NPC.rotation += MathHelper.Pi;

            // Fade in.
            NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.04f, 0f, 1f);

            float idealSpeed = 14f;

            // Fly in a single direction for a while before arcing towards the nearest target.
            if (AttackState == 0f)
            {
                if (AttackTimer == 0f)
                {
                    NPC.velocity = -Vector2.UnitY.RotatedByRandom(0.42f) * (idealSpeed - 5f);
                    SoundEngine.PlaySound(SoundID.NPCDeath19, NPC.position);
                }

                AttackTimer++;
                if (AttackTimer >= 60f)
                {
                    if (!Collision.SolidCollision(NPC.position, NPC.width, NPC.height) && AttackTimer >= 150f)
                    {
                        AttackState = 1f;
                        NPC.netUpdate = true;
                    }

                    float oldSpeed = NPC.velocity.Length();
                    NPC.velocity = (NPC.velocity * 14f + NPC.SafeDirectionTo(Main.player[NPC.target].Center) * oldSpeed) / 15f;
                    NPC.velocity.Normalize();
                    NPC.velocity *= oldSpeed;
                }
            }

            // Continue moving in a single direction before dying.
            else if (AttackState == 1f)
            {
                AttackTimer++;
                if (NPC.velocity.Length() > idealSpeed)
                    NPC.velocity *= 0.99f;

                NPC.dontTakeDamage = false;

                if (AttackTimer >= 360f)
                {
                    if (NPC.DeathSound != null)
                        SoundEngine.PlaySound(NPC.DeathSound, NPC.position);

                    NPC.life = 0;
                    NPC.HitEffect(0, 10.0);
                    NPC.checkDead();
                    NPC.active = false;
                    return;
                }

                if (AttackTimer >= 210f)
                {
                    NPC.noGravity = false;
                    NPC.velocity.Y += 0.3f;
                }
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => NPC.alpha == 0;

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Rabies, 180, true);
            player.AddBuff(BuffID.Bleeding, 240, true);
        }

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.position);

            NPC.position.X = NPC.position.X + (NPC.width / 2);
            NPC.position.Y = NPC.position.Y + (NPC.height / 2);
            NPC.width = NPC.height = 72;
            NPC.position.X = NPC.position.X - (NPC.width / 2);
            NPC.position.Y = NPC.position.Y - (NPC.height / 2);

            for (int i = 0; i < 30; i++)
            {
                Dust blood = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f, 100, default, 3f);
                blood.noGravity = true;
                blood.velocity.Y *= 10f;

                blood = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f, 100, default, 2f);
                blood.velocity.X *= 2f;
            }

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            if (Variant >= 4f)
                Variant = 0f;

            NPC.frameCounter++;
            NPC.frame.Width = 122;
            NPC.frame.Height = 74;
            NPC.frame.X = (int)(Variant * NPC.frame.Width);
            NPC.frame.Y = (int)(NPC.frameCounter / 5) % Main.npcFrameCount[NPC.type] * NPC.frame.Height;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = NPC.frame.Size() * 0.5f;
            Vector2 drawPosition = NPC.Center - Main.screenPosition;
            SpriteEffects direction = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, direction, 0f);
            return false;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
        }
    }
}