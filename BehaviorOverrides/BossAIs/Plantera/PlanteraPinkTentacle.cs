using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class PlanteraPinkTentacle : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float Time => ref NPC.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plantera's Tentacle");
            Main.npcFrameCount[NPC.type] = 4;

            // Ensure that the tentacle always draws, even when far offscreen.
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.damage = 120;
            NPC.width = 28;
            NPC.height = 28;
            NPC.defense = 5;
            NPC.lifeMax = 500;
            NPC.aiStyle = AIType = -1;
            NPC.dontTakeDamage = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.hide = true;
        }

        public override void AI()
        {
            // Die if Plantera is absent or not using tentacles.
            if (!Main.npc.IndexInRange(NPC.plantBoss) || Main.npc[NPC.plantBoss].ai[0] != (int)PlanteraBehaviorOverride.PlanteraAttackState.TentacleSnap)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.checkDead();
                NPC.netUpdate = true;
                return;
            }

            float attachAngle = NPC.ai[0];
            ref float attachOffset = ref NPC.ai[1];
            ref float time = ref NPC.ai[2];

            // Reel inward prior to snapping.
            if (time > 0f && time < 45f)
                attachOffset = MathHelper.Lerp(attachOffset, 60f, 0.05f);

            // Reach outward swiftly in hopes of hitting a target.
            if (time > 180f)
                attachOffset = MathHelper.Lerp(attachOffset, 3900f, 0.021f);

            if (time == 180f)
                SoundEngine.PlaySound(SoundID.Item74, NPC.Center);

            if (time > 220f)
            {
                NPC.scale *= 0.85f;

                // Die once small enough.
                NPC.Opacity = NPC.scale;
                if (NPC.scale < 0.01f)
                {
                    NPC.life = 0;
                    NPC.HitEffect();
                    NPC.checkDead();
                    NPC.active = false;
                }
            }

            NPC.Center = Main.npc[NPC.plantBoss].Center + attachAngle.ToRotationVector2() * attachOffset;
            NPC.rotation = attachAngle + MathHelper.Pi;
            NPC.dontTakeDamage = true;

            time++;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsOverPlayers.Add(index);
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter += 0.2f;
            NPC.frameCounter %= Main.npcFrameCount[NPC.type];
            int frame = (int)NPC.frameCounter;
            NPC.frame.Y = frame * frameHeight;
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Venom, 120, true);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!Main.npc.IndexInRange(NPC.plantBoss) || Main.npc[NPC.plantBoss].ai[0] != (int)PlanteraBehaviorOverride.PlanteraAttackState.TentacleSnap)
                return true;

            NPC plantera = Main.npc[NPC.plantBoss];

            Vector2 drawPosition = plantera.Center;
            float rotation = NPC.AngleFrom(plantera.Center) + MathHelper.PiOver2;
            bool canStillDraw = true;
            while (canStillDraw)
            {
                int moveDistance = 16;
                if (NPC.Distance(drawPosition) < 32f)
                {
                    moveDistance = (int)NPC.Distance(drawPosition) - 32 + moveDistance;
                    canStillDraw = false;
                }
                drawPosition += plantera.SafeDirectionTo(NPC.Center, Vector2.Zero) * moveDistance;
                Color color = Lighting.GetColor((int)(drawPosition.X / 16f), (int)(drawPosition.Y / 16f));
                Rectangle frame = new(0, 0, TextureAssets.Chain27.Value.Width, moveDistance);
                Main.spriteBatch.Draw(TextureAssets.Chain27.Value, drawPosition - Main.screenPosition, frame, color, rotation, TextureAssets.Chain27.Value.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            return true;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 2, hitDirection, -1f, 0, default, 1f);

            if (NPC.life <= 0)
            {
                for (int k = 0; k < 15; k++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 2, hitDirection, -1f, 0, default, 1f);
            }
        }
    }
}
