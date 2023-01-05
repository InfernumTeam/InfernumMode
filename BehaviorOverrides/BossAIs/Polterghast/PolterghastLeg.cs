using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.NPCs;
using InfernumMode.Graphics;
using InfernumMode.InverseKinematics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastLeg : ModNPC, IPixelPrimitiveDrawer
    {
        public Vector2 IdealPosition;

        public LimbCollection Limbs;

        public PrimitiveTrailCopy LimbDrawer = null;

        public Player Target => Main.player[NPC.target];

        public int Direction => (NPC.ai[0] >= 2f).ToDirectionInt();

        public ref float IdealPositionTimer => ref NPC.ai[1];

        public ref float FadeToRed => ref NPC.localAI[1];

        public static PolterghastBehaviorOverride.PolterghastAttackType CurrentAttack => (PolterghastBehaviorOverride.PolterghastAttackType)(int)Polterghast.ai[0];

        public static float AttackTimer => Polterghast.ai[1];

        public static bool Enraged => Polterghast.ai[3] == 1f;

        public static NPC Polterghast => Main.npc[CalamityGlobalNPC.ghostBoss];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Ghostly Leg");
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 1800;
            NPC.damage = 245;
            NPC.lifeMax = 5000;
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.scale = 1.15f;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(IdealPosition);

        public override void ReceiveExtraAI(BinaryReader reader) => IdealPosition = reader.ReadVector2();

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss) || !Main.npc[CalamityGlobalNPC.ghostBoss].active)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            if (NPC.localAI[0] == 0f)
            {
                Limbs = new LimbCollection(new ModifiedCyclicCoordinateDescentUpdateRule(0.15f, MathHelper.PiOver4), 160f, 160f, 160f);
                DecideNewPositionToStickTo();
                NPC.localAI[0] = 1f;
            }

            Limbs.Update(Polterghast.Center, NPC.Center);

            bool isBeingControlled = Polterghast.Infernum().ExtraAI[9] == NPC.whoAmI;
            float moveSpeed = 28f + Polterghast.velocity.Length() * 1.2f;

            // Reposition and move to the ideal position if not being manually controlled in Polter's AI.
            NPC.damage = NPC.defDamage;
            if (!isBeingControlled)
            {
                NPC.damage = 0;

                if (!NPC.WithinRange(IdealPosition, moveSpeed + 4f))
                    NPC.velocity = NPC.SafeDirectionTo(IdealPosition) * moveSpeed;
                else
                {
                    NPC.Center = IdealPosition;
                    NPC.velocity = Vector2.Zero;
                }

                if (CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.CloneSplit && AttackTimer % 180f == 30f)
                    IdealPositionTimer = 40f;

                if (!NPC.WithinRange(Polterghast.Center, 650f))
                    IdealPositionTimer++;
                float repositionRate = 30f - Polterghast.velocity.Length() - Utils.GetLerpValue(350f, 600f, IdealPositionTimer, true) * 10f;
                repositionRate = MathHelper.Clamp(repositionRate, 5f, 35f);
                if (IdealPositionTimer >= repositionRate)
                {
                    DecideNewPositionToStickTo();
                    IdealPositionTimer = 0f;
                    NPC.netUpdate = true;
                }
            }

            // Fade to red based on whether the leg is being controlled.
            FadeToRed = MathHelper.Clamp(FadeToRed + isBeingControlled.ToDirectionInt(), 0.3f, 1f);

            NPC.target = Polterghast.target;
        }

        public void DecideNewPositionToStickTo()
        {
            for (int tries = 0; tries < 20000; tries++)
            {
                int checkArea = (int)(40f * (tries / 20000f)) + 25;
                Point limbTilePosition = (Polterghast.Center / 16f + (MathHelper.TwoPi * NPC.ai[0] / 4f).ToRotationVector2().RotatedByRandom(0.69f) * Main.rand.NextFloat(checkArea)).ToPoint();

                if (!WorldGen.InWorld(limbTilePosition.X, limbTilePosition.Y, 4))
                    continue;

                // Only stick to a tile if it has at least 5 exposed tiles.
                int exposedTiles = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int tileType = Main.tile[limbTilePosition.X + dx, limbTilePosition.Y + dy].TileType;
                        if (!Main.tile[limbTilePosition.X + dx, limbTilePosition.Y + dy].HasTile || !Main.tileSolid[tileType])
                            exposedTiles++;
                    }
                }

                if (exposedTiles < 3)
                    continue;

                Vector2 endPosition = limbTilePosition.ToWorldCoordinates(8, 16);

                if (!Polterghast.WithinRange(endPosition, 720f))
                    continue;

                if (Math.Abs(MathHelper.WrapAngle(Polterghast.AngleTo(endPosition) - MathHelper.Pi * Direction)) > 0.5f)
                    continue;

                bool outOfGeneralDirection = Polterghast.SafeDirectionTo(endPosition).AngleBetween((MathHelper.TwoPi * NPC.ai[0] / 4f).ToRotationVector2()) > 1.05f; 
                if (tries < 15000 && (!Collision.CanHitLine(Polterghast.Center, 16, 16, endPosition, 16, 16) || outOfGeneralDirection))
                    continue;

                bool farFromOtherLimbs = false;
                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    if (Main.npc[j].type != NPC.type || !Main.npc[j].active || j == NPC.whoAmI)
                        continue;
                    if (!Main.npc[j].WithinRange(endPosition, 650f) || Main.npc[j].WithinRange(endPosition, 140f))
                    {
                        farFromOtherLimbs = true;
                        break;
                    }
                }

                if (farFromOtherLimbs)
                    continue;

                if (WorldGen.SolidTile(limbTilePosition.X, limbTilePosition.Y) || (tries >= 17500 && Main.tile[limbTilePosition.X, limbTilePosition.Y].WallType > 0))
                {
                    IdealPosition = endPosition;
                    NPC.netUpdate = true;
                    return;
                }
            }

            // If no position was found, go to a default position.
            IdealPosition = Polterghast.Center + (MathHelper.TwoPi * NPC.ai[0] / 4f).ToRotationVector2() * 570f * Main.rand.NextFloat(0.7f, 1.3f);
            if (!Polterghast.WithinRange(IdealPosition, 400f))
                IdealPosition = Polterghast.Center + Polterghast.SafeDirectionTo(IdealPosition) * 400f;

            NPC.netUpdate = true;
        }

        internal float PrimitiveWidthFunction(float completionRatio)
        {
            float fadeToMax = MathHelper.Lerp(0f, 1f, (float)Math.Sin(MathHelper.Pi * completionRatio) * (NPC.localAI[2] == 1f ? 0.5f : 1f));
            float pulse = MathHelper.Lerp(-0.4f, 2f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.6f) * 0.5f + 0.5f);
            return MathHelper.Lerp(9.5f, 12f + pulse, fadeToMax) * MathHelper.Clamp(Polterghast.scale, 0.75f, 1.5f);
        }

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.Cyan, Color.HotPink, FadeToRed) * Utils.GetLerpValue(54f, 45f, Polterghast.ai[2], true);
            return baseColor * Utils.GetLerpValue(0.02f, 0.1f, completionRatio, true) * Utils.GetLerpValue(0.98f, 0.9f, completionRatio, true);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            for (int i = 0; i < Limbs.Limbs.Length; i++)
            {
                if (Collision.CheckAABBvLineCollision(target.TopLeft, target.Size, Limbs.Limbs[i].ConnectPoint, Limbs.Limbs[i].EndPoint))
                    return true;
            }
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
           LimbDrawer ??= new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, InfernumEffectsRegistry.PolterghastEctoplasmVertexShader);

            if (Polterghast.ai[2] >= 54f)
                return;

            if (Limbs is null)
                return;

            InfernumEffectsRegistry.PolterghastEctoplasmVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < Limbs.Limbs.Length; i++)
            {
                NPC.localAI[2] = 0f;
                if (Limbs.Limbs[i] is null)
                    return;

                Vector2 offsetToNext = Vector2.Zero;
                if (i < Limbs.Limbs.Length - 1)
                {
                    offsetToNext = Limbs.Limbs[i + 1].EndPoint - Limbs.Limbs[i].EndPoint;
                    NPC.localAI[2] = 0f;
                }

                Vector2 directionToNext = offsetToNext.SafeNormalize(Vector2.Zero);

                for (int j = 4; j >= 0; j--)
                {
                    InfernumEffectsRegistry.PolterghastEctoplasmVertexShader.UseOpacity((float)Math.Pow(MathHelper.Lerp(0.9f, 0.05f, j / 4f), 4D));
                    InfernumEffectsRegistry.PolterghastEctoplasmVertexShader.UseSaturation(i);

                    if (j > 0 && NPC.velocity == Vector2.Zero)
                        continue;

                    Vector2 end = i == Limbs.Limbs.Length - 1 ? NPC.Center - Vector2.UnitY * 10f : Limbs.Limbs[i + 1].ConnectPoint;
                    if (i == Limbs.Limbs.Length - 1)
                        end -= NPC.velocity * j * 0.85f;
                    end += directionToNext * Utils.GetLerpValue(15f, 175f, offsetToNext.Length(), true) * 20f;

                    List<Vector2> drawPositions = new();
                    for (int k = 0; k < 10; k++)
                        drawPositions.Add(Vector2.Lerp(Limbs.Limbs[i].ConnectPoint, end, k / 9f));

                    LimbDrawer.DrawPixelated(drawPositions, -Main.screenPosition, 40);
                }
            }
            spriteBatch.ResetBlendState();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;

        public override bool CheckActive() => false;
    }
}
