using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class AncientVisionBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.AncientCultistSquidhead;

        #region AI
        public override bool PreAI(NPC npc)
        {
            npc.TargetClosest(false);
            int idealDirection = (npc.velocity.X > 0).ToDirectionInt();
            npc.spriteDirection = idealDirection;
            npc.noTileCollide = true;

            // Disable natural despawning.
            npc.Infernum().DisableNaturalDespawning = true;

            Player target = Main.player[npc.target];

            ref float direction = ref npc.ai[0];
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];

            switch ((int)attackState)
            {
                // Rise upward.
                case 0:
                    Vector2 flyDestination = target.Center + new Vector2(direction * 400f, -240f);
                    Vector2 idealVelocity = npc.SafeDirectionTo(flyDestination) * 12f;
                    npc.velocity = (npc.velocity * 29f + idealVelocity) / 29f;
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 1.5f);

                    // Decide rotation.
                    npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection > 0).ToInt() * MathHelper.Pi;

                    if (npc.WithinRange(flyDestination, 40f) || attackTimer > 150f)
                    {
                        attackState = 1f;
                        npc.velocity *= 0.65f;
                        npc.netUpdate = true;
                    }
                    break;

                // Slow down and look at the target.
                case 1:
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity *= 0.96f;
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.25f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + (npc.spriteDirection > 0).ToInt() * MathHelper.Pi, 0.2f);

                    // Charge once sufficiently slowed down.
                    float chargeSpeed = 23f;
                    if (npc.velocity.Length() < 1.25f)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                        for (int i = 0; i < 36; i++)
                        {
                            Dust magic = Dust.NewDustPerfect(npc.Center, 267);
                            magic.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 6f;
                            magic.scale = 1.1f;
                            magic.color = Color.Yellow;
                            magic.noGravity = true;
                        }

                        attackState = 2f;
                        attackTimer = 0f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }
                    break;

                // Charge and swoop.
                case 2:
                    float angularTurnSpeed = MathHelper.Pi / 300f;
                    idealVelocity = npc.SafeDirectionTo(target.Center);
                    Vector2 leftVelocity = npc.velocity.RotatedBy(-angularTurnSpeed);
                    Vector2 rightVelocity = npc.velocity.RotatedBy(angularTurnSpeed);
                    if (leftVelocity.AngleBetween(idealVelocity) < rightVelocity.AngleBetween(idealVelocity))
                        npc.velocity = leftVelocity;
                    else
                        npc.velocity = rightVelocity;

                    // Decide rotation.
                    npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection > 0).ToInt() * MathHelper.Pi;

                    if (attackTimer > 50f)
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 8f, 0.14f);
                        npc.netUpdate = true;
                    }
                    break;
            }
            attackTimer++;
            return false;
        }
        #endregion AI
    }
}
