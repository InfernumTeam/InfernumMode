using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class TerminusAnimationProj : ModProjectile
    {
        public SlotId ChargeSound = SlotId.Invalid;

        public PrimitiveTrailCopy LightDrawer;

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 540;

        public const int UpwardRiseTime = 96;

        public const int AEWSpawnDelay = 210;

        public override string Texture => "CalamityMod/Items/SummonItems/Terminus";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Die if Infernum is not active.
            if (!InfernumMode.CanUseCustomAIs)
            {
                Projectile.Kill();
                return;
            }

            // Play the charge sound if it has not been started yet.
            if (!ChargeSound.IsValid)
                ChargeSound = SoundEngine.PlaySound(BossRushEvent.TerminusActivationSound with { IsLooped = true }, Projectile.Center);

            // Update the charge sound's position.
            if (SoundEngine.TryGetActiveSound(ChargeSound, out var sound))
                sound.Position = Projectile.Center;

            Time++;

            // Rise upward.
            float upwardRiseSpeed = Utils.GetLerpValue(UpwardRiseTime, 8f, Time, true) * 5f;
            Projectile.velocity = -Vector2.UnitY * upwardRiseSpeed;

            // Summon the wyrm after enough time has passed. After it is spawned it will attempt to snatch the Terminus.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time == AEWSpawnDelay)
                CalamityUtils.SpawnBossBetter(Projectile.Center - Vector2.UnitY * 1000f, ModContent.NPCType<PrimordialWyrmHead>());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 baseDrawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color baseColor = Color.Lerp(Projectile.GetAlpha(lightColor), Color.White, Utils.GetLerpValue(10f, 45f, Time, true));
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw light rays.
            DrawLightRays();

            Main.EntitySpriteDraw(texture, baseDrawPosition, null, baseColor, 0f, origin, Projectile.scale, direction, 0);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            // Terminate the charge sound once the projectile is destroyed.
            if (SoundEngine.TryGetActiveSound(ChargeSound, out var sound))
                sound.Stop();
        }

        public void DrawLightRays()
        {
            Main.spriteBatch.EnterShaderRegion();

            float totalDeathRays = Lerp(0f, 8f, Utils.GetLerpValue(UpwardRiseTime, 180f, Time, true));
            float rayExpandFactor = Lerp(1f, 2f, Clamp((Time - 150f) / 50f, 0f, 1f));

            // Make the light rays dissipate if the AEW is close. This is done to ensure that the light rays don't suddenly vanish the
            // instant the Terminus is snatched, which would look a bit weird.
            float aewProximityOpacityFade = 1f;
            int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<PrimordialWyrmHead>());
            if (aewIndex >= 0 && Main.npc[aewIndex].active)
                aewProximityOpacityFade = Utils.GetLerpValue(200f, 780f, Projectile.Distance(Main.npc[aewIndex].Center), true);

            for (int i = 0; i < (int)totalDeathRays; i++)
            {
                float rayAnimationCompletion = 1f;
                if (i == (int)totalDeathRays - 1f)
                    rayAnimationCompletion = totalDeathRays - (int)totalDeathRays;
                rayAnimationCompletion *= rayExpandFactor * aewProximityOpacityFade;

                ulong seed = (ulong)(i + 1) * 177195uL;
                float rayDirection = TwoPi * i / 8f + Sin(Main.GlobalTimeWrappedHourly * (i + 1f) * 0.3f) * 0.51f;
                rayDirection += Main.GlobalTimeWrappedHourly * 0.48f;
                DrawLightRay(seed, rayDirection, rayAnimationCompletion, Projectile.Center);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

        public void DrawLightRay(ulong seed, float initialRayRotation, float rayBrightness, Vector2 rayStartingPoint)
        {
            // Parameters are not correctly passed into the delegates after the primitive drawer is created.
            // As a substitute, a direct AI variable is used as storage to allow for access.
            Projectile.localAI[0] = rayBrightness;

            float rayWidthFunction(float completionRatio, float rayBrightness2)
            {
                return Lerp(2f, 14f, completionRatio) * (1f + (rayBrightness2 - 1f) * 1.6f);
            }
            Color rayColorFunction(float completionRatio, float rayBrightness2)
            {
                return Color.White * Projectile.Opacity * Utils.GetLerpValue(0.8f, 0.5f, completionRatio, true) * Clamp(0f, 1.5f, rayBrightness2) * 0.6f;
            }

            LightDrawer ??= new PrimitiveTrailCopy(c => rayWidthFunction(c, Projectile.localAI[0]), c => rayColorFunction(c, Projectile.localAI[0]), null, false);

            Vector2 currentRayDirection = initialRayRotation.ToRotationVector2();
            float length = Lerp(225f, 360f, Utils.RandomFloat(ref seed)) * rayBrightness;
            List<Vector2> points = new();
            for (int i = 0; i <= 12; i++)
                points.Add(Vector2.Lerp(rayStartingPoint, rayStartingPoint + initialRayRotation.ToRotationVector2() * length, i / 12f));

            LightDrawer.Draw(points, -Main.screenPosition, 47);
        }
    }
}
