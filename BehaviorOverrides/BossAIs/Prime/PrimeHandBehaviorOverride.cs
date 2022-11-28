using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Prime.PrimeHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public abstract class PrimeHandBehaviorOverride : NPCBehaviorOverride
    {
        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            int headIndex = (int)npc.ai[1];
            bool isActive = CannonCanBeUsed(npc, out float telegraphInterpolant);

            // Disapepar if the head is not present.
            if (headIndex < 0 || headIndex >= Main.maxNPCs || !Main.npc[headIndex].active)
            {
                npc.active = false;
                return false;
            }

            NPC head = Main.npc[headIndex];
            PrimeAttackType attackState = (PrimeAttackType)head.ai[0];
            GetCannonAttributesByAttack(attackState, out int telegraphTime, out _, out _);

            float wrappedAttackTimer = head.Infernum().ExtraAI[CannonCycleTimerIndex] - telegraphTime;
            ref float telegraphIntensity = ref npc.localAI[0];

            // Reset damage. Why do the cannon and laser do contact damage at all?
            npc.damage = npc.defDamage;
            if (npc.type is NPCID.PrimeCannon or NPCID.PrimeLaser)
                npc.damage = 0;

            // Inherit HP from the cannon.
            if (npc.realLife >= 0)
                npc.life = Main.npc[npc.realLife].life;

            // Inherit attributes from the head.
            npc.target = head.target;
            Player target = Main.player[npc.target];

            Vector2 hoverDestination = head.Center + ArmPositionOrdering[npc.type] * new Vector2(1f, (head.Center.Y < target.Center.Y).ToDirectionInt());
            Vector2 aimDestination = target.Center + target.velocity * PredictivenessFactor;

            // Cast telegraphs.
            npc.localAI[0] = telegraphInterpolant;
            if (telegraphInterpolant > 0f)
            {
                // Fly approximately near the hover position, with increasing precision as the telegraphs get stronger.
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.06f);

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 33f;
                Vector2 hyperfastVelocity = Vector2.Zero.MoveTowards(((hoverDestination - npc.Center) / 12f).ClampMagnitude(20f, 80f), 80f);
                idealVelocity = Vector2.Lerp(idealVelocity, hyperfastVelocity, telegraphInterpolant * 0.8f);

                npc.SimpleFlyMovement(idealVelocity, 0.4f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, telegraphInterpolant * 0.2f);
                if (MathHelper.Distance(npc.Center.X, hoverDestination.X) > 180f)
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, idealVelocity.X, 0.05f);

                // Aim at the target.
                AimAtTarget(npc, aimDestination);

                Vector2 cannonDirection = (npc.rotation + MathHelper.PiOver2).ToRotationVector2();
                PerformTelegraphBehaviors(npc, attackState, telegraphIntensity, cannonDirection);

                // Arms that are busy telegraphing do not do damage.
                npc.damage = 0;
            }

            // Dangle about if not active and not occupied with telegraphs.
            else if (!isActive)
            {
                ref float angularVelocity = ref npc.localAI[1];

                // Have angular velocity rely on exponential momentum.
                angularVelocity = angularVelocity * 0.8f + npc.velocity.X * 0.056f * 0.2f;
                npc.rotation = npc.rotation.AngleLerp(angularVelocity, 0.15f);

                // Fly approximately near the hover position.
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 23f;
                npc.SimpleFlyMovement(idealVelocity, 0.4f);
                if (MathHelper.Distance(npc.Center.X, hoverDestination.X) > 180f)
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, idealVelocity.X, 0.05f);

                // Arms that are not attacking do not do damage.
                npc.damage = 0;
            }
            else
            {
                if (npc.ai[2] == 0f)
                {
                    npc.velocity = (hoverDestination - npc.Center) / 24f;
                    if (npc.WithinRange(hoverDestination, 32f))
                    {
                        npc.Center = hoverDestination;
                        npc.velocity = Vector2.Zero;
                    }
                }
                else
                    npc.ai[2] = 0f;

                Vector2 cannonDirection = (npc.rotation + MathHelper.PiOver2).ToRotationVector2();
                PerformAttackBehaviors(npc, attackState, target, wrappedAttackTimer, cannonDirection);
            }

            return false;
        }

        public static void AimAtTarget(NPC npc, Vector2 aimDestination)
        {
            float idealRotation = npc.AngleTo(aimDestination) - MathHelper.PiOver2;
            npc.rotation = npc.rotation.AngleLerp(idealRotation, 0.05f).AngleTowards(idealRotation, 0.02f);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float telegraphIntensity = npc.localAI[0];
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            // Draw telegraphs if necessary.
            if (telegraphIntensity > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D line = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/BloomLineSmall").Value;

                Color outlineColor = Color.Lerp(TelegraphColor, Color.White, telegraphIntensity) * Utils.GetLerpValue(1f, 0.7f, telegraphIntensity, true);
                Vector2 origin = new(line.Width / 2f, line.Height);
                Vector2 beamScale = new(telegraphIntensity * 0.5f, 2.4f);
                Vector2 beamDirection = npc.rotation.ToRotationVector2();
                float beamRotation = beamDirection.ToRotation() + MathHelper.Pi;
                Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }

            Main.spriteBatch.Draw(TextureAssets.Npc[npc.type].Value, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            if (npc.ai[3] == 0f && npc.type == NPCID.PrimeLaser)
                Main.spriteBatch.Draw(TextureAssets.BoneLaser.Value, drawPosition, npc.frame, new Color(200, 200, 200, 0), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }

        public abstract float PredictivenessFactor { get; }

        public abstract Color TelegraphColor { get; }

        public abstract void PerformAttackBehaviors(NPC npc, PrimeAttackType attackState, Player target, float attackTimer, Vector2 cannonDirection);

        public virtual void PerformTelegraphBehaviors(NPC npc, PrimeAttackType attackState, float telegraphIntensity, Vector2 cannonDirection) { }
    }
}