using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class ExplosivePlagueCharger : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float Timer => ref npc.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosive Plague Charger");
            Main.npcFrameCount[npc.type] = 5;
        }

        public override void SetDefaults()
        {
            npc.damage = 180;
            npc.npcSlots = 0f;
            npc.width = npc.height = 42;
            npc.defense = 10;
            npc.lifeMax = 620;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Lighting.AddLight(npc.Center, 0.03f, 0.2f, 0f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                npc.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (npc.timeLeft > 10)
                        npc.timeLeft = 10;
                    return;
                }
            }
            else if (npc.timeLeft > 600)
                npc.timeLeft = 600;

            // Fly upwards before charging at the target.
            if (Timer < 40f)
            {
                if (npc.velocity == Vector2.Zero)
                {
                    npc.velocity = -Vector2.UnitY.RotatedByRandom(0.26f) * Main.rand.NextFloat(5f, 8f);
                    npc.netUpdate = true;
                }
                npc.velocity *= 0.97f;
                npc.rotation = -npc.velocity.X * 0.02f;
            }

            // Charge after this.
            else
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(Target.Center) * 12.5f;
                if (BossRushEvent.BossRushActive)
                    idealVelocity *= 2f;

                npc.velocity = (npc.velocity * 34f + idealVelocity) / 35f;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.15f);
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.032f, 0.15f);
            }

            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            Timer++;

            // Explode if close to the target or enough time has passed.
            if (npc.WithinRange(Target.Center, 40f) || Timer >= Main.rand.NextFloat(400f, 620f))
            {
                npc.life = 0;
                npc.checkDead();
                npc.active = false;
            }

            // Gradually die.
            npc.life -= npc.life / 540;
            if (npc.life <= 0)
            {
                npc.HitEffect();
                npc.checkDead();
            }
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.position);

            npc.position = npc.Center;
            npc.width = npc.height = 216;
            npc.Center = npc.position;

            for (int i = 0; i < 15; i++)
            {
                Dust plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 2f);
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
                Dust plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 3f);
                plague.noGravity = true;
                plague.velocity *= 5f;

                plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 2f);
                plague.velocity *= 2f;
                plague.noGravity = true;
            }
            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter++;

            int wingFlapRate = (int)MathHelper.Clamp(10f - npc.velocity.Length() * 0.37f, 2f, 8f);
            if (npc.frameCounter >= wingFlapRate)
            {
                npc.frame.Y += 52;
                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                    npc.frame.Y = 0;

                npc.frameCounter = 0D;
            }
        }
    }
}
