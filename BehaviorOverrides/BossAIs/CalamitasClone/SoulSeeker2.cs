using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class SoulSeeker2 : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public float RingRadius => Main.npc[CalamityGlobalNPC.calamitas].Infernum().ExtraAI[6];
        public ref float RingAngle => ref NPC.ai[0];
        public ref float AngerTimer => ref NPC.ai[1];
        public ref float AttackTimer => ref NPC.ai[2];

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
            brotherIsPresent |= (Main.npc.IndexInRange(CalamityGlobalNPC.calamitas) && Main.npc[CalamityGlobalNPC.calamitas].ai[3] > 0f && Main.npc[CalamityGlobalNPC.calamitas].ai[3] < 50f);
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.calamitas) || !brotherIsPresent)
            {
                NPC.active = false;
                return;
            }

            NPC calamitas = Main.npc[CalamityGlobalNPC.calamitas];
            NPC.target = calamitas.target;
            NPC.Center = calamitas.Center + RingAngle.ToRotationVector2() * RingRadius;
            NPC.Opacity = 1f - calamitas.Opacity;
            float idealRotation = RingAngle;
            if (!Target.WithinRange(calamitas.Center, RingRadius + 60f))
            {
                idealRotation = NPC.AngleTo(Target.Center);

                AngerTimer++;
                AttackTimer++;
                if (AttackTimer >= MathHelper.Lerp(90f, 30f, Utils.GetLerpValue(30f, 520f, AngerTimer, true)))
                {
                    int dartDamage = 150;
                    Vector2 shootVelocity = NPC.SafeDirectionTo(Target.Center + Target.velocity * 15f) * 21f;
                    int dart = Utilities.NewProjectileBetter(NPC.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), dartDamage, 0f);
                    if (Main.projectile.IndexInRange(dart))
                    {
                        Main.projectile[dart].ai[0] = 1f;
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
            NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.1f);
        }

        public override bool CheckActive() => false;

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            NPC.frame.Y = (int)(NPC.frameCounter / 5D + RingAngle / MathHelper.TwoPi * 50f) % Main.npcFrameCount[NPC.type] * frameHeight;
        }

        public override bool PreKill() => false;
    }
}
