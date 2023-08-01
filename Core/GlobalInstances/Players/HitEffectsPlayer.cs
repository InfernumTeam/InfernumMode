using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class HitEffectsPlayer : ModPlayer
    {
        public int HitSoundCountdown
        {
            get;
            set;
        }

        public override void PostUpdate()
        {
            HitSoundCountdown--;
        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            if (npc.type != ModContent.NPCType<AresEnergyKatana>() || hurtInfo.Damage <= 0)
                return;

            // Play hit souds if the countdown has passed.
            if (HitSoundCountdown <= 0)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.AquaticScourgeGoreSound with { Volume = 3f }, Player.Center);
                HitSoundCountdown = 30;
            }

            for (int i = 0; i < 15; i++)
            {
                int bloodLifetime = Main.rand.Next(22, 36);
                float bloodScale = Main.rand.NextFloat(0.6f, 0.8f);
                Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                if (Main.rand.NextBool(20))
                    bloodScale *= 2f;

                Vector2 bloodVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.81f) * Main.rand.NextFloat(11f, 30f);
                bloodVelocity.Y -= 12f;
                BloodParticle blood = new(Player.Center, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                GeneralParticleHandler.SpawnParticle(blood);
            }
            for (int i = 0; i < 25; i++)
            {
                float bloodScale = Main.rand.NextFloat(0.2f, 0.33f);
                Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                Vector2 bloodVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.9f) * Main.rand.NextFloat(9f, 20.5f);
                BloodParticle2 blood = new(Player.Center, bloodVelocity, 20, bloodScale, bloodColor);
                GeneralParticleHandler.SpawnParticle(blood);
            }
        }
    }
}
