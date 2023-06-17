using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DukeFishron
{
    public class SharkronBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Sharkron2;

        public override bool PreAI(NPC npc)
        {
            npc.Infernum().ExtraAI[0]++;
            npc.noTileCollide = npc.Infernum().ExtraAI[0] < 90f;
            npc.noGravity = npc.noTileCollide;
            if (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height + 24, true) && !npc.noTileCollide)
            {
                if (npc.DeathSound != null)
                    SoundEngine.PlaySound(npc.DeathSound, npc.position);
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
            }

            // Fade in.
            npc.Opacity = Clamp(npc.Opacity + 0.06f, 0f, 1f);
            npc.rotation = npc.velocity.ToRotation();

            return false;
        }
    }
}
