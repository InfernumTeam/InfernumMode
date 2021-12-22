using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ThanatosHeadBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using System.Linq;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        // The Nuke's AI is not changed by this system because there's literally nothing you can do with it beyond its
        // normal behavior of shooting and reloading gauss nukes.
        public static Dictionary<ExoMechComboAttackType, int[]> AffectedAresArms => new Dictionary<ExoMechComboAttackType, int[]>()
        {
            [ExoMechComboAttackType.ThanatosAres_ExplosionCircle] = new int[] { ModContent.NPCType<AresTeslaCannon>(), 
                                                                                ModContent.NPCType<AresPlasmaFlamethrower>() },
        };

        public static bool ArmCurrentlyBeingUsed(NPC npc)
        {
            // Return false Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            if (AffectedAresArms.TryGetValue((ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return activeArms.Contains(npc.type);
            return false;
        }

        public static bool UseThanatosAresComboAttack(NPC npc, ref float attackTimer, ref float frame)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.ThanatosAres_ExplosionCircle:
                    return DoBehavior_ThanatosAres_ExplosionCircle(npc, target, ref attackTimer, ref frame);
            }
            return false;
        }

        public static bool DoBehavior_ThanatosAres_ExplosionCircle(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 180;

            // Thanatos spins around the target with its head always open.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                Vector2 spinDestination = target.Center + (attackTimer * MathHelper.TwoPi / 120f).ToRotationVector2() * 2600f;
                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f);
                if (npc.WithinRange(spinDestination, 40f))
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards((attackTimer + 8f) * MathHelper.TwoPi / 120f + MathHelper.PiOver2, 0.25f);

                frame = (int)ThanatosFrameType.Open;
            }

            // Ares' body hovers above the player, slowly moving back and forth horizontally.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 400f;
                if (attackTimer > attackDelay)
                    hoverDestination.X += (float)Math.Sin((attackTimer - attackDelay) * MathHelper.TwoPi / 180f) * 180f;
            }

            // Ares' plasma arm releases bursts of plasma that slow down and explode.
            if (npc.type == ModContent.NPCType<AresPlasmaFlamethrower>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 15f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 66f + Vector2.UnitY * 16f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }
            }

            return false;
        }
    }
}
