using CalamityMod.NPCs;
using CalamityMod.NPCs.Polterghast;
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

namespace InfernumMode.FuckYouModeAIs.Polterghast
{
    public class EerieLimb : ModNPC
    {
        public Vector2 IdealPosition;
        public Limb[] Limbs = new Limb[3];
        public PrimitiveTrailCopy LimbDrawer = null;
        public NPC Polterghast => Main.npc[CalamityGlobalNPC.ghostBoss];
        public Player Target => Main.player[npc.target];
        public int Direction => (npc.ai[0] >= 2f).ToDirectionInt();
        public ref float IdealPositionTimer => ref npc.ai[1];
        public PolterghastAIClass.PolterghastAttackType CurrentAttack => (PolterghastAIClass.PolterghastAttackType)(int)Polterghast.ai[0];
        public float AttackTimer => Polterghast.ai[1];
        public bool Enraged => Polterghast.ai[3] == 1f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Limb");

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = npc.height = 900;
            npc.damage = 225;
            npc.lifeMax = 5000;
            npc.dontTakeDamage = true;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.netAlways = true;
            npc.scale = 1.15f;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(IdealPosition);

        public override void ReceiveExtraAI(BinaryReader reader) => IdealPosition = reader.ReadVector2();

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss) || !Main.npc[CalamityGlobalNPC.ghostBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            if (npc.localAI[0] == 0f)
            {
                Limbs = new Limb[2];
                for (int i = 0; i < Limbs.Length; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit() * i / Limbs.Length * 160f;
                    Limbs[i] = new Limb(npc.Center + direction, direction.ToRotation());
                }
                DecideNewPositionToStickTo();
                npc.localAI[0] = 1f;
            }

            UpdateLimbs();

            if (CurrentAttack == PolterghastAIClass.PolterghastAttackType.Impale)
            {
                if (AttackTimer % 150f == 60f && (npc.ai[0] == 1f || npc.ai[0] == 3f))
                {
                    Vector2 searchDirection = -Polterghast.SafeDirectionTo(Target.Center).RotatedBy((npc.ai[0] == 1f).ToDirectionInt() + MathHelper.PiOver4);
                    IdealPosition = Polterghast.Center + searchDirection * 370f;
                }

                if (AttackTimer % 150f == 90f && Main.netMode != NetmodeID.MultiplayerClient && (npc.ai[0] == 1f || npc.ai[0] == 3f))
                {
                    IdealPosition = Target.Center;
                    npc.velocity = npc.SafeDirectionTo(Target.Bottom + Limbs[1].Rotation.ToRotationVector2() * 100f) * 42f;
                    if (Enraged)
                        npc.velocity *= 1.5f;
                    npc.netUpdate = true;
                }

                if (AttackTimer % 150f >= 90f && npc.WithinRange(IdealPosition, 120f))
                    npc.velocity *= 0.97f;
                else
                    npc.velocity *= 0.985f;

                if (AttackTimer % 150f >= 90f)
                    return;
            }

            float moveSpeed = 28f + Polterghast.velocity.Length() * 1.2f;
            if (!npc.WithinRange(IdealPosition, moveSpeed + 4f))
                npc.velocity = npc.SafeDirectionTo(IdealPosition) * moveSpeed;
            else
            {
                npc.Center = IdealPosition;
                npc.velocity = Vector2.Zero;
            }

            // Make spider patterns during the bestial explosion.
            if (AttackTimer >= 90f && AttackTimer < 315f && CurrentAttack == PolterghastAIClass.PolterghastAttackType.BeastialExplosion)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == 90f)
                {
                    Vector2 searchDirection = Polterghast.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.TwoPi * (npc.ai[0] - 1f) / 4f + MathHelper.PiOver4);
                    IdealPosition = Polterghast.Center + searchDirection * 1600f;
                    npc.netUpdate = true;
                }
                return;
            }

            if (CurrentAttack == PolterghastAIClass.PolterghastAttackType.CloneSplit && AttackTimer % 180f == 30f)
                IdealPositionTimer = 40f;

            if (!npc.WithinRange(Polterghast.Center, 650f))
                IdealPositionTimer++;
            float repositionRate = 30f - Polterghast.velocity.Length() - Utils.InverseLerp(350f, 600f, IdealPositionTimer, true) * 10f;
            repositionRate = MathHelper.Clamp(repositionRate, 5f, 35f);
            if (IdealPositionTimer >= repositionRate)
            {
                DecideNewPositionToStickTo();
                IdealPositionTimer = 0f;
                npc.netUpdate = true;
            }

            npc.target = Polterghast.target;
        }

        public void UpdateLimbs()
        {
            float horizontalTotalDistance = npc.Center.X - Limbs[0].StartingPoint.X;
            float verticalTotalDistance = npc.Center.Y - Limbs[0].StartingPoint.Y;
            float horizontalTotalLengthSquared = (float)Math.Pow(horizontalTotalDistance, 2D);
            float verticalTotalLengthSquared = (float)Math.Pow(verticalTotalDistance, 2D);
            float firstLineLength = Vector2.Distance(Limbs[0].StartingPoint, Limbs[1].StartingPoint);
            float secondLineLength = Vector2.Distance(Limbs[1].StartingPoint, npc.Center);
            float firstLineLengthSquared = (float)Math.Pow(firstLineLength, 2D);
            float secondLineLengthSquared = (float)Math.Pow(secondLineLength, 2D);

            Limbs[1].Rotation = (float)Math.Acos((horizontalTotalLengthSquared + verticalTotalLengthSquared - firstLineLengthSquared - secondLineLengthSquared) / (firstLineLength * secondLineLength * 2f));

            Limbs[0].Rotation = (float)Math.Atan(verticalTotalDistance / horizontalTotalDistance) * -Direction;
            Limbs[0].Rotation -= (float)Math.Atan(secondLineLength * (float)Math.Sin(Limbs[1].Rotation) / (firstLineLength + secondLineLength * (float)Math.Cos(Limbs[1].Rotation)));

            Limbs[0].StartingPoint = Polterghast.Center;

            float firstLimbEndAngleOffset = Math.Abs((float)Math.Atan(verticalTotalDistance / horizontalTotalDistance)) * -Direction;
            firstLimbEndAngleOffset += MathHelper.Pi + MathHelper.PiOver2 * Direction;

            Limbs[1].StartingPoint = Limbs[0].StartingPoint + firstLimbEndAngleOffset.ToRotationVector2() * 240f * Direction;
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

                        int tileType = Main.tile[limbTilePosition.X + dx, limbTilePosition.Y + dy].type;
                        if (!Main.tile[limbTilePosition.X + dx, limbTilePosition.Y + dy].active() || !Main.tileSolid[tileType])
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
                    if (Main.npc[j].type != npc.type || !Main.npc[j].active || j == npc.whoAmI)
                        continue;
                    if (!Main.npc[j].WithinRange(endPosition, 650f) || Main.npc[j].WithinRange(endPosition, 140f))
					{
                        farFromOtherLimbs = true;
                        break;
					}
				}

                if (farFromOtherLimbs)
                    continue;

                if (WorldGen.SolidTile(limbTilePosition.X, limbTilePosition.Y) || (tries >= 17500 && Main.tile[limbTilePosition.X, limbTilePosition.Y].wall > 0))
                {
                    IdealPosition = endPosition;
                    npc.netUpdate = true;
                    return;
                }
            }

            // If no position was found, go to a default position.
            IdealPosition = Polterghast.Center + (MathHelper.TwoPi * npc.ai[0] / 4f).ToRotationVector2() * 570f * Main.rand.NextFloat(0.7f, 1.3f);
            npc.netUpdate = true;
        }

        internal float PrimitiveWidthFunction(float completionRatio)
        {
            float pulse = MathHelper.Lerp(-2f, 6f, (float)Math.Sin(Main.GlobalTime * 2.6f) * 0.5f + 0.5f);
            return MathHelper.Lerp(14f, 16f + pulse, (float)Math.Sin(MathHelper.Pi * completionRatio)) * MathHelper.Clamp(Polterghast.scale, 0.75f, 1.5f);
        }

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            bool actsAsBorder = AttackTimer >= 140f && AttackTimer < 310f && CurrentAttack == PolterghastAIClass.PolterghastAttackType.BeastialExplosion;
            float redFade = 0.35f;

            // Have the legs turn red to signal that they do damage.
            if (AttackTimer >= 90f && CurrentAttack == PolterghastAIClass.PolterghastAttackType.BeastialExplosion)
                redFade += Utils.InverseLerp(90f, 130f, AttackTimer, true) * Utils.InverseLerp(310f, 290f, AttackTimer, true) * 0.32f;

            if (CurrentAttack == PolterghastAIClass.PolterghastAttackType.Impale && npc.ai[0] % 2f == 1f)
                redFade += Utils.InverseLerp(45f, 100f, AttackTimer % 150f, true) * 0.32f;

            Color baseColor = Color.Lerp(Color.Cyan, Color.Red, redFade);
            if (!actsAsBorder)
                baseColor *= Utils.InverseLerp(54f, 45f, Polterghast.ai[2], true);
            return baseColor * Utils.InverseLerp(0.02f, 0.05f, completionRatio, true) * Utils.InverseLerp(0.98f, 0.95f, completionRatio, true);
        }

		public override bool CanHitPlayer(Player target, ref int cooldownSlot)
		{
            if (Polterghast.ai[2] > 48f)
                return false;

            bool actsAsBorder = AttackTimer >= 140f && AttackTimer < 310f && CurrentAttack == PolterghastAIClass.PolterghastAttackType.BeastialExplosion;
            bool impaling = AttackTimer % 150f > 105f && CurrentAttack == PolterghastAIClass.PolterghastAttackType.Impale;
            if (actsAsBorder || impaling)
			{
                float _ = 0f;
                float lineWidth = PrimitiveWidthFunction(0f);
                if (Collision.CheckAABBvLineCollision(target.TopLeft, target.Size, Polterghast.Center, Limbs[1].StartingPoint, lineWidth, ref _))
                    return true;
                if (Collision.CheckAABBvLineCollision(target.TopLeft, target.Size, Limbs[1].StartingPoint, npc.Center, lineWidth, ref _))
                    return true;
            }

            return false;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (LimbDrawer is null)
                LimbDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, GameShaders.Misc["Infernum:PolterghastEctoplasm"]);

            if (Polterghast.ai[2] >= 54f)
                return false;

            GameShaders.Misc["Infernum:PolterghastEctoplasm"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));

            for (int i = 0; i < Limbs.Length; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    GameShaders.Misc["Infernum:PolterghastEctoplasm"].UseOpacity((float)Math.Pow(MathHelper.Lerp(1f, 0.05f, j / 3f), 4D));
                    GameShaders.Misc["Infernum:PolterghastEctoplasm"].UseSaturation(i);
                    if (j > 0 && npc.velocity == Vector2.Zero)
                        continue;

                    Vector2 end = i == Limbs.Length - 1 ? npc.Center - Vector2.UnitY * 10f : Limbs[i + 1].StartingPoint;
                    if (i == Limbs.Length - 1)
                        end -= npc.velocity * j * 0.85f;

                    List<Vector2> drawPositions = new List<Vector2>();
                    for (int k = 0; k <= 8; k++)
                        drawPositions.Add(Vector2.Lerp(Limbs[i].StartingPoint, end, k / 8f));

                    LimbDrawer.Draw(drawPositions, -Main.screenPosition, 38);
                }
            }
            return false;
        }

        public override bool CheckActive() => false;
    }
}
