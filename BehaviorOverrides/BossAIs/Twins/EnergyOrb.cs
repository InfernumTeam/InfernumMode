using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class EnergyOrb : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public NPC Owner => Main.npc[(int)NPC.ai[0]];
        public ref float AngularDirection => ref NPC.ai[1];
        public ref float AngularOffset => ref NPC.ai[2];
        internal ref float OwnerAttackTimer => ref Owner.Infernum().ExtraAI[10];
        internal TwinsAttackSynchronizer.RetinazerAttackState OwnerAttackState => (TwinsAttackSynchronizer.RetinazerAttackState)(int)Owner.Infernum().ExtraAI[11];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy Orb");
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 40;
            NPC.damage = 120;
            NPC.lifeMax = 1500000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.alpha = 255;
            NPC.scale = 1.15f;
        }

        public override void AI()
        {
            NPC.life = NPC.lifeMax;
            if (!Main.npc.IndexInRange((int)NPC.ai[0]) || !Owner.active)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            NPC.target = Owner.target;
            NPC.timeLeft = 3600;
            NPC.damage = NPC.defDamage;
            NPC.rotation += MathHelper.ToRadians(16f * AngularDirection);

            if (OwnerAttackState == TwinsAttackSynchronizer.RetinazerAttackState.DanceOfLightnings)
            {
                AngularOffset += AngularDirection / 8f * Utils.GetLerpValue(60f, 30f, OwnerAttackTimer, true);

                if (OwnerAttackTimer < 75f)
                {
                    if (OwnerAttackTimer > 60f)
                        AngularOffset = AngularOffset.AngleLerp(Target.AngleTo(Target.Center) + MathHelper.PiOver2 * AngularDirection - MathHelper.PiOver2, 0.25f);
                    NPC.Center = Vector2.Lerp(NPC.Center, Owner.Center + AngularOffset.ToRotationVector2() * Vector2.One * 150f, 0.21f);
                    NPC.damage = 0;
                }

                // Have the orbs fire outward in a cone.
                if (OwnerAttackTimer == 75f)
                    NPC.velocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.ToRadians(-AngularDirection * Main.rand.NextFloat(38f, 48f))) * 22f;
                if (OwnerAttackTimer >= 105f && OwnerAttackTimer <= 120f)
                    NPC.velocity *= 0.86f;

                // Have both orbs charge at the player.
                if (OwnerAttackTimer == 120f || OwnerAttackTimer == 320f)
                    NPC.velocity = NPC.SafeDirectionTo(Target.Center) * 22f;

                // Perform collision/explosion logic and transition to the charge attack part.
                if (OwnerAttackTimer > 120f && OwnerAttackTimer < 240f && AngularDirection == 1f)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (!Main.npc[i].active || Main.npc[i].type != NPC.type || i == NPC.whoAmI)
                            continue;

                        if (!Main.npc[i].WithinRange(NPC.Center, NPC.scale * 50f))
                            continue;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter((Main.npc[i].Center + NPC.Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<EnergyBlast>(), 130, 0f);

                        NPC.velocity *= 0.2f;
                        NPC.netUpdate = true;
                        Main.npc[i].velocity *= 0.2f;
                        Main.npc[i].netUpdate = true;

                        OwnerAttackTimer = 240f;
                        Owner.netUpdate = true;
                        break;
                    }
                }

                if (OwnerAttackTimer >= 240f && OwnerAttackTimer <= 320f)
                    NPC.Center = Vector2.Lerp(NPC.Center, Target.Center + new Vector2(AngularDirection * 480f, -360f), 0.09f);

                // Explode into homing energy orbs if the two did not collide.
                if (Main.netMode != NetmodeID.MultiplayerClient && OwnerAttackTimer == 215f)
                {
                    for (int i = 0; i < 2; i++)
                        Utilities.NewProjectileBetter(NPC.Center, Main.rand.NextVector2CircularEdge(12f, 12f), ModContent.ProjectileType<HomingEnergyOrb>(), 110, 0f);
                }

                if (OwnerAttackTimer >= 320f && OwnerAttackTimer < 420f && AngularDirection == 1f)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (!Main.npc[i].active || Main.npc[i].type != NPC.type || i == NPC.whoAmI)
                            continue;

                        if (!Main.npc[i].WithinRange(NPC.Center, NPC.scale * 50f))
                            continue;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter((Main.npc[i].Center + NPC.Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<BigEnergyBlast>(), 155, 0f);
                        OwnerAttackTimer = 420f;
                        Owner.netUpdate = true;
                        break;
                    }
                }
            }
            else
            {
                AngularOffset += AngularDirection / 8f;
                NPC.Center = Owner.Center + AngularOffset.ToRotationVector2() * Vector2.One * 150f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2[] baseOldPositions = NPC.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToArray();
            if (baseOldPositions.Length <= 2)
                return true;

            Texture2D npcTexture = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = npcTexture.Size() * 0.5f;
            List<Vector2> adjustedOldPositions = new BezierCurve(baseOldPositions).GetPoints(30 + (int)NPC.Distance(NPC.oldPos.Last()) / 13);
            for (int i = 0; i < adjustedOldPositions.Count; i++)
            {
                float completionRatio = i / (float)adjustedOldPositions.Count;
                float scale = NPC.scale * (float)Math.Pow(MathHelper.Lerp(1f, 0.4f, completionRatio), 2D);
                Color drawColor = Color.Lerp(Color.Red, Color.Purple, completionRatio) * (1f - completionRatio) * 0.8f;
                Vector2 drawPosition = adjustedOldPositions[i] + NPC.Size * 0.5f - Main.screenPosition;
                spriteBatch.Draw(npcTexture, drawPosition, NPC.frame, drawColor, NPC.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Electrified, 90);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.timeLeft > 10 && projectile.damage > 0)
                projectile.Kill();
        }

        public override bool CheckActive() => false;
    }
}
