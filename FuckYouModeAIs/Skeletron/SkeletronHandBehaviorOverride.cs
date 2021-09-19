using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Skeletron
{
	public class SkeletronHandBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.SkeletronHand;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            float armDirection = -npc.ai[0];
            NPC owner = Main.npc[(int)npc.ai[1]];
            float animationTime = owner.ai[2];
            if (!owner.active)
            {
                npc.active = false;
                return false;
            }
            float phaseChangeCountdown = owner.Infernum().ExtraAI[0];

            npc.life = npc.lifeMax = 200;
            npc.damage = npc.defDamage;
            npc.Calamity().DR = 0.4f;
            npc.dontTakeDamage = true;

            if (animationTime < 200f || phaseChangeCountdown > 0f)
            {
                Vector2 destination = owner.Center + new Vector2(armDirection * 125f, -285f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 17f, 0.3f);
                npc.damage = 0;

                npc.rotation = npc.AngleTo(destination - Vector2.UnitY * 25f) - MathHelper.PiOver2;
            }
            else
            {
                int ownerAttackState = (int)owner.ai[0];
                float attackTimer = owner.ai[1];
                Player target = Main.player[owner.target];

                switch (ownerAttackState)
                {
                    case 0:
                        Vector2 destination = owner.Center + new Vector2(armDirection * 200f, -230f);
                        if (npc.Center.Y > destination.Y)
                        {
                            if (npc.velocity.Y > 0f)
                                npc.velocity.Y *= 0.92f;
                            npc.velocity.Y -= 0.125f;
                            if (npc.velocity.Y > 1.5f)
                                npc.velocity.Y = 1.5f;
                        }
                        else if (npc.Center.Y < destination.Y)
                        {
                            if (npc.velocity.Y < 0f)
                                npc.velocity.Y *= 0.92f;
                            npc.velocity.Y += 0.125f;
                            if (npc.velocity.Y < -1.5f)
                                npc.velocity.Y = -1.5f;
                        }

                        if (npc.Center.X > destination.X)
                        {
                            if (npc.velocity.X > 0f)
                                npc.velocity.X *= 0.92f;
                            npc.velocity.X -= 0.2f;
                            if (npc.velocity.X > 3.5f)
                                npc.velocity.X = 3.5f;
                        }

                        if (npc.Center.X < destination.X)
                        {
                            if (npc.velocity.X < 0f)
                                npc.velocity.X *= 0.92f;
                            npc.velocity.X += 0.2f;
                            if (npc.velocity.X < -3.5f)
                                npc.velocity.X = -3.5f;
                        }
                        break;
                    case 1:
                        Vector2 idealPosition = owner.Center + new Vector2(armDirection * 200f, -230f);

                        bool facingPlayer = armDirection == (target.Center.X > npc.Center.X).ToDirectionInt();
                        float adjustedTimer = attackTimer % 160f;
                        if (facingPlayer && adjustedTimer > 65f && adjustedTimer < 140f)
                        {
                            float swipeAngularOffset = MathHelper.Lerp(-0.6f, 1.22f, Utils.InverseLerp(90f, 140f, adjustedTimer, true));
                            idealPosition = owner.Center + owner.SafeDirectionTo(target.Center).RotatedBy(swipeAngularOffset) * 250f;
                        }

                        npc.Center = Vector2.Lerp(npc.Center, idealPosition, 0.05f);
                        npc.Center = npc.Center.MoveTowards(idealPosition, 5f);
                        npc.velocity = Vector2.Zero;

                        if (Main.netMode != NetmodeID.MultiplayerClient && facingPlayer && adjustedTimer > 90f && adjustedTimer < 140f && adjustedTimer % 5f == 4f)
                        {
                            Vector2 skullSpawnPosition = npc.Center;
                            Vector2 skullShootVelocity = (skullSpawnPosition - owner.Center).SafeNormalize(Vector2.UnitY) * 6f;
                            skullSpawnPosition += skullShootVelocity * 4f;
                            Utilities.NewProjectileBetter(skullSpawnPosition, skullShootVelocity, ModContent.ProjectileType<NonHomingSkull>(), 105, 0f);
                        }

                        break;
                    case 2:
                        destination = owner.Center + new Vector2(armDirection * 200f, -230f);
                        npc.Center = Vector2.Lerp(npc.Center, destination, 0.035f);
                        npc.Center = npc.Center.MoveTowards(destination, 5f);
                        break;
                    case 3:
                        destination = owner.Center + new Vector2(armDirection * 460f, 360f);
                        npc.Center = Vector2.Lerp(npc.Center, destination, 0.05f);
                        npc.Center = npc.Center.MoveTowards(destination, 5f);

                        adjustedTimer = attackTimer % 180f;
                        if (adjustedTimer > 50f && adjustedTimer < 170f && adjustedTimer % 40f == 39f)
                        {
                            Main.PlaySound(SoundID.DD2_BetsyFireballShot, npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    Vector2 flameShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 5f) * 12f;
                                    Utilities.NewProjectileBetter(npc.Center, flameShootVelocity, ModContent.ProjectileType<ShadowflameFireball>(), 95, 0f);
                                }
                            }
                        }
                        break;
                }

                if (npc.velocity.Length() < owner.velocity.Length() * 0.4f)
                    npc.velocity = owner.velocity * 0.4f;

                npc.rotation = npc.AngleFrom(owner.Center) - MathHelper.PiOver2;
            }

            return false;
        }
    }
}
