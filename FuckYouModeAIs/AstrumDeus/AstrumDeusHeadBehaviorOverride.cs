using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.AstrumDeus
{
    public class AstrumDeusHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeusAttackType
        {
            AstralBombs,
            StellarCrash,
            CelestialLights,
            RealityWarpCharge,
        }

        // TODO - Add shoot noises to various attacks.

        public const float Phase2LifeThreshold = 0.55f;

        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusHeadSpectral>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Emit a pale white light idly.
            Lighting.AddLight(npc.Center, 0.3f, 0.3f, 0.3f);

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Reset damage. Do none by default if somewhat transparent.
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || Main.dayTime || !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasCreatedSegments = ref npc.localAI[0];

            // Create segments and initialize on the first frame.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedSegments == 0f)
            {
                CreateSegments(npc, 64, ModContent.NPCType<AstrumDeusBodySpectral>(), ModContent.NPCType<AstrumDeusTailSpectral>());
                attackType = (int)DeusAttackType.AstralBombs;
                hasCreatedSegments = 1f;
                npc.netUpdate = true;
            }

            // Quickly fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 16, 0, 255);

            switch ((DeusAttackType)(int)attackType)
            {
                case DeusAttackType.AstralBombs:
                    DoBehavior_AstralBombs(npc, target, lifeRatio, attackTimer);
                    break;
                case DeusAttackType.StellarCrash:
                    DoBehavior_StellarCrash(npc, target, lifeRatio, ref attackTimer);
                    break;
                case DeusAttackType.CelestialLights:
                    DoBehavior_CelestialLights(npc, target, lifeRatio, attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Custom Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            // Ascend into the sky and disappear.
            npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 26f, 0.08f);
            npc.velocity.X *= 0.975f;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Cap the despawn timer so that the boss can swiftly disappear.
            npc.timeLeft = Utils.Clamp(npc.timeLeft - 1, 0, 120);

            if (npc.timeLeft <= 0)
            {
                npc.life = 0;
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_AstralBombs(NPC npc, Player target, float lifeRatio, float attackTimer)
        {
            int shootRate = (int)MathHelper.Lerp(10f, 7f, 1f - lifeRatio);
            int totalBombsToShoot = lifeRatio < Phase2LifeThreshold ? 72 : 64;
            float flySpeed = MathHelper.Lerp(12f, 16f, 1f - lifeRatio);
            float flyAcceleration = MathHelper.Lerp(0.028f, 0.034f, 1f - lifeRatio);
            int shootTime = shootRate * totalBombsToShoot;
            int attackSwitchDelay = lifeRatio < Phase2LifeThreshold ? 45 : 105;

            // Initialize movement if it is too low.
            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 5.25f;

            // Drift towards the player. Contact damage is possible, but should be of little threat.
            if (!npc.WithinRange(target.Center, 325f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.075f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), flyAcceleration, true) * newSpeed;
            }

            // Release astral bomb mines from the sky.
            // They become faster/closer at specific life thresholds.
            if (attackTimer % shootRate == 0 && attackTimer < shootTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float bombShootOffset = lifeRatio < Phase2LifeThreshold ? 750f : 900f;
                    Vector2 bombShootPosition = target.Center - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * bombShootOffset;
                    Vector2 bombShootVelocity = (target.Center - bombShootPosition).SafeNormalize(Vector2.UnitY) * 11f;
                    if (lifeRatio < Phase2LifeThreshold)
                        bombShootVelocity *= 1.4f;

                    Utilities.NewProjectileBetter(bombShootPosition, bombShootVelocity, ModContent.ProjectileType<DeusMine2>(), 155, 0f);
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > shootTime + attackSwitchDelay)
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_StellarCrash(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int totalCrashes = lifeRatio < Phase2LifeThreshold ? 4 : 3;
            int crashRiseTime = lifeRatio < Phase2LifeThreshold ? 145 : 215;
            int crashChargeTime = lifeRatio < Phase2LifeThreshold ? 100 : 95;
            float crashSpeed = MathHelper.Lerp(32.5f, 37f, 1f - lifeRatio);
            float wrappedTime = attackTimer % (crashRiseTime + crashChargeTime);

            // Rise upward and release redirecting astral bombs.
            if (wrappedTime < crashRiseTime)
            {
                Vector2 riseDestination = target.Center - Vector2.UnitY * 1250f;
                if (!npc.WithinRange(riseDestination, 65f))
                {
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(riseDestination), 0.035f);
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(riseDestination) * 27.25f, 0.6f);
                }
                else
                    attackTimer += crashRiseTime - wrappedTime;

                if (npc.WithinRange(target.Center, 105f))
                    npc.Center = target.Center + target.SafeDirectionTo(npc.Center, -Vector2.UnitY) * 105f;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 60f == 59f)
                {
                    Vector2 shootVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(-Vector2.UnitY), -Vector2.UnitY, 0.75f) * Main.rand.NextFloat(9f, 12f);
                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AstralFlame2>(), 155, 0f);
                }
            }

            // Attempt to crash into the target from above after releasing flames as a dive-bomb.
            else
            {
                if (wrappedTime - crashRiseTime < 16)
                {
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center + target.velocity * 20f) * crashSpeed, crashSpeed * 0.11f);
                    if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 2f == 0f)
                    {
                        Vector2 shootVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(-Vector2.UnitY), npc.SafeDirectionTo(target.Center), 0.9f) * Main.rand.NextFloat(15.5f, 18f);
                        shootVelocity = shootVelocity.RotatedByRandom(0.46f);
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AstralFlame2>(), 155, 0f);
                    }
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > totalCrashes * (crashRiseTime + crashChargeTime) + 40)
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_CelestialLights(NPC npc, Player target, float lifeRatio, float attackTimer)
        {
            float flySpeed = MathHelper.Lerp(12.5f, 17f, 1f - lifeRatio);
            float flyAcceleration = MathHelper.Lerp(0.03f, 0.038f, 1f - lifeRatio);
            int attackSwitchDelay = lifeRatio < Phase2LifeThreshold ? 120 : 140;
            int shootRate = lifeRatio < Phase2LifeThreshold ? 3 : 7;

            ref float starCounter = ref npc.Infernum().ExtraAI[0];

            // Initialize movement if it is too low.
            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 5.25f;

            // Drift towards the player. Contact damage is possible, but should be of little threat.
            if (!npc.WithinRange(target.Center, 325f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), flyAcceleration, true) * newSpeed;
            }

            if (attackTimer > 60 && attackTimer % shootRate == shootRate - 1f)
            {
                int bodyType = ModContent.NPCType<AstrumDeusBodySpectral>();
                int shootIndex = (int)(starCounter * 2f);
                Vector2 starSpawnPosition = Vector2.Zero;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].ai[2] != shootIndex || Main.npc[i].type != bodyType || !Main.npc[i].active)
                        continue;

                    starSpawnPosition = Main.npc[i].Center;
                    break;
                }

                Main.PlaySound(SoundID.Item9, starSpawnPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient && starSpawnPosition != Vector2.Zero)
                {
                    Vector2 starVelocity = -Vector2.UnitY.RotatedByRandom(0.37f) * Main.rand.NextFloat(5f, 6f);
                    Utilities.NewProjectileBetter(starSpawnPosition, starVelocity, ModContent.ProjectileType<AstralStar>(), 160, 0f);
                }

                starCounter++;
            }

            if (attackTimer >= 60f + shootRate * 30f + 180f)
                GotoNextAttackState(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }
        #endregion Custom Behaviors

        #region Misc AI Operations
        public static void GotoNextAttackState(NPC npc)
        {
            DeusAttackType oldAttackState = (DeusAttackType)(int)npc.ai[0];
            DeusAttackType newAttackState = DeusAttackType.AstralBombs;

            switch (oldAttackState)
            {
                case DeusAttackType.AstralBombs:
                    newAttackState = DeusAttackType.CelestialLights;
                    break;
                case DeusAttackType.CelestialLights:
                    newAttackState = DeusAttackType.AstralBombs;
                    break;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = i;
                Main.npc[nextIndex].ai[1] = npc.whoAmI;
                Main.npc[nextIndex].ai[0] = previousIndex;
                if (i < wormLength - 1)
                    Main.npc[nextIndex].localAI[3] = i % 2;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        #endregion Misc AI Operations

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            return true;
        }
    }
}
