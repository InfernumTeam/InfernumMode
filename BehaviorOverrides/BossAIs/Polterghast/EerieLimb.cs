using CalamityMod.NPCs;
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
    public class EerieLimb : ModNPC
    {
        public Vector2 IdealPosition;
        public LimbCollection Limbs;
        public PrimitiveTrailCopy LimbDrawer = null;
        public NPC Polterghast => Main.npc[CalamityGlobalNPC.ghostBoss];
        public Player Target => Main.player[NPC.target];
        public int Direction => (NPC.ai[0] >= 2f).ToDirectionInt();
        public ref float IdealPositionTimer => ref NPC.ai[1];
        public PolterghastBehaviorOverride.PolterghastAttackType CurrentAttack => (PolterghastBehaviorOverride.PolterghastAttackType)(int)Polterghast.ai[0];
        public float AttackTimer => Polterghast.ai[1];
        public bool Enraged => Polterghast.ai[3] == 1f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Limb");

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 900;
            NPC.damage = 225;
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
                Limbs = new LimbCollection(new ModifiedCyclicCoordinateDescentUpdateRule(0.15f, MathHelper.PiOver2), 160f, 160f, 160f);
                DecideNewPositionToStickTo();
                NPC.localAI[0] = 1f;
            }

            Limbs.Update(Polterghast.Center, IdealPosition);

            float moveSpeed = 28f + Polterghast.velocity.Length() * 1.2f;
            if (!NPC.WithinRange(IdealPosition, moveSpeed + 4f))
                NPC.velocity = NPC.SafeDirectionTo(IdealPosition) * moveSpeed;
            else
            {
                NPC.Center = IdealPosition;
                NPC.velocity = Vector2.Zero;
            }

            // Make spider patterns during the bestial explosion.
            if (AttackTimer >= 90f && AttackTimer < 315f && CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.BeastialExplosion)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == 90f)
                {
                    Vector2 searchDirection = Polterghast.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.TwoPi * (NPC.ai[0] - 1f) / 4f + MathHelper.PiOver4);
                    IdealPosition = Polterghast.Center + searchDirection * 1600f;
                    NPC.netUpdate = true;
                }
                return;
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

            NPC.target = Polterghast.target;
        }

        public void DecideNewPositionToStickTo()
        {
            for (int tries = 0; tries < 20000; tries++)
            {
                int checkArea = (int)(30f * (tries / 20000f)) + 25;
                Point limbTilePosition = (Polterghast.Center / 16f + Main.rand.NextVector2Square(-checkArea, checkArea)).ToPoint();

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

                if (!Polterghast.WithinRange(endPosition, 650f))
                    continue;

                if (Math.Abs(MathHelper.WrapAngle(Polterghast.AngleTo(endPosition) - MathHelper.Pi * Direction)) > 0.5f)
                    continue;

                if (tries < 15000 && !Collision.CanHitLine(Polterghast.Center, 16, 16, endPosition, 16, 16))
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
            float pulse = MathHelper.Lerp(-0.4f, 2f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.6f) * 0.5f + 0.5f);
            return MathHelper.Lerp(9.5f, 12f + pulse, (float)Math.Sin(MathHelper.Pi * completionRatio)) * MathHelper.Clamp(Polterghast.scale, 0.75f, 1.5f);
        }

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            bool actsAsBorder = AttackTimer >= 140f && AttackTimer < 310f && CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.BeastialExplosion;
            float redFade = 0.35f;

            // Have the legs turn red to signal that they do damage.
            if (AttackTimer >= 90f && CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.BeastialExplosion)
                redFade += Utils.GetLerpValue(90f, 130f, AttackTimer, true) * Utils.GetLerpValue(310f, 290f, AttackTimer, true) * 0.32f;

            if (CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.Impale && NPC.ai[0] % 2f == 1f)
                redFade += Utils.GetLerpValue(45f, 100f, AttackTimer % 150f, true) * 0.32f;

            Color baseColor = Color.Lerp(Color.Cyan, Color.Red, redFade);
            if (!actsAsBorder)
                baseColor *= Utils.GetLerpValue(54f, 45f, Polterghast.ai[2], true);
            return baseColor * Utils.GetLerpValue(0.02f, 0.1f, completionRatio, true) * Utils.GetLerpValue(0.98f, 0.9f, completionRatio, true);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (Polterghast.ai[2] > 48f)
                return false;

            bool actsAsBorder = AttackTimer >= 140f && AttackTimer < 310f && CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.BeastialExplosion;
            bool impaling = AttackTimer % 150f > 105f && CurrentAttack == PolterghastBehaviorOverride.PolterghastAttackType.Impale;
            if (actsAsBorder || impaling)
            {
                float _ = 0f;
                float lineWidth = PrimitiveWidthFunction(0f);
                for (int i = 0; i < Limbs.Limbs.Length; i++)
                {
                    if (Collision.CheckAABBvLineCollision(target.TopLeft, target.Size, Limbs.Limbs[i].ConnectPoint, Limbs.Limbs[i].EndPoint, lineWidth, ref _))
                        return true;
                }
            }

            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            if (LimbDrawer is null)
                LimbDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, GameShaders.Misc["Infernum:PolterghastEctoplasm"]);

            if (Polterghast.ai[2] >= 54f)
                return false;

            GameShaders.Misc["Infernum:PolterghastEctoplasm"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            if (Limbs is null)
                return false;

            for (int i = 0; i < Limbs.Limbs.Length; i++)
            {
                if (Limbs.Limbs[i] is null)
                    return false;

                Vector2 offsetToNext = Vector2.Zero;
                if (i < Limbs.Limbs.Length - 1)
                    offsetToNext = Limbs.Limbs[i + 1].EndPoint - Limbs.Limbs[i].EndPoint;
                Vector2 directionToNext = offsetToNext.SafeNormalize(Vector2.Zero);

                for (int j = 4; j >= 0; j--)
                {
                    GameShaders.Misc["Infernum:PolterghastEctoplasm"].UseOpacity((float)Math.Pow(MathHelper.Lerp(0.9f, 0.05f, j / 4f), 4D));
                    GameShaders.Misc["Infernum:PolterghastEctoplasm"].UseSaturation(i);

                    if (j > 0 && NPC.velocity == Vector2.Zero)
                        continue;

                    Vector2 end = i == Limbs.Limbs.Length - 1 ? NPC.Center - Vector2.UnitY * 10f : Limbs.Limbs[i + 1].ConnectPoint;
                    if (i == Limbs.Limbs.Length - 1)
                        end -= NPC.velocity * j * 0.85f;
                    end += directionToNext * Utils.GetLerpValue(15f, 175f, offsetToNext.Length(), true) * 20f;

                    List<Vector2> drawPositions = new();
                    for (int k = 0; k < 20; k++)
                        drawPositions.Add(Vector2.Lerp(Limbs.Limbs[i].ConnectPoint, end, k / 19f));

                    LimbDrawer.Draw(drawPositions, -Main.screenPosition, 38);
                }
            }
            return false;
        }

        public override bool CheckActive() => false;
    }
}
