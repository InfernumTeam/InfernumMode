namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Paladin
{
    /*
    public class PaladinBehaviorOverride : NPCBehaviorOverride
    {
        public enum PaladinAttackType
        {
            ClearStupidRandomEnemies
        }

        public override int NPCOverrideType => NPCID.Paladin;

        public override bool PreAI(NPC npc)
        {
            // Select a target as necessary.
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];

            // Claim priority in the NPC slot system, to prevent swarms of regular enemies getting in the way of the fight.
            npc.npcSlots = 35f;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            switch ((PaladinAttackType)attackType)
            {
                case PaladinAttackType.ClearStupidRandomEnemies:
                    DoBehavior_ClearStupidRandomEnemies(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;

            return false;
        }

        public static void DoBehavior_ClearStupidRandomEnemies(NPC npc, Player target, ref float attackTimer)
        {
            int fadeInTime = 45;
            int jumpDelay = fadeInTime + 30;
            float jumpSpeed = 16f;
            float enemyKillRange = 2400f;

            // On the first frame, try to teleport near the target, to ensure that the animation can be seen.
            // If no spot could be found, disappear without any explanation.
            if (attackTimer == 1f)
            {
                bool foundValidTeleportPosition = false;
                Vector2 randomTeleportPosition = npc.Bottom;
                for (int i = 0; i < 2500; i++)
                {
                    randomTeleportPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(200f, i * 3f + 650f);
                    Point randomTilePosition = randomTeleportPosition.ToTileCoordinates();

                    // Ensure that the teleport position is not covered with tiles. If it is, try again.
                    if (Collision.SolidCollision(randomTeleportPosition - npc.Size * new Vector2(0.5f, 1f), npc.width, npc.height))
                        continue;

                    // Ensure that the teleport position has ground for the Paladin to stand on.
                    bool hasGround = false;
                    for (int dx = -3; dx < 3; dx++)
                    {
                        Point groundPosition = new(randomTilePosition.X + dx, randomTilePosition.Y + 1);
                        if (WorldGen.SolidTile(groundPosition))
                        {
                            hasGround = true;
                            break;
                        }
                    }

                    if (!hasGround)
                        continue;

                    // Ensure that the teleport position doesn't have a wall between itself and the target.
                    if (!Collision.CanHitLine(randomTeleportPosition - Vector2.UnitY * npc.height * 0.5f, 1, 1, target.Center, 1, 1))
                        continue;

                    // If the above filters didn't invalidate the teleport position then it is valid and can be teleported to.
                    foundValidTeleportPosition = true;
                    break;
                }

                if (!foundValidTeleportPosition)
                {
                    npc.active = false;
                    return;
                }

                // Teleport to the designated position.
                npc.Bottom = randomTeleportPosition;

                // Look at the target.
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                // Cancel any and all horizontal movement.
                npc.velocity.X = 0f;
                
                // Sync the aforementioned changes.
                npc.netUpdate = true;
            }

            // Fade in after the teleport.
            npc.Opacity = Utils.GetLerpValue(1f, fadeInTime, attackTimer, true);

            // Jump into the air. Once ground is reached stray enemies will be killed.
            if (attackTimer == jumpDelay)
            {
                npc.velocity.X = 0f;
                npc.velocity.Y = -jumpSpeed;
                npc.netUpdate = true;
            }

            // Kill any nearby stray enemies and create stomp effects.
            if (attackTimer >= jumpDelay + 1f && npc.collideX)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    bool enemyCanBeKilled = CalamityLists.dungeonEnemyBuffList.Contains(n.type) && n.type != npc.type && n.active && n.WithinRange(npc.Center, enemyKillRange);

                    // Enemies can drop loot if they've been hit already, to ensure that players aren't cheated out of any loot they were hoping to get.
                    bool enemyCanDropLoot = n.life < n.lifeMax;

                    // Skip over enemies that shouldn't be killed in the first place.
                    if (!enemyCanBeKilled)
                        continue;

                    n.life = 0;
                    n.HitEffect();

                    if (enemyCanDropLoot)
                        n.NPCLoot();

                    n.active = false;
                }
                
                Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitY * -3f, ProjectileID.DD2OgreSmash, 0, 0f);
            }
        }
    }
    */
}
