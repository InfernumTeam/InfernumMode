using CalamityMod.Buffs.DamageOverTime;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class AccessoryPlayer : ModPlayer
    {
        public bool Purity;

        public override void ResetEffects()
        {
            Purity = false;
        }

        public override void UpdateDead()
        {
            Purity = false;
        }

        public override void PostUpdateEquips()
        {
            if (Purity)
            {
                Player.GetDamage<GenericDamageClass>() += 0.3f;
                Player.GetAttackSpeed<GenericDamageClass>() += 0.3f;
                Player.buffImmune[ModContent.BuffType<Nightwither>()] = true;
            }
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Item, consider using ModifyHitNPC instead */
        {
            if (Purity)
                modifiers.DisableCrit();
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)/* tModPorter If you don't need the Projectile, consider using ModifyHitNPC instead */
        {
            if (Purity)
                modifiers.DisableCrit();
        }
    }
}
