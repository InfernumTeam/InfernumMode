using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Placeables.Banners;
using InfernumMode.BehaviorOverrides.BossAIs.DesertScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class GreatSandSharkBehaviorOverride : NPCBehaviorOverride
    {
        public enum GreatSandSharkAttackState
        {
            SandSwimChargeRush,
            DustDevils,
            ODSandSharkSummon,
            FastCharges,
            DuststormBurst,
            SandFlames,
            SharkWaves
        }

        public override int NPCOverrideType => ModContent.NPCType<GreatSandSharkNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.6f;
        public const float Phase3LifeRatio = 0.3f;

        public override void SetDefaults(NPC npc)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 1;

            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.npcSlots = 15f;
            npc.damage = 135;
            npc.width = npc.height = 140;
            npc.defense = 20;
            npc.DR_NERD(0.25f);
            npc.LifeMaxNERB(141466, 141466);
            npc.lifeMax /= 2;
            npc.aiStyle = -1;
            npc.modNPC.aiType = -1;
            npc.modNPC.music = MusicID.Boss5;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(0, 40, 0, 0);
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.buffImmune[BuffID.Ichor] = false;
            npc.buffImmune[ModContent.BuffType<MarkedforDeath>()] = false;
            npc.buffImmune[BuffID.Frostburn] = false;
            npc.buffImmune[BuffID.CursedInferno] = false;
            npc.buffImmune[BuffID.Daybreak] = false;
            npc.buffImmune[BuffID.StardustMinionBleed] = false;
            npc.buffImmune[BuffID.DryadsWardDebuff] = false;
            npc.buffImmune[BuffID.Oiled] = false;
            npc.buffImmune[BuffID.BetsysCurse] = false;
            npc.buffImmune[ModContent.BuffType<AstralInfectionDebuff>()] = false;
            npc.buffImmune[ModContent.BuffType<GodSlayerInferno>()] = false;
            npc.buffImmune[ModContent.BuffType<AbyssalFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<ArmorCrunch>()] = false;
            npc.buffImmune[ModContent.BuffType<DemonFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<HolyFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<Nightwither>()] = false;
            npc.buffImmune[ModContent.BuffType<Plague>()] = false;
            npc.buffImmune[ModContent.BuffType<Shred>()] = false;
            npc.buffImmune[ModContent.BuffType<WarCleave>()] = false;
            npc.buffImmune[ModContent.BuffType<WhisperingDeath>()] = false;
            npc.behindTiles = true;
            npc.netAlways = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.timeLeft = NPC.activeTime * 30;
            npc.modNPC.banner = npc.type;
            npc.modNPC.bannerItem = ModContent.ItemType<GreatSandSharkBanner>();
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float enrageTimer = ref npc.ai[2];
            ref float startDelay = ref npc.ai[3];

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            if (target.Center.Y > (Main.worldSurface - 100f) * 16D)
                enrageTimer--;
            else
                enrageTimer++;
            enrageTimer = MathHelper.Clamp(enrageTimer, 0f, 480f);

            bool pissedOff = enrageTimer >= 300f;

            if ((!target.active || target.dead || !npc.WithinRange(target.Center, 4700f)) && npc.target != 255)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 18f, 0.05f);
                npc.rotation = npc.velocity.Y * npc.direction * 0.02f;
                if (!npc.WithinRange(target.Center, 1600f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            // Reset things.
            npc.Calamity().CurrentlyEnraged = pissedOff;
            npc.Calamity().KillTime = 5;
            npc.defense = pissedOff ? 250 : npc.defDefense;
            npc.damage = npc.defDamage;
            npc.timeLeft = 3600;

            int desertTextureVariant = 0;
            if (target.ZoneDesert || target.ZoneUndergroundDesert)
            {
                if (target.ZoneCorrupt)
                    desertTextureVariant = 1;
                if (target.ZoneCrimson)
                    desertTextureVariant = 2;
                if (target.ZoneHoly)
                    desertTextureVariant = 3;
                if (target.Calamity().ZoneAstral)
                    desertTextureVariant = 4;
            }

            switch ((GreatSandSharkAttackState)(int)attackState)
            {
                case GreatSandSharkAttackState.SandSwimChargeRush:
                    DoAttack_SandSwimChargeRush(npc, target, startDelay < 135f, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.DustDevils:
                    DoAttack_DustDevils(npc, target, desertTextureVariant, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.ODSandSharkSummon:
                    DoAttack_ODSandSharkSummon(npc, target, desertTextureVariant, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.FastCharges:
                    DoAttack_FastCharges(npc, target, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.DuststormBurst:
                    DoAttack_DuststormBurst(npc, target, desertTextureVariant, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.SandFlames:
                    DoAttack_SandFlames(npc, target, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.SharkWaves:
                    DoAttack_SharkWaves(npc, target, desertTextureVariant, ref attackTimer);
                    break;
            }

            if (npc.velocity.HasNaNs())
                npc.velocity = -Vector2.UnitY;

            attackTimer++;
            startDelay++;

            return false;
        }

        public static void DoAttack_SandSwimChargeRush(NPC npc, Player target, bool dontCharge, float lifeRatio, ref float attackTimer)
        {
            bool inTiles = Collision.SolidCollision(npc.Center - Vector2.One * 36f, 72, 72);
            bool canCharge = inTiles && npc.WithinRange(target.Center, 750f) && !npc.WithinRange(target.Center, 280f) && attackTimer >= 60f && !dontCharge;
            float swimAcceleration = MathHelper.Lerp(0.85f, 1.05f, 1f - lifeRatio);
            float chargeSpeed = npc.Distance(target.Center) * 0.01f + MathHelper.Lerp(21f, 26f, 1f - lifeRatio);
            int chargeCount = 3;
            int blastCount = (int)MathHelper.Lerp(30f, 42f, 1f - lifeRatio);
            float blastSpeed = MathHelper.Lerp(9f, 12f, 1f - lifeRatio);

            ref float chargingFlag = ref npc.Infernum().ExtraAI[0];
            ref float chargeCountdown = ref npc.Infernum().ExtraAI[1];
            ref float chargeInterpolantTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[3];
            ref float chargeDirectionRotation = ref npc.Infernum().ExtraAI[4];

            if (attackTimer >= 180f)
                canCharge = true;

            if (!canCharge && chargeCountdown <= 0f)
            {
                npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.directionY = (target.Center.Y > npc.Center.Y).ToDirectionInt();

                // Swim towards the target quickly.
                if (inTiles || npc.Center.Y > target.Center.Y + 1000f)
                {
                    npc.velocity.X = MathHelper.Clamp(npc.velocity.X + npc.direction * swimAcceleration, -14f, 14f);
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + npc.directionY * swimAcceleration, -10f, 10f);
                }
                else if (npc.velocity.Y < 15f)
                    npc.velocity.Y += npc.WithinRange(target.Center, 600f) || npc.Center.Y < target.Center.Y ? 0.5f : 0.3f;

                chargingFlag = 0f;
                chargeInterpolantTimer = 0f;
                chargeDirectionRotation = -1000f;

                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
            }
            else
            {
                Vector2 chargeDirection = chargeDirectionRotation.ToRotationVector2();

                // Charge at the target and release a bunch of sand on the first frame the shark leaves solid tiles.
                attackTimer = 30f;
                if (chargingFlag == 0f && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < MathHelper.Pi * 0.45f)
                {
                    chargeCountdown = 35f;
                    chargingFlag = 1f;
                    chargeInterpolantTimer = 1f;
                    chargeDirectionRotation = npc.AngleTo(target.Center);
                    chargeCounter++;

                    // Release a radial spread of sand. There is a lot, but is is slow, and is supposed to be maneuvered through.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < blastCount; i++)
                        {
                            Vector2 sandVelocity = (MathHelper.TwoPi * i / blastCount).ToRotationVector2() * blastSpeed;
                            int sand = Utilities.NewProjectileBetter(npc.Center + sandVelocity * 3f, sandVelocity, ModContent.ProjectileType<SandstormBlast2>(), 175, 0f);
                            if (Main.projectile.IndexInRange(sand))
                                Main.projectile[sand].tileCollide = false;

                            if (lifeRatio < Phase2LifeRatio)
                            {
                                sandVelocity = (MathHelper.TwoPi * (i + 0.5f) / blastCount).ToRotationVector2() * blastSpeed * 0.67f;
                                sand = Utilities.NewProjectileBetter(npc.Center + sandVelocity * 3f, sandVelocity, ModContent.ProjectileType<SandstormBlast2>(), 175, 0f);
                                if (Main.projectile.IndexInRange(sand))
                                    Main.projectile[sand].tileCollide = false;
                            }
                        }
                    }

                    // Roar.
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GreatSandSharkRoar"), npc.Center);

                    npc.netUpdate = true;
                }
                else if (npc.velocity.Y < 15f)
                    npc.velocity.Y += 0.3f;

                if (chargeInterpolantTimer > 0f && chargeInterpolantTimer < 25f)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, chargeDirection * chargeSpeed, chargeInterpolantTimer / 45f);
                    chargeInterpolantTimer++;
                }

                chargeCountdown--;
            }

            // Define rotation and direction.
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = MathHelper.Clamp(npc.velocity.Y * npc.spriteDirection * 0.1f, -0.15f, 0.15f);
        }

        public static void DoAttack_DustDevils(NPC npc, Player target, int desertTextureVariant, float lifeRatio, ref float attackTimer)
        {
            int dustDevilReleaseRate = (int)MathHelper.Lerp(9f, 6f, 1f - lifeRatio);
            float swimAcceleration = MathHelper.Lerp(0.4f, 0.65f, 1f - lifeRatio);

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 30f, ref verticalSwimDirection);

            // Idly release dust devils.
            if (attackTimer % dustDevilReleaseRate == dustDevilReleaseRate - 1f && attackTimer < 540f)
            {
                Vector2 spawnPosition = target.Center + Vector2.UnitY * Main.rand.NextFloatDirection() * 850f;
                spawnPosition.X += Main.rand.NextBool().ToDirectionInt() * 800f;
                Vector2 shootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 6f;
                int dustDevil = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<DustDevil>(), 160, 0f);

                if (Main.projectile.IndexInRange(dustDevil))
                    Main.projectile[dustDevil].ai[1] = desertTextureVariant;
            }

            if (attackTimer > 620f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ODSandSharkSummon(NPC npc, Player target, int desertTextureVariant, float lifeRatio, ref float attackTimer)
        {
            int sharkSummonRate = (int)MathHelper.Lerp(45f, 24f, 1f - lifeRatio);
            float swimAcceleration = 0.4f;

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 30f, ref verticalSwimDirection);

            // Summon sand sharks.
            if (attackTimer % sharkSummonRate == sharkSummonRate - 1f && attackTimer < 120f)
            {
                float spawnOffsetFactor = MathHelper.Lerp(50f, 340f, attackTimer / 120f);
                for (int i = -1; i <= 1; i += 2)
                {
                    int shark = NPC.NewNPC((int)(npc.Center.X + spawnOffsetFactor * i), (int)npc.Center.Y, ModContent.NPCType<FlyingSandShark>());
                    if (Main.npc.IndexInRange(shark))
                        Main.npc[shark].ai[2] = desertTextureVariant;
                }
            }

            if (attackTimer > 150f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_FastCharges(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int hoverTime = 28;
            float hoverSpeed = 28f;
            int chargeTime = 35;
            float chargeSpeed = MathHelper.Lerp(22f, 27.5f, 1f - lifeRatio);
            int chargeCount = 5;
            float idealRotation = npc.AngleTo(target.Center);

            ref float horizontalChargeOffset = ref npc.Infernum().ExtraAI[0];
            ref float chargeState = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            if (chargeCounter == 0f)
                hoverTime += 35;

            if (npc.spriteDirection == 1)
                idealRotation += MathHelper.Pi;

            if (idealRotation < 0f)
                idealRotation += MathHelper.TwoPi;
            if (idealRotation > MathHelper.TwoPi)
                idealRotation -= MathHelper.TwoPi;

            float rotationalSpeed = 0.12f;
            if (chargeState == 1f)
                rotationalSpeed = 0f;
            npc.rotation = npc.rotation.AngleTowards(idealRotation, rotationalSpeed);

            // Hover below the target in anticipation of a charge.
            if (chargeState == 0f && !target.dead)
            {
                int horizontalDirectionToTarget = Math.Sign(target.Center.X - npc.Center.X);
                if (horizontalChargeOffset == 0f)
                    horizontalChargeOffset = horizontalDirectionToTarget * 300f;

                Vector2 idealHoverVelocity = npc.SafeDirectionTo(target.Center + new Vector2(horizontalChargeOffset, 200f) - npc.velocity) * hoverSpeed;
                npc.SimpleFlyMovement(idealHoverVelocity, hoverSpeed / 25f);

                if (horizontalDirectionToTarget != 0)
                {
                    if (attackTimer == 1f && horizontalDirectionToTarget != npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.direction = horizontalDirectionToTarget;
                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Do the charge.
                if (attackTimer >= hoverTime)
                {
                    // Roar.
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GreatSandSharkRoar"), npc.Center);

                    attackTimer = 0f;
                    chargeState = 1f;

                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.rotation = npc.velocity.ToRotation();
                    if (horizontalDirectionToTarget != 0)
                    {
                        npc.direction = horizontalDirectionToTarget;
                        if (npc.spriteDirection == 1)
                            npc.rotation += MathHelper.Pi;

                        npc.spriteDirection = -npc.direction;
                    }
                    npc.netUpdate = true;
                }
            }

            // Do the charge specific behaviors.
            if (chargeState == 1f)
            {
                // Create sand dust.
                int dustCount = 8;
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 40f) / 2f, npc.height) * 0.75f).RotatedBy(MathHelper.TwoPi * i / dustCount);
                    Vector2 dustVelocity = (Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                    Dust sand = Dust.NewDustPerfect(dustSpawnPosition + dustVelocity, 32, dustVelocity);
                    sand.scale *= 1.45f;
                    sand.velocity *= 0.25f;
                    sand.velocity -= npc.velocity;
                    sand.noGravity = true;
                    sand.noLight = true;
                }

                // Accelerate.
                npc.velocity *= 1.01f;

                if (attackTimer > chargeTime)
                {
                    chargeCounter++;
                    attackTimer = 0f;
                    chargeState = 0f;

                    if (chargeCounter >= chargeCount)
                        SelectNextAttack(npc);

                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAttack_DuststormBurst(NPC npc, Player target, int desertTextureVariant, float lifeRatio, ref float attackTimer)
        {
            int dustCreationCount = (int)MathHelper.Lerp(25f, 40f, 1f - lifeRatio);
            float swimAcceleration = 1.7f;

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 10f, ref verticalSwimDirection);

            // Create a burst of sand dust.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 120f)
            {
                for (int i = 0; i < dustCreationCount; i++)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2Circular(15f, 35f);
                    int dustDevil = Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<DustCloud>(), 160, 0f);

                    if (Main.projectile.IndexInRange(dustDevil))
                        Main.projectile[dustDevil].ai[1] = desertTextureVariant;
                }
            }

            if (attackTimer > 170f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_SandFlames(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int sandBallReleaseRate = (int)MathHelper.Lerp(45f, 35f, 1f - lifeRatio);
            float swimAcceleration = MathHelper.Lerp(0.4f, 0.65f, 1f - lifeRatio);

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 30f, ref verticalSwimDirection);

            // Idly release dust devils.
            if (attackTimer % sandBallReleaseRate == sandBallReleaseRate - 1f && attackTimer < 420f)
            {
                Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(750f, 750f);
                Vector2 shootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 9f;
                Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SandFlameBall>(), 160, 0f);
            }

            if (attackTimer > 540f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_SharkWaves(NPC npc, Player target, int desertTextureVariant, ref float attackTimer)
        {
            int sharkSummonRate = 62;
            float swimAcceleration = 0.45f;

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 30f, ref verticalSwimDirection);

            if (attackTimer == 25f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DesertScourgeRoar"), target.Center);

            // Summon sand sharks.
            if (attackTimer % sharkSummonRate == sharkSummonRate - 1f && attackTimer < 320f)
            {
                for (float dx = -1100f; dx < 1100f; dx += 200f)
                {
                    Vector2 spawnPosition = target.Center + new Vector2(dx, -1100f);
                    Vector2 shootVelocity = Vector2.UnitY * 8f;
                    int shark = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SwervingSandShark>(), 165, 0f);

                    if (Main.projectile.IndexInRange(shark))
                        Main.projectile[shark].ai[1] = desertTextureVariant;
                }
            }

            if (attackTimer > 430f)
                SelectNextAttack(npc);
        }

        public static void DefaultJumpMovement(NPC npc, ref Player target, float swimAcceleration, float jumpSpeed, ref float verticalSwimDirection)
        {
            bool inTiles = Collision.SolidCollision(npc.Center - Vector2.One * 16f - Vector2.UnitY * 24f, 32, 32);
            bool eligableToCharge = inTiles && !npc.WithinRange(target.Center, 150f) && target.velocity.Y > -2f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (lifeRatio < Phase3LifeRatio)
            {
                swimAcceleration *= 1.33f;
                jumpSpeed *= 1.25f;
            }

            // Rapidly approach the target and attempt to charge at them if eligable.
            if (eligableToCharge)
            {
                // Continuously check for a new target.
                npc.TargetClosest();
                target = Main.player[npc.target];

                // Accelerate towards the target.
                npc.velocity.X = MathHelper.Clamp(npc.velocity.X + npc.direction * swimAcceleration, -15f, 15f);
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + npc.directionY * swimAcceleration, -10f, 10f);

                Point aheadTilePosition = (npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * npc.Size.Length() * 0.5f + npc.velocity).ToTileCoordinates();
                Tile aheadTile = CalamityUtils.ParanoidTileRetrieval(aheadTilePosition.X, aheadTilePosition.Y);
                bool aheadTileIsSolid = aheadTile.nactive();

                // Charge at the target if doing so would lead to exiting tiles and it'd be in the same direction as the current velocity.
                if (!aheadTileIsSolid && Math.Sign(npc.velocity.X) == npc.direction)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center - Vector2.UnitY * 80f, -Vector2.UnitY) * jumpSpeed;
                    npc.netUpdate = true;
                }
            }

            else
            {
                if (inTiles)
                {
                    verticalSwimDirection = -1f;
                    npc.velocity.X += npc.direction * swimAcceleration * 0.7f;

                    // Slow down dramatically if over the horizontal speed limit.
                    if (Math.Abs(npc.velocity.X) > 10f)
                        npc.velocity.X *= 0.95f;
                }
                else
                    verticalSwimDirection = 1f;

                if (Math.Abs(npc.velocity.Y) > 6f && Math.Sign(npc.velocity.Y) == verticalSwimDirection)
                    verticalSwimDirection *= -1f;
                npc.velocity.Y += verticalSwimDirection * swimAcceleration * 0.4f;

                // Slow down dramatically if over the vertical speed limit.
                if (Math.Abs(npc.velocity.Y) > 6.5f)
                    npc.velocity.Y *= 0.95f;
            }

            // Define rotation and direction.
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = MathHelper.Clamp(npc.velocity.Y * npc.spriteDirection * 0.1f, -0.15f, 0.15f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            ref float attackIndex = ref npc.Infernum().ExtraAI[5];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            GreatSandSharkAttackState[] phase1AttackTable = new GreatSandSharkAttackState[]
            {
                GreatSandSharkAttackState.SandSwimChargeRush,
                GreatSandSharkAttackState.DustDevils,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.ODSandSharkSummon,
                GreatSandSharkAttackState.DustDevils,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.SandSwimChargeRush,
            };
            GreatSandSharkAttackState[] phase2AttackTable = new GreatSandSharkAttackState[]
            {
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.DustDevils,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.SandSwimChargeRush,
                GreatSandSharkAttackState.ODSandSharkSummon,
                GreatSandSharkAttackState.DustDevils,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.DuststormBurst,
                GreatSandSharkAttackState.SandSwimChargeRush,
                GreatSandSharkAttackState.SandFlames,
            };
            GreatSandSharkAttackState[] phase3AttackTable = new GreatSandSharkAttackState[]
            {
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.DustDevils,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.SharkWaves,
                GreatSandSharkAttackState.SandSwimChargeRush,
                GreatSandSharkAttackState.ODSandSharkSummon,
                GreatSandSharkAttackState.DustDevils,
                GreatSandSharkAttackState.FastCharges,
                GreatSandSharkAttackState.DuststormBurst,
                GreatSandSharkAttackState.SandSwimChargeRush,
                GreatSandSharkAttackState.SharkWaves,
                GreatSandSharkAttackState.SandFlames,
            };

            attackIndex++;

            npc.ai[0] = (int)phase1AttackTable[(int)attackIndex % phase1AttackTable.Length];
            if (lifeRatio < Phase2LifeRatio)
                npc.ai[0] = (int)phase2AttackTable[(int)attackIndex % phase2AttackTable.Length];
            if (lifeRatio < Phase3LifeRatio)
                npc.ai[0] = (int)phase3AttackTable[(int)attackIndex % phase3AttackTable.Length];

            npc.ai[1] = 0f;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter += 0.15f;
            npc.frameCounter %= Main.npcFrameCount[npc.type];
            npc.frame.Y = (int)npc.frameCounter * frameHeight;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = Main.npcTexture[npc.type];
            Color topLeftLight = Lighting.GetColor((int)npc.TopLeft.X / 16, (int)npc.TopLeft.Y / 16);
            Color topRightLight = Lighting.GetColor((int)npc.TopRight.X / 16, (int)npc.TopRight.Y / 16);
            Color bottomLeftLight = Lighting.GetColor((int)npc.BottomLeft.X / 16, (int)npc.BottomLeft.Y / 16);
            Color bottomRightLight = Lighting.GetColor((int)npc.BottomRight.X / 16, (int)npc.BottomRight.Y / 16);
            Vector4 averageLight = (topLeftLight.ToVector4() + topRightLight.ToVector4() + bottomLeftLight.ToVector4() + bottomRightLight.ToVector4()) * 0.25f;
            Color averageColor = new Color(averageLight);
            Vector2 origin = npc.frame.Size() * 0.5f;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < 8; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(averageColor);
                    float afterimageFade = 8f - i;
                    afterimageColor *= afterimageFade / (NPCID.Sets.TrailCacheLength[npc.type] * 1.5f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(averageColor), npc.rotation, origin, npc.scale, spriteEffects, 0);
            return false;
        }
    }
}
