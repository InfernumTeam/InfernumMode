using CalamityMod;
using InfernumMode;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WayfinderItem = InfernumMode.Content.Items.Wayfinder;

namespace InfernumMode.Content.Projectiles.Wayfinder
{
    public class WayfinderItemProjectile : ModProjectile
    {
        #region Fields + Properties

        public PrimitiveTrailCopy LightDrawer;

        public SlotId SoundID;

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public const int RayCreationTime = 120;

        public const int RayExpandTime = 50;

        public const int IdleDrawTime = 72;

        public const int VisualEffectsDissipateTime = 60;

        public const int Lifetime = RayCreationTime + RayExpandTime + IdleDrawTime + VisualEffectsDissipateTime; // 302

        #endregion

        #region Overrides
        public override string Texture => "InfernumMode/Content/Items/Wayfinder";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wayfinder");
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Slow down after flying upward for long enough.
            if (Time >= 45f)
                Projectile.velocity *= 0.9f;

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            if (Time == 0) // 170
                SoundID = SoundEngine.PlaySound(InfernumSoundRegistry.WayfinderObtainSound, Projectile.Center);

            if (SoundEngine.TryGetActiveSound(SoundID, out ActiveSound s))
            {
                if (s.IsPlaying && s.Position != Projectile.Center)
                    s.Position = Projectile.Center;
            }

            if (Time is < RayCreationTime + RayExpandTime + IdleDrawTime and > 10f)
            {
                float interpolant = (Time - 10f) / (RayCreationTime + RayExpandTime + IdleDrawTime - 10f);
                int amount = (int)Lerp(0, 6f, interpolant);
                float offsetAmount = Lerp(0f, 25f, interpolant);
                float scale = Lerp(0f, 1.3f, interpolant);
                WayfinderHoldout.CreateFlameExplosion(Projectile.Center, offsetAmount, offsetAmount, amount, scale, 30);
            }

            if (Time >= RayCreationTime + RayExpandTime + IdleDrawTime)
            {
                int fireLifetime = 30;
                if (Time is Lifetime)
                    fireLifetime = 60;

                WayfinderHoldout.CreateFlameExplosion(Projectile.Center, 25f, 25f, 30, 1.3f, fireLifetime);
            }

            Projectile.rotation = -PiOver4;

            Time++;
        }

        public override bool? CanDamage() => false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void Kill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            // If server-side, then the item must be spawned for each client individually.
            int itemID = ModContent.ItemType<WayfinderItem>();
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                int item = Item.NewItem(Projectile.GetSource_Loot(), Projectile.Center, itemID, 1, true, -1);
                Main.timeItemSlotCannotBeReusedFor[item] = 18000;
                for (int i = 0; i < Main.maxPlayers; ++i)
                {
                    if (Main.player[i].active)
                        NetMessage.SendData(MessageID.InstancedItem, i, -1, null, item);
                }

                Main.item[item].active = false;
            }

            // Otherwise just drop the item.
            else
                Item.NewItem(Projectile.GetSource_Loot(), Projectile.Center, itemID, 1, true, -1);
        }
        #endregion

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            float dissipateInterpolant = Utils.GetLerpValue(Lifetime, Lifetime - VisualEffectsDissipateTime, Time, true);
            float rayExpandFactor = Lerp(1f, 2f, Clamp((Time - RayCreationTime - RayExpandTime) / 90f, 0f, 1000f)) * dissipateInterpolant;
            DrawBloomCircle(rayExpandFactor);
            DrawLightRays();

            Main.spriteBatch.ExitShaderRegion();
            DrawWayfinder();
            return false;
        }

        public float DrawLightRays()
        {
            // Draw a bunch of god rays.
            float dissipateInterpolant = Utils.GetLerpValue(Lifetime, Lifetime - VisualEffectsDissipateTime, Time, true);
            float totalDeathRays = Lerp(0f, 8f, Utils.GetLerpValue(0f, RayCreationTime, Time, true)) * dissipateInterpolant;
            float rayExpandFactor = Lerp(1f, 2f, Clamp((Time - RayCreationTime - RayExpandTime) / 90f, 0f, 1000f)) * dissipateInterpolant;

            for (int i = 0; i < (int)totalDeathRays; i++)
            {
                float rayAnimationCompletion = 1f;
                if (i == (int)totalDeathRays - 1f)
                    rayAnimationCompletion = totalDeathRays - (int)totalDeathRays;
                rayAnimationCompletion *= rayExpandFactor;

                ulong seed = (ulong)(i + 1) * 3141592uL;
                float rayDirection = TwoPi * i / 8f + Sin(Main.GlobalTimeWrappedHourly * (i + 1f) * 0.3f) * 0.51f;
                rayDirection += Main.GlobalTimeWrappedHourly * 0.48f;
                DrawLightRay(seed, rayDirection, rayAnimationCompletion, Projectile.Center);
            }

            return rayExpandFactor;
        }

        public void DrawWayfinder()
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, drawPosition, frame, Color.White * 0.7f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
        }

        public void DrawBloomCircle(float rayExpandFactor)
        {
            // Create bloom over the waypoint.
            float dissipateInterpolant = Utils.GetLerpValue(Lifetime, Lifetime - VisualEffectsDissipateTime, Time, true);
            float bloomInterpolant = Utils.GetLerpValue(0f, RayCreationTime * 0.67f, Time, true) * dissipateInterpolant;
            if (bloomInterpolant > 0f)
            {
                Texture2D bloomCircle = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/THanosAura").Value;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                Vector2 bloomSize = new Vector2(200f) / bloomCircle.Size() * Pow(bloomInterpolant, 2f);
                bloomSize *= 1f + (rayExpandFactor - 1f) * 2f;

                Main.spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Orange * bloomInterpolant, 0f, bloomCircle.Size() * 0.5f, bloomSize, 0, 0f);
                Main.spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Orange * bloomInterpolant, 0f, bloomCircle.Size() * 0.5f, bloomSize * 0.8f, 0, 0f);
                Main.spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Yellow * bloomInterpolant, 0f, bloomCircle.Size() * 0.5f, bloomSize * 0.55f, 0, 0f);
                Main.spriteBatch.Draw(bloomCircle, drawPosition, null, Color.Wheat * bloomInterpolant, 0f, bloomCircle.Size() * 0.5f, bloomSize * 0.5f, 0, 0f);
            }
        }

        public void DrawLightRay(ulong seed, float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            // Parameters are not correctly passed into the delegates after the primitive drawer is created.
            // As a substitute, a direct NPC variable is used as storage to allow for access.
            Projectile.Infernum().ExtraAI[8] = rayBrightness;

            float rayWidthFunction(float completionRatio, float rayBrightness2)
            {
                return Lerp(2f, 14f, completionRatio) * (1f + (rayBrightness2 - 1f) * 1.6f);
            }
            Color rayColorFunction(float completionRatio, float rayBrightness2)
            {
                float dissipateInterpolant = Utils.GetLerpValue(Lifetime, Lifetime - VisualEffectsDissipateTime, Time, true);
                return Color.White * Projectile.Opacity * Utils.GetLerpValue(0.8f, 0.5f, completionRatio, true) * Clamp(0f, 0.65f, rayBrightness2) * dissipateInterpolant;
            }

            LightDrawer ??= new PrimitiveTrailCopy(c => rayWidthFunction(c, Projectile.Infernum().ExtraAI[8]), c => rayColorFunction(c, Projectile.Infernum().ExtraAI[8]), null, false);

            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();
            float length = Lerp(125f, 220f, Utils.RandomFloat(ref seed)) * rayBrightness;
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightDrawer.Draw(points, -Main.screenPosition, 47);
        }
        #endregion
    }
}