using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalBoltBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalBolt;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[0]++;
            if (projectile.localAI[0] == 0f)
            {
                if (Main.rand.NextBool(2))
                    SoundEngine.PlaySound(SoundID.Item124, projectile.position);
                else
                    SoundEngine.PlaySound(SoundID.Item125, projectile.position);
                projectile.localAI[0] = 1f;
            }

            projectile.alpha = Utils.Clamp(projectile.alpha - 40, 0, 255);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.velocity = Vector2.Clamp(projectile.velocity, new Vector2(-10f), new Vector2(10f));
            projectile.timeLeft = (int)MathHelper.Min(250 * projectile.MaxUpdates, projectile.timeLeft);

            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                projectile.active = false;
                return false;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            Rectangle collisionArea = core.Infernum().Arena;
            collisionArea.Inflate(-600, -600);

            projectile.tileCollide = projectile.Hitbox.Intersects(collisionArea);

            Dust electrivity = Dust.NewDustDirect(projectile.Center, 0, 0, 229, 0f, 0f, 100, default, 1f);
            electrivity.noLight = true;
            electrivity.noGravity = true;
            electrivity.velocity = projectile.velocity;
            electrivity.position -= Vector2.One * 4f;
            electrivity.scale = 0.8f;

            projectile.frameCounter++;
            if (projectile.frameCounter >= 9)
            {
                projectile.frameCounter = 0;
                projectile.frame++;
                if (projectile.frame >= 5)
                    projectile.frame = 0;
            }
            return false;
        }
    }
}