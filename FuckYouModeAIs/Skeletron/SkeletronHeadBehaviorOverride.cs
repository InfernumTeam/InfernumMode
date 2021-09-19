using CalamityMod;
using InfernumMode.FuckYouModeAIs.Polterghast;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Skeletron
{
	public class SkeletronHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.SkeletronHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
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

            if (summonAnimationTimer < 200f)
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

                    if (phaseChangeCountdown == 35f)
                    {
                        Main.PlaySound(SoundID.Roar, target.Center, 0);
                        npc.velocity = -Vector2.UnitY * 4f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<PolterghastWave>(), 0, 0f);
                    }

                    attackTimer = 0f;
                    return false;
                }

                // Do normal behavior at first.
                if (lifeRatio > 0.75f)
                    DoTypicalAI(npc, target, ref attackTimer);
                else if (lifeRatio > 0.4f)
                    DoPhase2AI(npc, target, ref attackTimer, ref attackState);
                else
                    DoPhase3AI(npc, target, ref attackTimer, ref attackState);

                // Phase transition effects.
                switch ((int)phaseChangeState)
                {
                    case 0:
                        if (lifeRatio < 0.75f)
                        {
                            phaseChangeCountdown = 90f;
                            phaseChangeState = 1f;
                        }
                        break;
                    case 1:
                        if (lifeRatio < 0.4f)
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
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.InverseLerp(0f, 15f, animationTimer, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.InverseLerp(200f, 192f, animationTimer, true);
            }

            npc.Opacity = Utils.InverseLerp(0f, 45f, animationTimer, true);
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (animationTimer < 90f)
                npc.velocity = -Vector2.UnitY * MathHelper.Lerp(0.1f, 4f, Utils.InverseLerp(0f, 35f, animationTimer, true) * Utils.InverseLerp(45f, 35f, animationTimer, true));

            // Summon hands.
            if (animationTimer == 80f)
			{
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int hand = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                    Main.npc[hand].ai[0] = -1f;
                    Main.npc[hand].ai[1] = npc.whoAmI;
                    Main.npc[hand].target = npc.target;
                    Main.npc[hand].netUpdate = true;

                    hand = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.SkeletronHand, npc.whoAmI);
                    Main.npc[hand].ai[0] = 1f;
                    Main.npc[hand].ai[1] = npc.whoAmI;
                    Main.npc[hand].target = npc.target;
                    Main.npc[hand].netUpdate = true;
                }
			}

            // Roar and attack.
            if (animationTimer == 160f)
			{
                Main.PlaySound(SoundID.Item122, target.Center);
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
                Main.PlaySound(SoundID.Roar, target.Center, 0);
                npc.velocity = npc.SafeDirectionTo(target.Center) * 14f;
                npc.netUpdate = true;
			}
        }

        public static void DoHoverMovement(NPC npc, Vector2 destination, Vector2 acceleration)
		{
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

        public static void DoTypicalAI(NPC npc, Player target, ref float attackTimer)
		{
            // Hover above the target and release skulls.
            if (attackTimer % 1050f < 600f)
			{
                Vector2 destination = target.Center - Vector2.UnitY * 250f;
                Vector2 acceleration = new Vector2(0.135f, 0.085f);
                DoHoverMovement(npc, destination, acceleration);

                int skullShootRate = 42;
                bool targetInLineOfSight = Collision.CanHit(npc.Center, 1, 1, target.position, target.width, target.head);
                if (attackTimer % skullShootRate == skullShootRate - 1f && targetInLineOfSight)
                {
                    Main.PlaySound(SoundID.Item8, target.Center);
                    Vector2 skullShootVelocity = npc.velocity;
                    skullShootVelocity.X *= 0.4f;
                    skullShootVelocity = skullShootVelocity.ClampMagnitude(10f, 16f);
                    Vector2 skullShootPosition = npc.Center + skullShootVelocity * 5f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int skull = Utilities.NewProjectileBetter(skullShootPosition, skullShootVelocity, ProjectileID.Skull, 100, 0f);
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
                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.rotation += npc.direction * 0.3f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * 5.75f;

                // Increase speed while charging.
                npc.damage = (int)(npc.defDamage * 1.4);

                // But lower defense.
                npc.defense -= 7;

                // Make the attack go by significantly quicker when hurting the player because telefrag spinning is legitimately awful.
                if (npc.WithinRange(target.Center, 60f))
                    attackTimer += 10f;
            }
		}

        public static void DoPhase2AI(NPC npc, Player target, ref float attackTimer, ref float attackState)
		{
            switch ((int)attackState)
			{
                // Hover over the target and release skulls directly at them.
                case 0:
                    int totalShots = 10;
                    Vector2 destination = target.Center - Vector2.UnitY * 360f;
                    Vector2 acceleration = new Vector2(0.18f, 0.12f);
                    DoHoverMovement(npc, destination, acceleration);

                    npc.rotation = npc.velocity.X * 0.05f;

                    if (!npc.WithinRange(target.Center, 85f) && attackTimer % 50f == 49f)
					{
                        int currentShotCounter = (int)(attackTimer / 50f);
                        Main.PlaySound(SoundID.Item8, target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float skullSpeed = 3.3f;
                            int skullCount = 3;
                            if (currentShotCounter % 4 == 3)
                            {
                                skullSpeed *= 1.1f;
                                skullCount = 5;
                            }

                            for (int i = 0; i < skullCount; i++)
                            {
                                Vector2 skullVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.59f, 0.59f, i / (skullCount - 1f))) * skullSpeed;
                                int skull = Utilities.NewProjectileBetter(npc.Center + skullVelocity * 6f, skullVelocity, ModContent.ProjectileType<NonHomingSkull>(), 90, 0f);
                                if (Main.projectile.IndexInRange(skull))
                                    Main.projectile[skull].ai[0] = 0.005f;
                            }
                        }
					}

                    // Go to the next state after enough shots have been performed.
                    if (attackTimer >= totalShots * 50f + 35f)
					{
                        attackTimer = 0f;
                        attackState = 1f;
                        npc.netUpdate = true;
					}

                    break;

                // Hover and swipe at the player.
                case 1:
                    destination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 520f, -330f);
                    acceleration = new Vector2(0.3f, 0.3f);

                    if (attackTimer < 90f)
                    {
                        DoHoverMovement(npc, destination, acceleration);
                        npc.Center = npc.Center.MoveTowards(destination, 10f);
                    }
                    else
                        npc.velocity *= 0.94f;

                    npc.damage = 0;
                    npc.rotation = npc.velocity.X * 0.05f;

                    if (attackTimer % 160f == 85f)
                        Main.PlaySound(SoundID.Roar, target.Center, 0);

                    if (attackTimer >= 305f)
                    {
                        attackTimer = 0f;
                        attackState = 2f;
                        npc.netUpdate = true;
                    }
                    break;

                // Spin charge.
                case 2:
                    if (attackTimer < 50f)
					{
                        npc.velocity *= 0.7f;
                        npc.rotation *= 0.7f;
					}

                    // Roar and charge after enough time has passed.
                    if (attackTimer == 50f)
                        Main.PlaySound(SoundID.Roar, target.Center, 0);

                    if (attackTimer >= 50f && attackTimer % 45f == 0f)
                    {
                        Main.PlaySound(SoundID.Item8, target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 skullVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 4f) * 7f;
                                Utilities.NewProjectileBetter(npc.Center, skullVelocity, ModContent.ProjectileType<NonHomingSkull>(), 95, 0f);
                            }
                        }
                    }

                    if (attackTimer > 50f && attackTimer < 270f)
					{
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 5.3f;

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
                        attackState = 0f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
            }
		}

        public static void DoPhase3AI(NPC npc, Player target, ref float attackTimer, ref float attackState)
        {
            switch ((int)attackState)
            {
                // Hover over the target and release skulls directly at them.
                case 0:
                    int totalShots = 10;
                    Vector2 destination = target.Center - Vector2.UnitY * 450f;
                    Vector2 acceleration = new Vector2(0.18f, 0.12f);
                    DoHoverMovement(npc, destination, acceleration);

                    npc.rotation = npc.velocity.X * 0.05f;

                    if (!npc.WithinRange(target.Center, 85f) && attackTimer % 40f == 39f && attackTimer < totalShots * 50f)
                    {
                        int currentShotCounter = (int)(attackTimer / 40f);
                        Main.PlaySound(SoundID.Item8, target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float skullSpeed = 3.8f;
                            int skullCount = 4;
                            if (currentShotCounter % 2 == 1)
                            {
                                skullSpeed *= 1.1f;
                                skullCount = 5;
                            }

                            for (int i = 0; i < skullCount; i++)
                            {
                                Vector2 skullVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.59f, 0.59f, i / (skullCount - 1f))) * skullSpeed;
                                int skull = Utilities.NewProjectileBetter(npc.Center + skullVelocity * 6f, skullVelocity, ModContent.ProjectileType<NonHomingSkull>(), 90, 0f);
                                if (Main.projectile.IndexInRange(skull))
                                    Main.projectile[skull].ai[0] = 0.005f;
                            }
                        }
                    }

                    // Go to the next state after enough shots have been performed.
                    if (attackTimer >= totalShots * 50f + 115f)
                    {
                        attackTimer = 0f;
                        attackState = 2f;
                        npc.netUpdate = true;
                    }

                    break;

                // Spin charge.
                case 2:
                    if (attackTimer < 30f)
                    {
                        npc.velocity *= 0.7f;
                        npc.rotation *= 0.7f;
                    }

                    // Roar and charge after enough time has passed.
                    if (attackTimer == 30f)
                        Main.PlaySound(SoundID.Roar, target.Center, 0);

                    if (attackTimer >= 30f && attackTimer % 45f == 0f)
                    {
                        Main.PlaySound(SoundID.Item8, target.Center);
                        float skullSpeed = 5f;
                        int skullCount = 4;

                        for (int i = 0; i < skullCount; i++)
                        {
                            Vector2 skullVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 95f).RotatedBy(MathHelper.Lerp(-0.44f, 0.44f, i / (skullCount - 1f))) * skullSpeed;
                            int skull = Utilities.NewProjectileBetter(npc.Center + skullVelocity * 6f, skullVelocity, ModContent.ProjectileType<NonHomingSkull>(), 90, 0f);
                            if (Main.projectile.IndexInRange(skull))
                                Main.projectile[skull].ai[0] = 0.005f;
                        }
                    }

                    if (attackTimer > 30f && attackTimer < 300f)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 5.3f;

                        npc.rotation += 0.2f;
                        npc.rotation %= MathHelper.TwoPi;

                        if (npc.WithinRange(target.Center, 50f))
                            attackTimer += 11f;
                    }

                    if (attackTimer > 300f)
                    {
                        npc.velocity *= 0.94f;
                        npc.rotation = npc.rotation.AngleLerp(0f, 0.07f);
                    }

                    if (attackTimer >= 320f)
                    {
                        attackState = 3f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Hover above the target and release shadow flames from the hands.
                case 3:
                    float adjustedTimer = attackTimer % 180f;
                    destination = target.Center - Vector2.UnitY * 400f;
                    acceleration = new Vector2(0.22f, 0.19f);
                    if (adjustedTimer > 45f)
                    {
                        destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 100f;
                        npc.Center = npc.Center.MoveTowards(destination, 10f);
                    }

                    DoHoverMovement(npc, destination, acceleration);
                    npc.rotation = npc.velocity.X * 0.05f;

                    if (attackTimer >= 385f)
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }

                    break;
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
                backGlowFade = Utils.InverseLerp(10f, 65f, phaseChangeTimer, true);
            if (npc.Infernum().ExtraAI[1] >= 3f)
                backGlowFade = 1f;

            Texture2D npcTexture = Main.npcTexture[npc.type];
            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * (i + 0.5f) / 6f).ToRotationVector2() * 4f + Vector2.UnitY * 2f;
                Vector2 drawPosition = npc.Center + drawOffset - Main.screenPosition;
                Color drawColor = Color.Lerp(Color.Transparent, Color.Fuchsia, backGlowFade) * backGlowFade * 0.24f;
                drawColor.A = 0;

                spriteBatch.Draw(npcTexture, drawPosition, null, drawColor, npc.rotation, npcTexture.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            return true;
        }
        #endregion
    }
}
