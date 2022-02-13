using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class AncientVisionBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.AncientCultistSquidhead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region AI
        public override bool PreAI(NPC npc)
        {
            int direction = (npc.ai[0] == 0f).ToDirectionInt();
            ref float attackTimer = ref npc.ai[1];
            ref float attackState = ref npc.ai[2];

            npc.noGravity = true;
            npc.noTileCollide = true;

            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest();
                updateAllOtherVisions();

                if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
                {
                    if (npc.timeLeft > 45)
                        npc.timeLeft = 45;
                    else
                        npc.Opacity = Utils.InverseLerp(4f, 45f, npc.timeLeft, true);
                }
                return false;
            }

            npc.Opacity = 1f;

            void updateAllOtherVisions()
            {
                // Only one NPC can perform this operation, to prevent overlap.
                if (direction != 1)
                    return;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type != npc.type || !Main.npc[i].active || i == npc.whoAmI)
                        continue;

                    Main.npc[i].target = npc.target;
                    Main.npc[i].ai[1] = npc.ai[1];
                    Main.npc[i].ai[2] = npc.ai[2];
                    Main.npc[i].netUpdate = true;
                }
            }

            Player target = Main.player[npc.target];

            switch ((int)attackState)
            {
                case 0:
                    Vector2 destination = target.Center + new Vector2(direction * 320f, -215f);
                    npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center), 0.1f);
                    npc.spriteDirection = -1;

                    npc.velocity = (npc.velocity * 4f + npc.SafeDirectionTo(destination) * 15f) / 5f;
                    if (npc.WithinRange(destination, 50f))
                    {
                        attackState = 1f;
                        attackTimer = 0f;
                        updateAllOtherVisions();
                        npc.netUpdate = true;
                    }
                    break;
                case 1:
                    if (attackState == 20f)
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 17f;

                    if (attackTimer < 20f)
                    {
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * -7f, 0.08f);
                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center), 0.1f);
                    }
                    else if (attackState < 125f)
                    {
                        if (!npc.WithinRange(target.Center, 160f))
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.035f);
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (BossRushEvent.BossRushActive ? 20f : 13f);

                        npc.rotation = npc.velocity.ToRotation();
                        npc.spriteDirection = (Math.Cos(npc.rotation) > 0f).ToDirectionInt();
                    }

                    if (attackTimer >= 125f)
                    {
                        npc.velocity *= 0.95f;
                        if (attackTimer >= 185f)
                        {
                            attackState = 0f;
                            attackTimer = 0f;
                            updateAllOtherVisions();
                            npc.netUpdate = true;
                        }
                    }
                    break;
            }
            attackTimer++;
            return false;
        }
        #endregion AI
    }
}
