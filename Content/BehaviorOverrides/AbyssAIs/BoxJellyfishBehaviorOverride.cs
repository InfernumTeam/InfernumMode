using CalamityMod.NPCs.Abyss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class BoxJellyfishBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<BoxJellyfish>();

        #region Fields
        public enum AttackType
        {
            SwimAimlessly,
            Attack
        }

        public const int MinimumSchoolSize = 2;
        public const int MaximumSchoolSize = 4;
        public const int AttackStateIndex = 0;
        public const int NPCWhoAmIIndex = 1;
        public const int PlayerWhoAmIIndex = 2;
        public const int RandomMoveTimeIndex = 1;
        public const int ZapCooldownIndex = 2;
        #endregion

        #region AI
        public override bool PreAI(NPC npc)
        {
            ref float attackTimer = ref npc.ai[1];
            ref float attackState = ref npc.Infernum().ExtraAI[AttackStateIndex];
            ref float zapCooldown = ref npc.Infernum().ExtraAI[ZapCooldownIndex];

            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 60;
            NPCID.Sets.UsesNewTargetting[npc.type] = true;

            // Define rotation.
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection * PiOver2);
            if (npc.spriteDirection == -1)
                npc.rotation += Pi;

            if ((AttackType)attackState is AttackType.SwimAimlessly)
            {
                NPCAimedTarget target = npc.GetTargetData();

                if (!target.Invalid)
                {
                    attackState = 1;
                    attackTimer = 0;
                }
            }
            // Mark us as a predator.
            npc.Infernum().IsAbyssPredator = true;

            // Spawn other jellys along with this one.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[0] == 0f && npc.wet)
            {
                Utilities.SpawnSchoolOfFish(npc, MinimumSchoolSize, MaximumSchoolSize);
                return false;
            }

            // Constantly slow down over time
            npc.velocity *= 0.98f;

            // Reduce the zap cooldown if needed.
            if (zapCooldown > 0f)
                zapCooldown--;

            switch ((AttackType)attackState)
            {
                case AttackType.SwimAimlessly:
                    DoBehavior_SwimAimlessly(npc, ref attackTimer);
                    break;
                case AttackType.Attack:
                    DoBehavior_Attack(npc, ref attackTimer, npc.GetTargetData(), ref zapCooldown);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SwimAimlessly(NPC npc, ref float attackTimer)
        {
            int directionChangeDelay = 120;
            int playerSearchDistance = 200;
            ref float moveTime = ref npc.Infernum().ExtraAI[RandomMoveTimeIndex];

            // Initialize the random move delay on the first frame.
            if (attackTimer is 0)
            {
                float random = Main.rand.NextFloat(0.8f, 1.2f);
                moveTime = (int)(directionChangeDelay * random);
            }

            // Change direction periodically
            if (attackTimer % moveTime == 0)
            {
                Vector2 velocity = Vector2.One.RotatedByRandom(TwoPi) * Main.rand.NextFloat(3, 6);

                Vector2 ahead = npc.Center + velocity * 40f;
                bool aboutToLeaveWorld = ahead.X >= Main.maxTilesX * 16f - 700f || ahead.X < 700f;
                bool shouldTurnAround = aboutToLeaveWorld;

                for (float x = -0.47f; x < 0.47f; x += 0.06f)
                {
                    Vector2 checkDirection = npc.velocity.SafeNormalize(Vector2.Zero).RotatedBy(x);
                    if (!Collision.CanHit(npc.Center, 1, 1, npc.Center + checkDirection * 125f, 1, 1) ||
                        !Collision.WetCollision(npc.Center + checkDirection * 50f, npc.width, npc.height))
                    {
                        shouldTurnAround = true;
                        break;
                    }
                }

                if (shouldTurnAround)
                    Utilities.TurnAroundBehavior(npc, ahead, aboutToLeaveWorld);
                else
                    npc.velocity = velocity;
            }

            // Check for a player constantly.
            Utilities.TargetClosestAbyssPredator(npc, false, 675f, playerSearchDistance);
            NPCAimedTarget target = npc.GetTargetData();
            if (target.Type == NPCTargetType.Player && !npc.WithinRange(target.Center, playerSearchDistance))
                target.Type = NPCTargetType.None;
        }

        public static void DoBehavior_Attack(NPC npc, ref float attackTimer, NPCAimedTarget target, ref float zapCooldown)
        {
            int directionChangeDelay = 120;
            ref float moveTime = ref npc.Infernum().ExtraAI[RandomMoveTimeIndex];

            // Initialize the random move delay on the first frame.
            if (attackTimer is 0)
            {
                float random = Main.rand.NextFloat(0.8f, 1.2f);
                moveTime = (int)(directionChangeDelay * random);
            }

            // Go back to swimming if the target is invalid.
            if (target.Invalid)
            {
                npc.Infernum().ExtraAI[AttackStateIndex] = 0;
                attackTimer = 0;
                return;
            }

            // Move towards the target.
            if (attackTimer % moveTime == moveTime - 1)
                npc.velocity = npc.SafeDirectionTo(target.Center) * Main.rand.NextFloat(7, 12);

            // If close enough to the target, release a shock.
            bool anyZaps = Main.projectile.Any((Projectile p) => p.type == ModContent.ProjectileType<BoxJellyZap>() && p.active && p.ai[0] != npc.whoAmI);
            if (npc.Center.Distance(target.Center) < 100 && !anyZaps)
            {
                if (zapCooldown is 0)
                {
                    Projectile zap = Projectile.NewProjectileDirect(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BoxJellyZap>(), 60, 0, Main.myPlayer, npc.whoAmI);
                    zapCooldown = zap.timeLeft + 180;
                }
            }

            // If the target is suitably far away, return to swimming.
            if (npc.Center.Distance(target.Center) > 500)
            {
                npc.target = 0;
                npc.Infernum().ExtraAI[AttackStateIndex] = 0;
                attackTimer = 0;
                return;
            }
        }
        #endregion
    }
}
