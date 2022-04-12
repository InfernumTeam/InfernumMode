using CalamityMod;
using CalamityMod.Events;
using InfernumMode.BehaviorOverrides.BossAIs.Polterghast;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.Skeletron
{
    public class SkeletronHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum SkeletronAttackType
        {
            Phase1Fakeout,
            HoverSkulls,
            SpinCharge,
            HandWaves,
            HandShadowflameBurst,
            HandShadowflameWaves,
            DownwardAcceleratingSkulls
        }

        public override int NPCOverrideType => NPCID.SkeletronHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.85f;
        public const float Phase3LifeRatio = 0.475f;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3950f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            Player target = Main.player[npc.target];

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float summonAnimationTimer = ref npc.ai[2];
            ref float animationChargeTimer = ref npc.ai[3];
            ref float phaseChangeCountdown = ref npc.Infernum().ExtraAI[0];
            ref float phaseChangeState = ref npc.Infernum().ExtraAI[1];

            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;
            npc.Calamity().DR = 0.35f;
            npc.dontTakeDamage = false;

            if (summonAnimationTimer < 225f)
            {
                DoSpawnAnimationStuff(npc, target, summonAnimationTimer, ref animationChargeTimer);
                summonAnimationTimer++;
                return false;
            }

            if (Main.dayTime)
            {
                npc.velocity = (npc.velocity * 14f + npc.SafeDirectionTo(target.Center) * 25f) / 15f;
                npc.rotation += (npc.velocity.X > 0f).ToDirectionInt() * 0.3f;
                npc.dontTakeDamage = true;
                npc.damage = 99999;
                return false;
            }

            if (animationChargeTimer > 0f)
            {
                animationChargeTimer--;
                npc.rotation = npc.velocity.X * 0.04f;
            }

            if (animationChargeTimer <= 0f)
            {
                // Do phase transition effects as needed.
                if (phaseChangeCountdown > 0f)
                {
                    npc.velocity *= 0.96f;
                    npc.rotation *= 0.94f;

                    attackState = 0f;
                    attackTimer = 0f;
                    phaseChangeCountdown--;

                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 325f;
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.04f);
                    npc.damage = 0;

                    if (phaseChangeCountdown == 35f)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, target.Center, 0);
                        npc.velocity = -Vector2.UnitY * 4f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(new InfernumSource(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PolterghastWave>(), 0, 0f);
                    }

                    attackTimer = 0f;
                    return false;
                }

                switch ((SkeletronAttackType)(int)attackState)
                {
                    case SkeletronAttackType.Phase1Fakeout:
                        DoBehavior_Phase1Fakeout(npc, target, ref attackTimer);
                        if (phase2)
                            attackState = (int)SkeletronAttackType.HoverSkulls;
                        break;
                    case SkeletronAttackType.HoverSkulls:
                        DoBehavior_HoverSkulls(npc, target, ref attackTimer);
                        break;
                    case SkeletronAttackType.HandWaves:
                        DoBehavior_HandWaves(npc, target, ref attackTimer);
                        break;
                    case SkeletronAttackType.SpinCharge:
                        DoBehavior_SpinCharge(npc, target, phase3, ref attackTimer);
                        break;
                    case SkeletronAttackType.HandShadowflameBurst:
                        DoBehavior_HandShadowflameBurst(npc, target, ref attackTimer);
                        break;
                    case SkeletronAttackType.HandShadowflameWaves:
                        DoBehavior_HandShadowflameWaves(npc, target, ref attackTimer);
                        break;
                    case SkeletronAttackType.DownwardAcceleratingSkulls:
                        DoBehavior_DownwardAcceleratingSkulls(npc, target, ref attackTimer);
                        break;
                }

                // Phase transition effects.
                switch ((int)phaseChangeState)
                {
                    case 0:
                        if (phase2)
                        {
                            phaseChangeCountdown = 90f;
                            phaseChangeState = 1f;
                        }
                        break;
                    case 1:
                        if (phase3)
                        {
                            phaseChangeCountdown = 90f;
                            phaseChangeState = 2f;
                        }
                        break;
                }
            }

            attackTimer++;
            return false;
        }

        public static void DoDespawnEffects(NPC npc)
        {
            npc.velocity *= 0.7f;
            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);
            if (npc.Opacity <= 0f)
            {
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoSpawnAnimationStuff(NPC npc, Player target, float animationTimer, ref float animationChargeTimer)
        {
            // Focus on the boss as it spawns.
            if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 2000f))
            {
                Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.GetLerpValue(0f, 15f, animationTimer, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.GetLerpValue(200f, 192f, animationTimer, true);
            }

            npc.Opacity = Utils.GetLerpValue(0f, 45f, animationTimer, true);
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (animationTimer < 90f)
                npc.velocity = -Vector2.UnitY * MathHelper.Lerp(0.1f, 4f, Utils.GetLerpValue(0f, 35f, animationTimer, true) * Utils.GetLerpValue(45f, 35f, animationTimer, true));

            // Summon hands.
            if (animationTimer == 80f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int hand = NPC.NewNPC(new InfernumSource(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                    Main.npc[hand].ai[0] = -1f;
                    Main.npc[hand].ai[1] = npc.whoAmI;
                    Main.npc[hand].target = npc.target;
                    Main.npc[hand].netUpdate = true;

                    hand = NPC.NewNPC(new InfernumSource(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                    Main.npc[hand].ai[0] = 1f;
                    Main.npc[hand].ai[1] = npc.whoAmI;
                    Main.npc[hand].target = npc.target;
                    Main.npc[hand].netUpdate = true;
                }
            }

            // Roar and attack.
            if (animationTimer == 160f)
            {
                SoundEngine.PlaySound(SoundID.Item122, target.Center);
                for (int i = 0; i < 220; i++)
                {
                    Dust ectoplasm = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 60f), 264);
                    ectoplasm.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f, 12f);
                    ectoplasm.velocity = Vector2.Lerp(ectoplasm.velocity, (MathHelper.TwoPi * i / 220f).ToRotationVector2() * ectoplasm.velocity.Length(), 0.8f);
                    ectoplasm.velocity = Vector2.Lerp(ectoplasm.velocity, -Vector2.UnitY * ectoplasm.velocity.Length(), 0.5f);
                    ectoplasm.fadeIn = Main.rand.NextFloat(1.3f, 1.9f);
                    ectoplasm.scale = Main.rand.NextFloat(1.65f, 1.85f);
                    ectoplasm.noGravity = true;
                }
            }

            if (animationTimer == 190f)
            {
                animationChargeTimer = 70f;
                SoundEngine.PlaySound(SoundID.Roar, target.Center, 0);

                float chargeSpeed = MathHelper.Lerp(6f, 14f, Utils.GetLerpValue(560f, 1230f, npc.Distance(target.Center), true));
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.netUpdate = true;
            }
        }

        public static void DoHoverMovement(NPC npc, Vector2 destination, Vector2 acceleration)
        {
            if (BossRushEvent.BossRushActive)
                acceleration *= 4f;

            if (npc.Center.Y > destination.Y)
            {
                if (npc.velocity.Y > 0f)
                    npc.velocity.Y *= 0.98f;
                npc.velocity.Y -= acceleration.Y;
                if (npc.velocity.Y > 2f)
                    npc.velocity.Y = 2f;
            }
            else if (npc.Center.Y < destination.Y)
            {
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y *= 0.98f;
                npc.velocity.Y += acceleration.Y;
                if (npc.velocity.Y < -2f)
                    npc.velocity.Y = -2f;
            }

            if (npc.Center.X > destination.X)
            {
                if (npc.velocity.X > 0f)
                    npc.velocity.X *= 0.98f;
                npc.velocity.X -= acceleration.X;
                if (npc.velocity.X > 2f)
                    npc.velocity.X = 2f;
            }

            if (npc.Center.X < destination.X)
            {
                if (npc.velocity.X < 0f)
                    npc.velocity.X *= 0.98f;
                npc.velocity.X += acceleration.X;
                if (npc.velocity.X < -2f)
                    npc.velocity.X = -2f;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            SkeletronAttackType currentAttack = (SkeletronAttackType)(int)npc.ai[0];

            npc.TargetClosest();
            switch (currentAttack)
            {
                case SkeletronAttackType.HoverSkulls:
                    npc.ai[0] = phase3 ? (int)SkeletronAttackType.DownwardAcceleratingSkulls : (int)SkeletronAttackType.SpinCharge;
                    break;
                case SkeletronAttackType.SpinCharge:
                case SkeletronAttackType.DownwardAcceleratingSkulls:
                    npc.ai[0] = (int)SkeletronAttackType.HandWaves;
                    if (phase3 && currentAttack != SkeletronAttackType.SpinCharge)
                        npc.ai[0] = (int)SkeletronAttackType.SpinCharge;
                    break;
                case SkeletronAttackType.HandWaves:
                    npc.ai[0] = (int)SkeletronAttackType.HandShadowflameBurst;
                    break;
                case SkeletronAttackType.HandShadowflameBurst:
                    npc.ai[0] = phase3 ? (int)SkeletronAttackType.HandShadowflameWaves : (int)SkeletronAttackType.HoverSkulls;
                    break;
                case SkeletronAttackType.HandShadowflameWaves:
                    npc.ai[0] = phase3 ? (int)SkeletronAttackType.DownwardAcceleratingSkulls : (int)SkeletronAttackType.HoverSkulls;
                    break;
            }
            npc.netUpdate = true;
        }

        public static void DoBehavior_Phase1Fakeout(NPC npc, Player target, ref float attackTimer)
        {
            // Hover above the target and release skulls.
            if (attackTimer % 1050f < 600f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 250f;
                Vector2 acceleration = new(0.135f, 0.085f);
                DoHoverMovement(npc, destination, acceleration);

                int skullShootRate = 42;
                bool targetInLineOfSight = Collision.CanHit(npc.Center, 1, 1, target.position, target.width, target.head);
                if (attackTimer % skullShootRate == skullShootRate - 1f && targetInLineOfSight)
                {
                    SoundEngine.PlaySound(SoundID.Item8, target.Center);
                    Vector2 skullShootVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(Vector2.UnitY), npc.SafeDirectionTo(target.Center, Vector2.UnitY), 0.75f) * npc.velocity.Length();
                    skullShootVelocity.X *= 0.4f;
                    skullShootVelocity = skullShootVelocity.ClampMagnitude(10f, 16f);
                    if (BossRushEvent.BossRushActive)
                        skullShootVelocity *= 1.85f;

                    Vector2 skullShootPosition = npc.Center + skullShootVelocity * 5f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int skull = Utilities.NewProjectileBetter(skullShootPosition, skullShootVelocity, ProjectileID.Skull, 95, 0f);
                        if (Main.projectile.IndexInRange(skull))
                        {
                            Main.projectile[skull].ai[0] = -1f;
                            Main.projectile[skull].tileCollide = false;
                        }
                    }
                }
                npc.rotation = npc.velocity.X * 0.04f;
            }
            else
            {
                if (attackTimer % 1050f == 601f)
                    SoundEngine.PlaySound(SoundID.Roar, target.Center, 0);

                float moveSpeed = BossRushEvent.BossRushActive ? 20.75f : 7.75f;
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.rotation += npc.direction * 0.3f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * moveSpeed;

                // Increase speed while charging.
                npc.damage = (int)(npc.defDamage * 1.4);

                // But lower defense.
                npc.defense -= 7;

                // Make the attack go by significantly quicker when hurting the player because telefrag spinning is legitimately awful.
                if (npc.WithinRange(target.Center, 60f))
                    attackTimer += 10f;
            }
        }

        public static void DoBehavior_HoverSkulls(NPC npc, Player target, ref float attackTimer)
        {
            int totalShots = 6;
            int shootRate = 45;
            Vector2 destination = target.Center - Vector2.UnitY * 360f;
            Vector2 acceleration = new(0.08f, 0.06f);
            DoHoverMovement(npc, destination, acceleration);

            npc.rotation = npc.velocity.X * 0.05f;

            // Make skeletron a little beefier due to being easy to hit.
            npc.defense = npc.defDefense + 6;

            if (!npc.WithinRange(target.Center, 85f) && attackTimer % shootRate == shootRate - 1f)
            {
                int currentShotCounter = (int)(attackTimer / shootRate);
                SoundEngine.PlaySound(SoundID.Item8, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float skullSpeed = 3.3f;
                    int skullCount = 3;
                    if (currentShotCounter % 4 == 3)
                    {
                        skullSpeed *= 1.1f;
                        skullCount = 5;
                    }

                    if (BossRushEvent.BossRushActive)
                        skullSpeed *= 3.25f;

                    for (int i = 0; i < skullCount; i++)
                    {
                        Vector2 skullShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.59f, 0.59f, i / (skullCount - 1f))) * skullSpeed;
                        int skull = Utilities.NewProjectileBetter(npc.Center + skullShootVelocity * 6f, skullShootVelocity, ModContent.ProjectileType<NonHomingSkull>(), 90, 0f);
                        if (Main.projectile.IndexInRange(skull))
                            Main.projectile[skull].ai[0] = 0.005f;
                    }
                }
            }

            // Go to the next state after enough shots have been performed.
            if (attackTimer >= totalShots * shootRate + 35f)
            {
                SelectNextAttack(npc);
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_HandWaves(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 520f, -330f);
            Vector2 acceleration = new(0.3f, 0.3f);

            if (attackTimer < 90f)
            {
                DoHoverMovement(npc, destination, acceleration);
                npc.Center = npc.Center.MoveTowards(destination, 10f);
            }
            else
                npc.velocity *= 0.94f;

            // Release skulls downward to prevent invalidating the attack by sitting under skeletron like a coward lmao
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 6f == 5f && attackTimer >= 90f)
            {
                Vector2 shootVelocity = -Vector2.UnitY.RotatedByRandom(0.75f) * Main.rand.NextFloat(8f, 12.65f);
                if (Main.rand.NextBool(2))
                    shootVelocity.Y *= -1f;
                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ModContent.ProjectileType<NonHomingSkull>(), 95, 0f);
            }

            npc.damage = 0;
            npc.rotation = npc.velocity.X * 0.05f;

            if (attackTimer % 160f == 85f)
                SoundEngine.PlaySound(SoundID.Roar, target.Center, 0);

            if (attackTimer >= 305f)
            {
                SelectNextAttack(npc);
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SpinCharge(NPC npc, Player target, bool phase3, ref float attackTimer)
        {
            if (attackTimer < 50f)
            {
                npc.velocity *= 0.7f;
                npc.rotation *= 0.7f;
            }

            // Roar and charge after enough time has passed.
            if (attackTimer == 50f)
                SoundEngine.PlaySound(SoundID.Roar, target.Center, 0);

            if (attackTimer >= 50f && attackTimer % 45f == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item8, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (phase3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 skullShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 3f) * 10f;
                            int skull = Utilities.NewProjectileBetter(npc.Center, skullShootVelocity, ProjectileID.Skull, 95, 0f);
                            if (Main.projectile.IndexInRange(skull))
                            {
                                Main.projectile[skull].ai[0] = -1f;
                                Main.projectile[skull].tileCollide = false;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 skullShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 8f) * 14f;
                            Utilities.NewProjectileBetter(npc.Center, skullShootVelocity, ModContent.ProjectileType<SpinningFireball>(), 95, 0f);
                        }
                    }
                }
            }

            if (attackTimer is > 50f and < 270f)
            {
                float moveSpeed = BossRushEvent.BossRushActive ? 21.25f : 7.25f;
                if (phase3)
                    moveSpeed *= 1.18f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * moveSpeed;

                npc.rotation += 0.2f;
                npc.rotation %= MathHelper.TwoPi;

                if (npc.WithinRange(target.Center, 50f))
                    attackTimer += 10f;
            }

            if (attackTimer > 270f)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.07f);
            }

            if (attackTimer >= 290f)
            {
                SelectNextAttack(npc);
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_HandShadowflameBurst(NPC npc, Player target, ref float attackTimer)
        {
            float adjustedTimer = attackTimer % 180f;
            Vector2 destination = target.Center - Vector2.UnitY * 400f;
            Vector2 acceleration = new(0.5f, 0.35f);
            if (adjustedTimer > 45f)
            {
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 100f;
                npc.Center = npc.Center.MoveTowards(destination, 16f);
            }

            // Make skeletron a little beefier due to being easy to hit.
            npc.defense = npc.defDefense + 6;

            DoHoverMovement(npc, destination, acceleration);
            npc.rotation = npc.velocity.X * 0.05f;

            if (attackTimer >= 385f)
            {
                SelectNextAttack(npc);
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_HandShadowflameWaves(NPC npc, Player target, ref float attackTimer)
        {
            float adjustedTimer = attackTimer % 150f;
            Vector2 destination = target.Center - Vector2.UnitY * 360f;
            Vector2 acceleration = new(0.4f, 0.27f);
            if (adjustedTimer > 45f)
            {
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 100f;
                npc.Center = npc.Center.MoveTowards(destination, 10f);
            }

            // Make skeletron a little beefier due to being easy to hit.
            npc.defense = npc.defDefense + 6;

            DoHoverMovement(npc, destination, acceleration);
            npc.rotation = npc.velocity.X * 0.05f;

            if (attackTimer >= 660f)
            {
                SelectNextAttack(npc);
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_DownwardAcceleratingSkulls(NPC npc, Player target, ref float attackTimer)
        {
            int totalShots = 7;
            int shootRate = 90;
            int attackDelay = 135;
            Vector2 destination = target.Center - Vector2.UnitY * 400f;
            Vector2 acceleration = new(0.08f, 0.12f);
            DoHoverMovement(npc, destination, acceleration);

            npc.rotation = npc.velocity.X * 0.05f;

            // Make skeletron a little beefier due to being easy to hit.
            npc.defense = npc.defDefense + 6;

            // Release magic from the mouth as a telegraph.
            if (attackTimer < attackDelay)
            {
                Dust magic = Dust.NewDustDirect(npc.Bottom - new Vector2(npc.width * 0.5f, 30f), npc.width, 16, 264);
                magic.velocity = Main.rand.NextFloat(-0.43f, 0.43f).ToRotationVector2() * Main.rand.NextFloat(2f, 8f);
                magic.velocity.X *= Main.rand.NextBool().ToDirectionInt();
                magic.scale = Main.rand.NextFloat(1f, 1.4f);
                magic.fadeIn = 0.6f;
                magic.noLight = true;
                magic.noGravity = true;
            }

            if (!npc.WithinRange(target.Center, 85f) && attackTimer % shootRate == shootRate - 1f && attackTimer > attackDelay)
            {
                SoundEngine.PlaySound(SoundID.Item8, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float maxOffset = 18f;
                    float openOffsetArea = Main.rand.NextFloat(-maxOffset * 0.32f, maxOffset * 0.32f);
                    float fuck = Main.rand.NextFloat(-0.6f, 0.6f);
                    for (float offset = -maxOffset; offset < maxOffset; offset += maxOffset * 0.1f)
                    {
                        // Don't fire skulls from some areas, to allow the player to have an avoidance area.
                        if (MathHelper.Distance(openOffsetArea, offset + fuck) < 1.9f)
                            continue;

                        Vector2 shootVelocity = Vector2.UnitX * (offset + fuck) * 0.3f;
                        shootVelocity.Y += Main.rand.NextFloat(2f);
                        int fire = Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 20f, shootVelocity, ModContent.ProjectileType<AcceleratingSkull>(), 100, 0f);
                        if (Main.projectile.IndexInRange(fire))
                        {
                            Main.projectile[fire].ai[0] = offset + fuck;
                            Main.projectile[fire].netUpdate = true;
                        }
                    }

                    // Fire one skull directly at the target.
                    Vector2 skullShootVelocity = npc.SafeDirectionTo(target.Center) * 5f;
                    int skull = Utilities.NewProjectileBetter(npc.Center + skullShootVelocity * 6f, skullShootVelocity, ModContent.ProjectileType<AcceleratingSkull>(), 95, 0f);
                    if (Main.projectile.IndexInRange(skull))
                        Main.projectile[skull].ai[0] = -9999f;
                }
            }

            // Go to the next state after enough shots have been performed.
            if (attackTimer >= totalShots * shootRate + 65f)
            {
                SelectNextAttack(npc);
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        #endregion AI

        #region Drawing and Frames

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float phaseChangeTimer = 90f - npc.Infernum().ExtraAI[0];
            bool canDrawBehindGlow = npc.Infernum().ExtraAI[1] >= 2f;
            float backGlowFade = 0f;

            if (canDrawBehindGlow)
                backGlowFade = Utils.GetLerpValue(10f, 65f, phaseChangeTimer, true);
            if (npc.Infernum().ExtraAI[1] >= 3f)
                backGlowFade = 1f;

            Texture2D npcTexture = TextureAssets.Npc[npc.type].Value;
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * (i + 0.5f) / 6f).ToRotationVector2() * 4f + Vector2.UnitY * 2f;
                Vector2 drawPosition = npc.Center + drawOffset - Main.screenPosition;
                Color drawColor = Color.Lerp(Color.Transparent, Color.Fuchsia, backGlowFade) * backGlowFade * 0.24f;
                drawColor.A = 0;

                Main.spriteBatch.Draw(npcTexture, drawPosition, null, drawColor, npc.rotation, npcTexture.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            return true;
        }
        #endregion
    }
}
