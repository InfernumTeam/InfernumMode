using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeLaserBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeLaser;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region AI
        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float hoverDirection = npc.ai[0];
            float ownerIndex = npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float shouldBeInactiveFlag = ref npc.ai[3];

            if (!Main.npc.IndexInRange((int)ownerIndex) || !Main.npc[(int)ownerIndex].active)
            {
                npc.life = 0;
                npc.StrikeNPCNoInteraction(9999, 0f, 0);
                npc.netUpdate = true;
                return false;
            }

            NPC owner = Main.npc[(int)ownerIndex];
            npc.target = owner.target;

            Player target = Main.player[npc.target];

            // Disable contact damage.
            npc.damage = 0;

            bool shouldBeInactive = PrimeHeadBehaviorOverride.ShouldBeInactive(npc.type, owner.ai[2]);
            Vector2 hoverDestination = owner.Center + new Vector2(hoverDirection * -180f, shouldBeInactive ? 260f : -150f);
            if (shouldBeInactive)
                hoverDestination += owner.velocity * 4f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 20f, shouldBeInactive ? 0.07f : 0.18f);
            if (!npc.WithinRange(hoverDestination, 450f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 20f, 0.1f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 2f);
            }

            shouldBeInactiveFlag = shouldBeInactive.ToInt();
            if (shouldBeInactive)
            {
                attackTimer = 0f;
                PrimeHeadBehaviorOverride.ArmHoverAI(npc);
                return false;
            }

            attackTimer++;
            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.08f);
            Lighting.AddLight(npc.Center, Color.Red.ToVector3() * 1.4f);

            int shootRate = 70 - (4 - PrimeHeadBehaviorOverride.RemainingArms) * 18;

            if (attackTimer >= shootRate)
            {
                Main.PlaySound(SoundID.Item33, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootSpeed = BossRushEvent.BossRushActive ? 20.5f : 13.5f;
                    if (lifeRatio < 0.5f || PrimeHeadBehaviorOverride.RemainingArms <= 2)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.49f, 0.49f, i / 2f)) * shootSpeed;
                            Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 4f, laserShootVelocity, ModContent.ProjectileType<LaserBolt>(), 135, 0f);
                        }
                    }
                    else
                    {
                        Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 4f, laserShootVelocity, ModContent.ProjectileType<LaserBolt>(), 130, 0f);
                    }
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            return false;
        }

        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            spriteBatch.Draw(Main.npcTexture[npc.type], drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            if (npc.ai[3] == 0f)
                spriteBatch.Draw(Main.BoneLaserTexture, drawPosition, npc.frame, new Color(200, 200, 200, 0), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Drawing
    }
}