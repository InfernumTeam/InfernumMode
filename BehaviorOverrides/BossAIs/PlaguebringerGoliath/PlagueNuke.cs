using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueNuke : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public bool Unfinished
		{
			get => npc.ai[1] == 1f;
            set => npc.ai[1] = value.ToInt();
		}
        public ref float ExistTimer => ref npc.ai[0];
        public ref float DisappearTimer => ref npc.ai[2];
        public const int BuildTime = 720;
        public const int ExplodeDelay = 150;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plague Nuke");
            Main.npcFrameCount[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            npc.damage = 100;
            npc.npcSlots = 0f;
            npc.width = npc.height = 54;
            npc.defense = 15;
            npc.lifeMax = 10000;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.dontTakeDamage = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            // Fall onto the ground and disappear if unfinished.
            if (Unfinished)
			{
                DisappearTimer++;
                npc.velocity.X *= 0.95f;
                npc.Opacity = Utils.InverseLerp(360f, 210f, DisappearTimer, true);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                npc.noTileCollide = false;
                npc.noGravity = false;

                if (DisappearTimer > 360f)
                    npc.active = false;
                return;
			}

            ExistTimer++;

            bool moving = ExistTimer >= BuildTime && !Unfinished;

            // Attempt to hit the target if moving.
            if (moving)
            {
                npc.velocity = (npc.velocity * 39f + npc.SafeDirectionTo(Target.Center) * 14f) / 40f;
                if (npc.velocity.Length() < 7f)
                    npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 7f;

                // Explode and die after the explosion delay is passed.
                if (ExistTimer > BuildTime + ExplodeDelay)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeMechGaussRifle"), npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<PlagueNuclearExplosion>(), 350, 0f);

                    npc.life = 0;
                    npc.checkDead();
                    npc.active = false;
                }
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                return;
            }
            else
            {
                List<NPC> builders = Main.npc.Where(n =>
                {
                    return n.active && (n.type == ModContent.NPCType<BuilderDroneSmall>() || n.type == ModContent.NPCType<BuilderDroneBig>());
                }).ToList();

                // Stop being built mid-way if the builders are all gone.
                if (builders.Count == 0)
				{
                    Unfinished = true;
                    npc.netUpdate = true;
                    return;
                }

                Vector2 averageBuilderPosition = Vector2.Zero;
                for (int i = 0; i < builders.Count; i++)
                    averageBuilderPosition += builders[i].Center;
                averageBuilderPosition /= builders.Count;

                // Attempt to move to the average position between all the builders.
                npc.Center = Vector2.Lerp(npc.Center, averageBuilderPosition, 0.0145f);

                float distanceToAveragePosition = npc.Distance(averageBuilderPosition);

                Vector2 idealVelocity = npc.SafeDirectionTo(averageBuilderPosition) * MathHelper.Min(distanceToAveragePosition, 16f);
                npc.velocity = (npc.velocity * 3f + idealVelocity) / 4f;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 1.5f);

                // Rotate towards the player.
                float idealRotation = npc.AngleTo(Target.Center + Target.velocity * 25f) - MathHelper.PiOver2;
                npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.025f);
            }

            npc.TargetClosest();
        }

        public override bool PreNPCLoot() => false;

		public override bool CheckDead()
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.position);

            npc.position = npc.Center;
            npc.width = npc.height = 84;
            npc.Center = npc.position;
            return true;
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlagueNukeGlowmask");
            Vector2 origin = npc.frame.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Color color = npc.GetAlpha(drawColor);

            spriteBatch.Draw(texture, drawPosition, npc.frame, color, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
		}

		public override void FindFrame(int frameHeight)
        {
            float buildCompletion = MathHelper.Clamp(ExistTimer / BuildTime, 0f, 1f);
            npc.frame.Y = (int)MathHelper.Lerp(0f, Main.npcFrameCount[npc.type] - 1f, buildCompletion) * frameHeight;
        }
    }
}
