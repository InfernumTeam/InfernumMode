using System;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs.AstrumDeus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class DeusSpawn : ModNPC
    {
        public bool OrbitAroundDeus
        {
            get => NPC.ai[3] == 0f;
            set => NPC.ai[3] = value ? 0f : 1f;
        }

        public ref float OrbitOffsetAngle => ref NPC.ai[0];

        public ref float OrbitOffsetRadius => ref NPC.ai[1];

        public ref float OrbitAngularVelocity => ref NPC.ai[2];

        public Player Target => Main.player[NPC.target];

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            // DisplayName.SetDefault("Deus Spawn");
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.damage = 170;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 62;
            NPC.defense = 0;
            NPC.lifeMax = 3700;
            if (BossRushEvent.BossRushActive)
                NPC.lifeMax = 35500;

            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Disable contact damage when orbiting.
            NPC.damage = 0;

            // Fuck off if Deus is not present.
            int deusIndex = NPC.FindFirstNPC(ModContent.NPCType<AstrumDeusHead>());
            if (deusIndex == -1)
            {
                NPC.active = false;
                return;
            }

            NPC astrumDeus = Main.npc[deusIndex];

            // Orbit around deus as necessary.
            if (OrbitAroundDeus)
            {
                OrbitOffsetAngle += ToRadians(OrbitAngularVelocity);
                NPC.Center = astrumDeus.Center + OrbitOffsetAngle.ToRotationVector2() * OrbitOffsetRadius;
                NPC.spriteDirection = (Math.Cos(OrbitOffsetAngle) > 0f).ToDirectionInt();
                NPC.rotation = Sin(OrbitOffsetAngle) * 0.11f;
                return;
            }

            // If the spawn shouldn't orbit deus, have it weakly home in on targets and do damage again.
            float flySpeed = BossRushEvent.BossRushActive ? 28f : 19.5f;
            NPC.damage = NPC.defDamage;
            NPC.target = astrumDeus.target;
            NPC.velocity = (NPC.velocity * 59f + NPC.SafeDirectionTo(Target.Center) * flySpeed) / 60f;
            NPC.spriteDirection = (NPC.velocity.X > 0f).ToDirectionInt();
            NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.02f, 0.25f);

            // Go bye bye and explode if sufficiently close to the target or enough time has passed.
            NPC.ai[3]++;
            if (NPC.WithinRange(Target.Center, 105f) || NPC.ai[3] >= 175f)
            {
                NPC.active = false;
                PreKill();
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AstrumDeus/DeusSpawnGlow").Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Vector2 origin = NPC.frame.Size() * 0.5f;
            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            drawColor = Color.Lerp(drawColor, Color.White, 0.5f);

            Main.EntitySpriteDraw(texture, drawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, direction, 0);
            Main.EntitySpriteDraw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, direction, 0);
            return false;
        }

        public override bool PreKill()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            // Release astral flames.
            for (int i = 0; i < 2; i++)
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 15; i++)
            {
                Dust fire = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 0.8f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }

            // Create a spread of homing astral plasma.
            for (int i = 0; i < 8; i++)
            {
                Vector2 cinderVelocity = (TwoPi * i / 8f).ToRotationVector2() * 5.5f;
                Utilities.NewProjectileBetter(NPC.Center, cinderVelocity, ModContent.ProjectileType<AstralPlasmaSpark>(), AstrumDeusHeadBehaviorOverride.AstralPlasmaSparkDamage, 0f, -1, 1f);
            }
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= 6D)
            {
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;

                NPC.frameCounter = 0D;
            }
        }
    }
}
