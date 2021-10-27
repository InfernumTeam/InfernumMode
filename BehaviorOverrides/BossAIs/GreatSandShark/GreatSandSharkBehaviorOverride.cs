using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Placeables.Banners;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
            ODSandSharkSummon
        }

        public override int NPCOverrideType => ModContent.NPCType<GreatSandSharkNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public override void SetDefaults(NPC npc)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 1;

            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.npcSlots = 15f;
            npc.damage = 135;
            npc.width = 300;
            npc.height = 120;
            npc.defense = 20;
            npc.DR_NERD(0.25f);
            npc.LifeMaxNERB(84720, 84720);
            npc.lifeMax /= 2;
            npc.aiStyle = -1;
            npc.modNPC.aiType = -1;
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
            npc.buffImmune[ModContent.BuffType<SilvaStun>()] = false;
            npc.behindTiles = true;
            npc.netAlways = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.timeLeft = NPC.activeTime * 30;
            npc.modNPC.banner = npc.type;
            npc.modNPC.bannerItem = ModContent.ItemType<GreatSandSharkBanner>();
        }

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            npc.TargetClosest();

            Player target = Main.player[npc.target];
            bool pissedOff = !(Main.sandTiles > 1000 && target.Center.Y > (Main.worldSurface - 180f) * 16D);

            if ((!target.active || target.dead || !npc.WithinRange(target.Center, pissedOff ? 2250f : 4200f)) && npc.target != 255)
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
                    DoAttack_SandSwimChargeRush(npc, target, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.DustDevils:
                    DoAttack_DustDevils(npc, target, desertTextureVariant, lifeRatio, ref attackTimer);
                    break;
                case GreatSandSharkAttackState.ODSandSharkSummon:
                    DoAttack_ODSandSharkSummon(npc, target, desertTextureVariant, lifeRatio, ref attackTimer);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoAttack_SandSwimChargeRush(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            bool inTiles = Collision.SolidCollision(npc.Center - Vector2.One * 36f, 72, 72);
            bool canCharge = inTiles && npc.WithinRange(target.Center, 750f) && attackTimer >= 60f;
            float swimAcceleration = MathHelper.Lerp(0.85f, 1.05f, 1f - lifeRatio);
            float chargeSpeed = npc.Distance(target.Center) * 0.01f + MathHelper.Lerp(18.5f, 21f, 1f - lifeRatio);
            int chargeCount = 4;

            ref float chargingFlag = ref npc.Infernum().ExtraAI[0];
            ref float chargeCountdown = ref npc.Infernum().ExtraAI[1];
            ref float chargeInterpolantTimer = ref npc.Infernum().ExtraAI[2];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[3];

            if (!canCharge && chargeCountdown <= 0f)
            {
                npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.directionY = (target.Center.Y > npc.Center.Y).ToDirectionInt();

                // Swim towards the target quickly.
                if (inTiles)
                {
                    npc.velocity.X = MathHelper.Clamp(npc.velocity.X + npc.direction * swimAcceleration, -14f, 14f);
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + npc.directionY * swimAcceleration, -10f, 10f);
                }
                else if (npc.velocity.Y < 15f)
                    npc.velocity.Y += npc.WithinRange(target.Center, 600f) || npc.Center.Y < target.Center.Y ? 0.5f : 0.3f;

                chargingFlag = 0f;
                chargeInterpolantTimer = 0f;

                if (chargeCounter > chargeCount)
                    SelectNextAttack(npc);
            }
            else
            {
                Vector2 chargeDirection = npc.SafeDirectionTo(target.Center, -Vector2.UnitY);

                // Charge at the target and release a bunch of sand on the first frame the shark leaves solid tiles.
                if (chargingFlag == 0f && npc.velocity.AngleBetween(chargeDirection) < MathHelper.Pi * 0.45f)
                {
                    chargeCountdown = 35f;
                    chargeInterpolantTimer = 1f;
                    chargeCounter++;

                    // Release a radial spread of sand. There is a lot, but is is slow, and is supposed to be maneuvered through.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 36; i++)
                        {
                            Vector2 sandVelocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 6f;
                            int sand = Utilities.NewProjectileBetter(npc.Center + sandVelocity * 3f, sandVelocity, ModContent.ProjectileType<SandBlast>(), 155, 0f);
                            if (Main.projectile.IndexInRange(sand))
                                Main.projectile[sand].tileCollide = false;
                        }
                    }

                    // Roar.
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GreatSandSharkRoar"), npc.Center);

                    npc.netUpdate = true;
                    chargingFlag = 1f;
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
            int dustDevilReleaseRate = (int)MathHelper.Lerp(10f, 7f, 1f - lifeRatio);
            float swimAcceleration = MathHelper.Lerp(0.4f, 0.65f, 1f - lifeRatio);

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 30f, ref verticalSwimDirection);

            // Idly release dust devils.
            if (attackTimer % dustDevilReleaseRate == dustDevilReleaseRate - 1f && attackTimer < 540f)
			{
                Vector2 spawnPosition = target.Center + Vector2.UnitY * Main.rand.NextFloatDirection() * 850f;
                spawnPosition.X += Main.rand.NextBool(2).ToDirectionInt() * 800f;
                Vector2 shootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 8f;
                int dustDevil = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<DustDevil>(), 160, 0f);

                if (Main.projectile.IndexInRange(dustDevil))
                    Main.projectile[dustDevil].ai[1] = desertTextureVariant;
			}

            if (attackTimer > 620f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_ODSandSharkSummon(NPC npc, Player target, int desertTextureVariant, float lifeRatio, ref float attackTimer)
        {
            int sharkSummonRate = (int)MathHelper.Lerp(30f, 16f, 1f - lifeRatio);
            float swimAcceleration = 0.4f;

            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];
            DefaultJumpMovement(npc, ref target, swimAcceleration, swimAcceleration * 30f, ref verticalSwimDirection);

            // Summon sand sharks.
            if (attackTimer % sharkSummonRate == sharkSummonRate - 1f && attackTimer < 120f)
            {
                float spawnOffsetFactor = MathHelper.Lerp(100f, 540f, attackTimer / 120f);
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

        public static void DefaultJumpMovement(NPC npc, ref Player target, float swimAcceleration, float jumpSpeed, ref float verticalSwimDirection)
        {
            bool inTiles = Collision.SolidCollision(npc.Center - Vector2.One * 16f - Vector2.UnitY * 24f, 32, 32);
            bool eligableToCharge = inTiles && !npc.WithinRange(target.Center, 150f) && target.velocity.Y > -2f;

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
            Player target = Main.player[npc.target];
            GreatSandSharkAttackState previousAttackState = (GreatSandSharkAttackState)(int)npc.ai[0];

            List<GreatSandSharkAttackState> possibleNextAttacks = new List<GreatSandSharkAttackState>()
            {
                GreatSandSharkAttackState.SandSwimChargeRush,
                GreatSandSharkAttackState.DustDevils
            };
            possibleNextAttacks.AddWithCondition(GreatSandSharkAttackState.ODSandSharkSummon, npc.WithinRange(target.Center, 750f));

            if (possibleNextAttacks.Count > 1)
                possibleNextAttacks.Remove(previousAttackState);

            npc.ai[0] = (int)Main.rand.Next(possibleNextAttacks);
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
            Vector4 averageLight = (topLeftLight.ToVector4() + topRightLight.ToVector4() + bottomLeftLight.ToVector4() + bottomRightLight.ToVector4()) * 0.5f;
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
