using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CalClone;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class SoulSeeker2 : ModNPC
    {
        public Player Target => Main.player[NPC.target];

        public ref float RingAngle => ref NPC.ai[0];

        public ref float AngerTimer => ref NPC.ai[1];

        public ref float AttackTimer => ref NPC.ai[2];

        public override string Texture => "CalamityMod/NPCs/CalClone/SoulSeeker";

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Soul Seeker");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = AIType = -1;
            NPC.damage = 0;
            NPC.width = 40;
            NPC.height = 30;
            NPC.defense = 0;
            NPC.lifeMax = 100;
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.canGhostHeal = false;
        }

        public override void AI()
        {
            bool brotherIsPresent = NPC.AnyNPCs(ModContent.NPCType<Cataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<Catastrophe>());
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.calamitas) || !brotherIsPresent)
            {
                NPC.active = false;
                return;
            }

            NPC calamitas = Main.npc[CalamityGlobalNPC.calamitas];
            NPC.target = calamitas.target;
            NPC.Center = calamitas.Center + RingAngle.ToRotationVector2() * 950f;
            NPC.Opacity = 1f - calamitas.Opacity;

            NPC.spriteDirection = (calamitas.Center.X > NPC.Center.X).ToDirectionInt();
            if (!Target.WithinRange(calamitas.Center, 1000f))
            {
                NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();

                AngerTimer++;
                AttackTimer++;
                if (AttackTimer >= MathHelper.Lerp(90f, 30f, Utils.GetLerpValue(30f, 520f, AngerTimer, true)))
                {
                    Vector2 shootVelocity = NPC.SafeDirectionTo(Target.Center + Target.velocity * 15f) * 21f;
                    int dart = Utilities.NewProjectileBetter(NPC.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), CalamitasShadowBehaviorOverride.BrimstoneDartDamage, 0f, -1, 1f);
                    if (Main.projectile.IndexInRange(dart))
                    {
                        Main.projectile[dart].tileCollide = false;
                        Main.projectile[dart].netUpdate = true;
                    }
                    AttackTimer = 0f;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                AngerTimer = 0f;
                AttackTimer = 0f;
            }
        }

        public override bool CheckActive() => false;

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            NPC.frame.Width = 88;
            NPC.frame.Height = 105;
            NPC.frame.Y = (int)(NPC.frameCounter / 5D + RingAngle / MathHelper.TwoPi * 50f) % 5 * NPC.frame.Height;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Other/CalamitasEnchantDemon").Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Vector2 origin = NPC.frame.Size() * 0.5f;
            SpriteEffects direction = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, direction, 0f);
            return false;
        }

        public override bool PreKill() => false;
    }
}