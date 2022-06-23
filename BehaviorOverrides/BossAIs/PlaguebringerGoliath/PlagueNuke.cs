using CalamityMod.Items.Weapons.DraedonsArsenal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueNuke : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public bool Unfinished
        {
            get => NPC.ai[1] == 1f;
            set => NPC.ai[1] = value.ToInt();
        }
        public ref float ExistTimer => ref NPC.ai[0];
        public ref float DisappearTimer => ref NPC.ai[2];
        public const int BuildTime = 660;
        public const int ExplodeDelay = 150;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plague Nuke");
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.damage = 100;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 54;
            NPC.defense = 15;
            NPC.lifeMax = 10000;
            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            // Fall onto the ground and disappear if unfinished.
            if (Unfinished)
            {
                DisappearTimer++;
                NPC.velocity.X *= 0.95f;
                NPC.Opacity = Utils.GetLerpValue(460f, 310f, DisappearTimer, true);
                NPC.noTileCollide = false;
                NPC.noGravity = false;

                if (DisappearTimer > 360f || NPC.collideX || NPC.collideY)
                {
                    if (Main.netMode != NetmodeID.Server && NPC.collideX || NPC.collideY)
                    {
                        for (int i = 1; i <= 5; i++)
                            Gore.NewGore(NPC.GetSource_FromAI(), NPC.Center, Main.rand.NextVector2Circular(2f, 2f), Mod.Find<ModGore>($"PlagueNuke{i}").Type);
                    }
                    NPC.active = false;
                }
                NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
                return;
            }

            ExistTimer++;

            bool moving = ExistTimer >= BuildTime && !Unfinished;

            // Attempt to hit the target if moving.
            List<NPC> builders = Main.npc.Where(n =>
            {
                return n.active && (n.type == ModContent.NPCType<BuilderDroneSmall>() || n.type == ModContent.NPCType<BuilderDroneBig>());
            }).ToList();
            if (moving)
            {
                NPC.velocity = (NPC.velocity * 39f + NPC.SafeDirectionTo(Target.Center) * 14f) / 40f;
                if (NPC.velocity.Length() < 7f)
                    NPC.velocity = NPC.velocity.SafeNormalize(-Vector2.UnitY) * 7f;

                // Explode and die after the explosion delay is passed.
                if (ExistTimer > BuildTime + ExplodeDelay)
                {
                    SoundEngine.PlaySound(GaussRifle.FireSound, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<PlagueNuclearExplosion>(), 750, 0f);

                    NPC.life = 0;
                    NPC.checkDead();
                    NPC.active = false;
                }
                NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
                return;
            }
            else
            {
                // Stop being built mid-way if the builders are all gone.
                if (builders.Count == 0)
                {
                    Unfinished = true;
                    NPC.netUpdate = true;
                    return;
                }

                Vector2 averageBuilderPosition = Vector2.Zero;
                for (int i = 0; i < builders.Count; i++)
                    averageBuilderPosition += builders[i].Center;
                averageBuilderPosition /= builders.Count;

                // Attempt to move to the average position between all the builders.
                NPC.Center = Vector2.Lerp(NPC.Center, averageBuilderPosition, 0.0145f);

                float distanceToAveragePosition = NPC.Distance(averageBuilderPosition);

                Vector2 idealVelocity = NPC.SafeDirectionTo(averageBuilderPosition) * MathHelper.Min(distanceToAveragePosition, 16f);
                NPC.velocity = (NPC.velocity * 3f + idealVelocity) / 4f;
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 1.5f);

                // Rotate towards the player.
                float idealRotation = NPC.AngleTo(Target.Center + Target.velocity * 25f) - MathHelper.PiOver2;
                NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.025f);
            }

            NPC.TargetClosest();
        }

        public override bool PreKill() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.position);

            NPC.position = NPC.Center;
            NPC.width = NPC.height = 84;
            NPC.Center = NPC.position;
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlagueNukeGlowmask").Value;
            Vector2 origin = NPC.frame.Size() * 0.5f;
            Vector2 drawPosition = NPC.Center - Main.screenPosition;
            Color color = NPC.GetAlpha(drawColor);

            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, color, NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            float buildCompletion = MathHelper.Clamp(ExistTimer / BuildTime, 0f, 1f);
            NPC.frame.Y = (int)MathHelper.Lerp(0f, Main.npcFrameCount[NPC.type] - 1f, buildCompletion) * frameHeight;
        }
    }
}
