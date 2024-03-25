using CalamityMod.DataStructures;
using CalamityMod.NPCs.Abyss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class MorayEelBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<MorayEel>();

        public const int HidingSpotXIndex = 0;

        public const int HidingSpotYIndex = 1;

        public const int InitialSnapDirectionIndex = 2;

        public const int CurrentSnapDirectionIndex = 3;

        public override bool PreAI(NPC npc)
        {
            // Ensure that the eel can target critters.
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 60;
            NPCID.Sets.UsesNewTargetting[npc.type] = true;

            // Be an abyss predator.
            npc.Infernum().IsAbyssPredator = true;

            ref float retreatingToHidingSpot = ref npc.ai[0];
            ref float snapTimer = ref npc.ai[1];
            ref float snapCooldown = ref npc.ai[2];
            ref float peekingOut = ref npc.ai[3];
            ref float initialSnapDirection = ref npc.Infernum().ExtraAI[InitialSnapDirectionIndex];
            ref float currentSnapDirection = ref npc.Infernum().ExtraAI[CurrentSnapDirectionIndex];
            ref float stuckTimer = ref npc.Infernum().ExtraAI[4];

            Vector2 spotToHideIn = new(npc.Infernum().ExtraAI[HidingSpotXIndex], npc.Infernum().ExtraAI[HidingSpotYIndex]);

            // Resize.
            npc.Size = Vector2.One * 24f;

            // Choose an initial tile to hide in.
            if (Main.netMode != NetmodeID.MultiplayerClient && spotToHideIn == Vector2.Zero)
            {
                int tries;
                for (tries = 0; tries < 1800; tries++)
                {
                    int x = (int)(npc.Center.X / 16f) + Main.rand.Next(-25, 25);
                    int y = (int)(npc.Center.Y / 16f) + Main.rand.Next(-25, 25);
                    Tile tile = Framing.GetTileSafely(x, y);

                    // Try again if the tile isn't solid or isn't exposed to air.
                    if (!WorldGen.SolidTile(tile) || !Utilities.IsTileExposedToAir(x, y, out float? angleToOpenAir) || tile.Slope != SlopeType.Solid || tile.IsHalfBlock)
                        continue;

                    // Try again if there's no open water near the tile.
                    Vector2 moveDirection = angleToOpenAir.Value.ToRotationVector2();
                    Vector2 collisionCheckPosition = new Vector2(x * 16f + 8f, y * 16f + 8f) + moveDirection * 16f;
                    float collisionDistance = LumUtils.DistanceToTileCollisionHit(collisionCheckPosition, moveDirection, 20) ?? 20;
                    if (collisionDistance <= 10)
                        continue;

                    npc.Infernum().ExtraAI[HidingSpotXIndex] = x * 16f + 8f;
                    npc.Infernum().ExtraAI[HidingSpotYIndex] = y * 16f + 8f;
                    break;
                }

                // Just die if no spot was suitable.
                if (tries >= 1799)
                    npc.active = false;

                spotToHideIn = new(npc.Infernum().ExtraAI[HidingSpotXIndex], npc.Infernum().ExtraAI[HidingSpotYIndex]);
                npc.Center = spotToHideIn;
                npc.netUpdate = true;
            }

            Utilities.TargetClosestAbyssPredator(npc, false, 500f, 1600f);
            NPCAimedTarget target = npc.GetTargetData();

            // Become invulnerable and mostly transparent if hiding in a tile.
            npc.dontTakeDamage = InHidingSpot(npc);
            npc.Opacity = Clamp(npc.Opacity - npc.dontTakeDamage.ToDirectionInt(), 0.35f, 1f);

            // Also emit some particle effects as an indicator.
            if (InHidingSpot(npc))
            {
                Dust sparkle = Dust.NewDustDirect(spotToHideIn - Vector2.One * 8f, 16, 16, DustID.AncientLight);
                sparkle.color = Color.Lerp(Color.ForestGreen, Color.Gray, Main.rand.NextFloat(0.6f));
                sparkle.velocity = Main.rand.NextVector2Circular(4f, 4f);
                sparkle.noGravity = true;
            }

            // Decide rotation.
            npc.rotation = npc.AngleFrom(spotToHideIn) + PiOver2;

            // Prevent the tile from being destroyed.
            FixExploitManEaters.ProtectSpot((int)(spotToHideIn.X / 16f), (int)(spotToHideIn.Y / 16f));

            // Do nothing other than hiding if instructed to do so.
            if (retreatingToHidingSpot == 1f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(spotToHideIn) * 8f, 0.15f);

                // Stop once the hiding spot has been reached.
                stuckTimer++;
                if (InHidingSpot(npc) || stuckTimer >= 150f)
                {
                    npc.Center = spotToHideIn;
                    npc.velocity = Vector2.Zero;
                    retreatingToHidingSpot = 0f;
                    stuckTimer = 0f;
                    npc.netUpdate = true;
                }

                return false;
            }

            // Don't do any snapping and such if the cooldown is active.
            if (snapCooldown > 0f)
            {
                snapCooldown--;
                if (snapCooldown <= 0f)
                    npc.netUpdate = true;
                return false;
            }

            if (snapTimer > 0f)
            {
                int snapTime = peekingOut == 1f ? 45 : 25;
                float idealSpeed = peekingOut == 1f ? 5f : 26f;
                float newSpeed = Lerp(npc.velocity.Length(), idealSpeed, 0.08f);
                npc.velocity = currentSnapDirection.ToRotationVector2() * newSpeed;

                // Get closer to the target if one not peeking.
                if (peekingOut == 0f && !target.Invalid)
                    currentSnapDirection = currentSnapDirection.AngleTowards(npc.AngleTo(target.Center), 0.0125f);

                // Retreat if velocity is zero for some reason.
                if (npc.velocity == Vector2.Zero)
                {
                    snapTimer = 0f;
                    retreatingToHidingSpot = 1f;
                    npc.netUpdate = true;
                    return false;
                }

                snapTimer++;
                if (snapTimer >= snapTime || (Collision.SolidCollision(npc.Center, 1, 1) && snapTimer > 5f))
                {
                    snapTimer = 0f;
                    snapCooldown = 35f;
                    peekingOut = 0f;
                    retreatingToHidingSpot = 1f;
                }

                return false;
            }

            Vector2 snapDirection = Vector2.UnitY.RotatedBy(PiOver2 * Main.rand.Next(4));

            // Pick a potential direction to snap out.
            // This is important for attacking.
            int snapDirectionTries = 0;
            float targetSnapAngularThreshold = 0.68f;
            while ((LumUtils.DistanceToTileCollisionHit(spotToHideIn, snapDirection, 50) ?? 50f) < 5f)
            {
                snapDirectionTries++;
                snapDirection = snapDirection.RotatedBy(PiOver2);

                if (snapDirectionTries >= 8)
                    return false;

                // Try again if there's a defined target and it isn't in the line of sight of the current direction.
                if (!target.Invalid && snapDirection.AngleBetween(npc.SafeDirectionTo(target.Center)) > targetSnapAngularThreshold)
                    continue;
            }

            // Snap out if a suitable target gets close.
            // Otherwise, sometimes randomly peek out.
            bool canSnapAtTarget =
                !target.Invalid &&
                snapDirection.AngleBetween(npc.SafeDirectionTo(target.Center)) < targetSnapAngularThreshold &&
                Collision.CanHitLine(npc.TopLeft + snapDirection * 12f, npc.width, npc.height, target.Center, target.Width, target.Height);
            if (Main.rand.NextBool(25) && !canSnapAtTarget)
                peekingOut = 1f;

            if (peekingOut == 1f || canSnapAtTarget)
            {
                if (canSnapAtTarget)
                {
                    SoundEngine.PlaySound(SoundID.Item96, npc.Center);
                    peekingOut = 0f;
                }
                else
                    SoundEngine.PlaySound(SoundID.Item95, npc.Center);

                // Add some randomness when peeking.
                if (peekingOut == 1f)
                    snapDirection = snapDirection.RotatedByRandom(Pi / 6f);

                npc.velocity = snapDirection * 4f;
                initialSnapDirection = currentSnapDirection = npc.velocity.ToRotation();
                snapTimer = 1f;
                npc.netUpdate = true;
            }

            return false;
        }

        public static bool InHidingSpot(NPC npc)
        {
            Vector2 hidingSpot = new(npc.Infernum().ExtraAI[HidingSpotXIndex], npc.Infernum().ExtraAI[HidingSpotYIndex]);
            return npc.WithinRange(hidingSpot, 24f) || npc.velocity.Length() < 0.01f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (InHidingSpot(npc))
                return false;

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/MorayEelHead").Value;
            Texture2D body1Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/MorayEelBody1").Value;
            Texture2D body2Texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/MorayEelBody2").Value;

            Vector2 hidingSpot = new(npc.Infernum().ExtraAI[HidingSpotXIndex], npc.Infernum().ExtraAI[HidingSpotYIndex]);
            Vector2 idealDrawPosition = hidingSpot;
            Vector2 backOffset = (npc.rotation - PiOver2).ToRotationVector2() * -18f;
            if (Collision.SolidCollision(idealDrawPosition + backOffset, 4, 4))
                idealDrawPosition += backOffset;

            List<Vector2> bezierPoints =
            [
                idealDrawPosition
            ];

            // Calculate points to create segments at based on a catmull-rom spine.
            float bendFactor = Utils.GetLerpValue(80f, 250f, npc.Distance(idealDrawPosition), true);
            for (int i = 0; i < 20; i++)
            {
                Vector2 leftEnd = idealDrawPosition - npc.Infernum().ExtraAI[InitialSnapDirectionIndex].ToRotationVector2() * bendFactor * 450f;
                Vector2 rightEnd = npc.Center + npc.Infernum().ExtraAI[CurrentSnapDirectionIndex].ToRotationVector2() * bendFactor * 450f;
                bezierPoints.Add(Vector2.CatmullRom(leftEnd, idealDrawPosition, npc.Center, rightEnd, i / 19f));
            }
            bezierPoints.Add(npc.Center);

            // Generalize points with a bezier curve.
            BezierCurve bezierCurve = new([.. bezierPoints]);
            int totalChains = (int)(npc.Distance(idealDrawPosition) / 16f);
            totalChains = (int)Clamp(totalChains, 2f, 100f);
            for (int i = 0; i < totalChains - 1; i++)
            {
                Texture2D textureToUse = (totalChains - i - 1) switch
                {
                    1 => headTexture,
                    2 => body1Texture,
                    _ => body2Texture,
                };
                Vector2 drawPosition = bezierCurve.Evaluate(i / (float)totalChains);
                lightColor = Lighting.GetColor((int)(drawPosition.X / 16f), (int)(drawPosition.Y / 16f));
                float angle = (bezierCurve.Evaluate(i / (float)totalChains + 1f / totalChains) - drawPosition).ToRotation() + PiOver2;
                spriteBatch.Draw(textureToUse, drawPosition - Main.screenPosition + (npc.rotation - PiOver2).ToRotationVector2() * 36f, null, lightColor, angle, textureToUse.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
