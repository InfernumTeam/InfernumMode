using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.SandElemental
{
    public class SandElementalBehaviorOverride : NPCBehaviorOverride
    {
        public enum SandElementalAttackState
        {
            HoverMovement,
            TornadoSlam,
            SandBursts
        }

        public override int NPCOverrideType => NPCID.SandElemental;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            // Pick a target.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Go through tiles.
            npc.noTileCollide = true;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentFrame = ref npc.localAI[0];

            switch ((SandElementalAttackState)(int)attackState)
            {
                case SandElementalAttackState.HoverMovement:
                    DoBehavior_HoverMovement(npc, target, ref currentFrame, ref attackTimer);
                    break;
                case SandElementalAttackState.TornadoSlam:
                    DoBehavior_TornadoSlam(npc, target, ref currentFrame, ref attackTimer);
                    break;
                case SandElementalAttackState.SandBursts:
                    DoBehavior_SandBursts(npc, target, ref currentFrame, ref attackTimer);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoBehavior_HoverMovement(NPC npc, Player target, ref float currentFrame, ref float attackTimer)
        {
            int hoverTime = 180;

            // Rise if ground is close.
            float distanceToGround = MathHelper.Distance(Utilities.GetGroundPositionFrom(npc.Center).Y, npc.Center.Y);
            if (distanceToGround < 64f)
                npc.directionY = -1;

            // Descend if ground is far.
            if (distanceToGround > 92f)
                npc.directionY = 1;

            float verticalSpeed = npc.directionY == 1 ? 0.075f : -0.13f;
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + verticalSpeed, -2.4f, 2.4f);
            if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                npc.position.Y -= 5f;

            // Attempt to move horizontally towards the target.
            if (MathHelper.Distance(npc.Center.X, target.Center.X) < 150f)
                npc.velocity.X *= 0.98f;
            else
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, npc.SafeDirectionTo(target.Center).X * 9f, 0.075f);

            // Decide the direction and rotation.
            npc.rotation = npc.velocity.X * 0.02f;
            if (Math.Abs(npc.velocity.X) > 0.2f)
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

            // Decide frames.
            currentFrame = MathHelper.Lerp(0f, 3f, attackTimer / 35f % 1f);

            if (attackTimer >= hoverTime)
            {
                npc.ai[0] = (int)Utils.SelectRandom(Main.rand, SandElementalAttackState.TornadoSlam, SandElementalAttackState.SandBursts);
                attackTimer = 0f;
            }
        }

        public static void DoBehavior_TornadoSlam(NPC npc, Player target, ref float currentFrame, ref float attackTimer)
        {
            int animationChangeTime = 40;
            int castTime = 48;

            // Decide the direction and rotation.
            npc.rotation = npc.velocity.X * 0.02f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Slow down and perform a cast animation.
            npc.velocity *= 0.96f;
            npc.rotation = npc.velocity.X * 0.02f;

            if (attackTimer < animationChangeTime)
                currentFrame = MathHelper.Lerp(4f, 10f, attackTimer / animationChangeTime);
            if (attackTimer >= animationChangeTime)
            {
                float castInterpolant = Utils.InverseLerp(0f, castTime, attackTimer - animationChangeTime, true);
                int frameIncrement = (int)Math.Round(MathHelper.Lerp(0f, 3f, castInterpolant));

                // Create sandnadoes.
                if (attackTimer == animationChangeTime + castTime / 2)
                {
                    Main.PlaySound(SoundID.Item100, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 sandnadoSpawnPosition = target.Center + Vector2.UnitX * i * -540f;
                            int sandnado = Utilities.NewProjectileBetter(sandnadoSpawnPosition, Vector2.Zero, ModContent.ProjectileType<Sandnado2>(), 125, 0f);
                            if (Main.projectile.IndexInRange(sandnado))
                                Main.projectile[sandnado].ai[1] = i;
                        }
                    }
                }

                // Make sandnadoes slam into each-other.
                if (attackTimer >= animationChangeTime + castTime / 2 + 5f && attackTimer <= animationChangeTime + castTime)
                {
                    float sandnadoSpeedInterpolant = Utils.InverseLerp(castTime / 2 + 5f, animationChangeTime + castTime, attackTimer, true);
                    float sandnadoSpeed = MathHelper.Lerp(0f, 16f, (float)Math.Pow(sandnadoSpeedInterpolant, 4D));
                    foreach (Projectile sandnado in Utilities.AllProjectilesByID(ModContent.ProjectileType<Sandnado2>()))
                        sandnado.velocity = Vector2.UnitX * sandnado.ai[1] * sandnadoSpeed;
                }

                // Emit dust on the hands.
                if (frameIncrement < 2)
                {
                    int dustCount = frameIncrement >= 1 ? 4 : 2;
                    for (int j = -1; j <= 1; j += 2)
                    {
                        // Create spirit flames.
                        Vector2 handPosition = npc.Center + new Vector2(j * 26f, -60f).RotatedBy(npc.rotation);
                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 5f == 4f)
                        {
                            Vector2 shootVelocity = (target.Center - handPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.81f);
                            shootVelocity = Vector2.Lerp(shootVelocity, -Vector2.UnitY, 0.45f) * 10f;
                            int fuck = Utilities.NewProjectileBetter(handPosition, shootVelocity, ProjectileID.DesertDjinnCurse, 120, 0f);
                            Main.projectile[fuck].ai[0] = target.whoAmI;
                        }

                        for (int i = 0; i < dustCount; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(handPosition, Main.rand.NextBool(3) ? 32 : 27);
                            dust.velocity = (dust.position - npc.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.5f) * Main.rand.NextFloat(1.1f, 4f);
                            dust.scale *= Main.rand.NextFloat(1.1f, 1.56f);
                            dust.noGravity = true;
                        }
                    }
                }

                switch (frameIncrement)
                {
                    case 0:
                        currentFrame = 10f;
                        break;
                    case 1:
                        currentFrame = 11f;
                        break;
                    case 2:
                        currentFrame = 9f;
                        break;
                    case 3:
                        currentFrame = MathHelper.Lerp(5f, 8f, attackTimer / 30f % 1f);
                        break;
                }
            }

            if (attackTimer >= animationChangeTime + castTime)
            {
                npc.ai[0] = (int)SandElementalAttackState.HoverMovement;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SandBursts(NPC npc, Player target, ref float currentFrame, ref float attackTimer)
        {
            int castPrepationTime = 48;
            int totalBursts = 2;

            // Slow down and perform a cast animation.
            npc.velocity *= 0.96f;
            npc.rotation = npc.velocity.X * 0.02f;
            currentFrame = MathHelper.Lerp(4f, 12f, attackTimer / castPrepationTime % 1f);

            Vector2 sandBallSpawnPosition = npc.Center - Vector2.UnitY * 60f;
            float wrappedAttackTimer = attackTimer % castPrepationTime;

            if (wrappedAttackTimer >= castPrepationTime - 32f && wrappedAttackTimer < castPrepationTime - 10f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust dust = Dust.NewDustPerfect(sandBallSpawnPosition, Main.rand.NextBool() ? 274 : 32);
                    dust.position += Main.rand.NextVector2Square(-32f, 32f);
                    dust.velocity = -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 4f);
                    dust.scale *= 1.2f;
                    dust.noGravity = true;
                }
            }

            if (wrappedAttackTimer == castPrepationTime - 10f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float offsetAngle = MathHelper.Lerp(-0.27f, 0.27f, i / 2f);
                    Vector2 shootVelocity = (target.Center - sandBallSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * 11f;
                    Utilities.NewProjectileBetter(sandBallSpawnPosition, shootVelocity, ModContent.ProjectileType<SandFlameBall>(), 125, 0f);
                }
            }

            if (attackTimer >= castPrepationTime * totalBursts)
            {
                npc.ai[0] = (int)SandElementalAttackState.HoverMovement;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(frameHeight * Math.Round(npc.localAI[0]));
        }
    }
}
