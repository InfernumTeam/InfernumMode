using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class ExplosivePlagueCharger : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float Timer => ref NPC.ai[0];
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            // DisplayName.SetDefault("Explosive Plague Charger");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.damage = 150;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 42;
            NPC.defense = 10;
            NPC.lifeMax = 620;
            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Lighting.AddLight(NPC.Center, 0.03f, 0.2f, 0f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                NPC.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (NPC.timeLeft > 10)
                        NPC.timeLeft = 10;
                    return;
                }
            }
            else if (NPC.timeLeft > 600)
                NPC.timeLeft = 600;

            // Fly upwards before charging at the target.
            if (Timer < 40f)
            {
                if (NPC.velocity == Vector2.Zero)
                {
                    NPC.velocity = -Vector2.UnitY.RotatedByRandom(0.26f) * Main.rand.NextFloat(5f, 8f);
                    NPC.netUpdate = true;
                }
                NPC.velocity *= 0.97f;
                NPC.rotation = -NPC.velocity.X * 0.02f;
            }

            // Charge after this.
            else
            {
                Vector2 idealVelocity = NPC.SafeDirectionTo(Target.Center) * 12.5f;
                if (BossRushEvent.BossRushActive)
                    idealVelocity *= 2f;

                NPC.velocity = (NPC.velocity * 34f + idealVelocity) / 35f;
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 0.15f);
                NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.032f, 0.15f);
            }

            NPC.spriteDirection = (NPC.velocity.X > 0f).ToDirectionInt();
            Timer++;

            // Explode if close to the target or enough time has passed.
            if (NPC.WithinRange(Target.Center, 40f) || Timer >= Main.rand.NextFloat(400f, 620f))
            {
                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }

            // Gradually die.
            NPC.life -= NPC.life / 540;
            if (NPC.life <= 0)
            {
                NPC.HitEffect();
                NPC.checkDead();
            }
        }

        public override bool PreKill() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.position);

            NPC.position = NPC.Center;
            NPC.width = NPC.height = 216;
            NPC.Center = NPC.position;

            for (int i = 0; i < 15; i++)
            {
                Dust plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                if (Main.rand.NextBool(2))
                {
                    plague.scale = 0.5f;
                    plague.fadeIn = Main.rand.NextFloat(1f, 2f);
                }
                plague.velocity *= 3f;
                plague.noGravity = true;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, 0f, 0f, 100, default, 3f);
                plague.noGravity = true;
                plague.velocity *= 5f;

                plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                plague.velocity *= 2f;
                plague.noGravity = true;
            }
            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            int wingFlapRate = (int)Clamp(10f - NPC.velocity.Length() * 0.37f, 2f, 8f);
            if (NPC.frameCounter >= wingFlapRate)
            {
                NPC.frame.Y += 66;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;

                NPC.frameCounter = 0D;
            }
        }
    }
}
