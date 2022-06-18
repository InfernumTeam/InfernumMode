using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class EnergyOrb : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public NPC Owner => Main.npc[(int)npc.ai[0]];
        public ref float AngularDirection => ref npc.ai[1];
        public ref float AngularOffset => ref npc.ai[2];
        internal ref float OwnerAttackTimer => ref Owner.Infernum().ExtraAI[10];
        internal TwinsAttackSynchronizer.RetinazerAttackState OwnerAttackState => (TwinsAttackSynchronizer.RetinazerAttackState)(int)Owner.Infernum().ExtraAI[11];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy Orb");
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = npc.height = 40;
            npc.damage = 120;
            npc.lifeMax = 1500000;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.netAlways = true;
            npc.alpha = 255;
            npc.scale = 1.15f;
        }

        public override void AI()
        {
            npc.life = npc.lifeMax;
            if (!Main.npc.IndexInRange((int)npc.ai[0]) || !Owner.active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            npc.target = Owner.target;
            npc.timeLeft = 3600;
            npc.damage = npc.defDamage;
            npc.rotation += MathHelper.ToRadians(16f * AngularDirection);

            if (OwnerAttackState == TwinsAttackSynchronizer.RetinazerAttackState.DanceOfLightnings)
            {
                AngularOffset += AngularDirection / 8f * Utils.InverseLerp(60f, 30f, OwnerAttackTimer, true);

                if (OwnerAttackTimer < 75f)
                {
                    if (OwnerAttackTimer > 60f)
                        AngularOffset = AngularOffset.AngleLerp(Target.AngleTo(Target.Center) + MathHelper.PiOver2 * AngularDirection - MathHelper.PiOver2, 0.25f);
                    npc.Center = Vector2.Lerp(npc.Center, Owner.Center + AngularOffset.ToRotationVector2() * Vector2.One * 150f, 0.21f);
                    npc.damage = 0;
                }

                // Have the orbs fire outward in a cone.
                if (OwnerAttackTimer == 75f)
                    npc.velocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.ToRadians(-AngularDirection * Main.rand.NextFloat(38f, 48f))) * 22f;
                if (OwnerAttackTimer >= 105f && OwnerAttackTimer <= 120f)
                    npc.velocity *= 0.86f;

                // Have both orbs charge at the player.
                if (OwnerAttackTimer == 120f || OwnerAttackTimer == 320f)
                    npc.velocity = npc.SafeDirectionTo(Target.Center) * 22f;

                // Perform collision/explosion logic and transition to the charge attack part.
                if (OwnerAttackTimer > 120f && OwnerAttackTimer < 240f && AngularDirection == 1f)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (!Main.npc[i].active || Main.npc[i].type != npc.type || i == npc.whoAmI)
                            continue;

                        if (!Main.npc[i].WithinRange(npc.Center, npc.scale * 50f))
                            continue;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter((Main.npc[i].Center + npc.Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<EnergyBlast>(), 130, 0f);

                        npc.velocity *= 0.2f;
                        npc.netUpdate = true;
                        Main.npc[i].velocity *= 0.2f;
                        Main.npc[i].netUpdate = true;

                        OwnerAttackTimer = 240f;
                        Owner.netUpdate = true;
                        break;
                    }
                }

                if (OwnerAttackTimer >= 240f && OwnerAttackTimer <= 320f)
                    npc.Center = Vector2.Lerp(npc.Center, Target.Center + new Vector2(AngularDirection * 480f, -360f), 0.09f);

                // Explode into homing energy orbs if the two did not collide.
                if (Main.netMode != NetmodeID.MultiplayerClient && OwnerAttackTimer == 215f)
                {
                    for (int i = 0; i < 2; i++)
                        Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(12f, 12f), ModContent.ProjectileType<HomingEnergyOrb>(), 110, 0f);
                }

                if (OwnerAttackTimer >= 320f && OwnerAttackTimer < 420f && AngularDirection == 1f)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (!Main.npc[i].active || Main.npc[i].type != npc.type || i == npc.whoAmI)
                            continue;

                        if (!Main.npc[i].WithinRange(npc.Center, npc.scale * 50f))
                            continue;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter((Main.npc[i].Center + npc.Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<BigEnergyBlast>(), 155, 0f);
                        OwnerAttackTimer = 420f;
                        Owner.netUpdate = true;
                        break;
                    }
                }
            }
            else
            {
                AngularOffset += AngularDirection / 8f;
                npc.Center = Owner.Center + AngularOffset.ToRotationVector2() * Vector2.One * 150f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2[] baseOldPositions = npc.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToArray();
            if (baseOldPositions.Length <= 2)
                return true;

            Texture2D npcTexture = Main.npcTexture[npc.type];
            Vector2 origin = npcTexture.Size() * 0.5f;
            List<Vector2> adjustedOldPositions = new BezierCurve(baseOldPositions).GetPoints(30 + (int)npc.Distance(npc.oldPos.Last()) / 13);
            for (int i = 0; i < adjustedOldPositions.Count; i++)
            {
                float completionRatio = i / (float)adjustedOldPositions.Count;
                float scale = npc.scale * (float)Math.Pow(MathHelper.Lerp(1f, 0.4f, completionRatio), 2D);
                Color drawColor = Color.Lerp(Color.Red, Color.Purple, completionRatio) * (1f - completionRatio) * 0.8f;
                Vector2 drawPosition = adjustedOldPositions[i] + npc.Size * 0.5f - Main.screenPosition;
                spriteBatch.Draw(npcTexture, drawPosition, npc.frame, drawColor, npc.rotation, origin, scale, SpriteEffects.None, 0f);
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
