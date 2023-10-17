using CalamityMod.CalPlayer;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public partial class InfernumPlayer : ModPlayer
    {
        #region Delegates
        public delegate void PlayerActionDelegate(InfernumPlayer player);

        public delegate void DataDelegate(InfernumPlayer player, TagCompound tag);

        public delegate void ModifyHurtDelegate(InfernumPlayer player, ref Player.HurtModifiers modifiers);

        public delegate void ModifyHitByProjectileDelegate(InfernumPlayer player, Projectile proj, ref Player.HurtModifiers modifiers);

        public delegate void ModifyHitByNPCDelegate(InfernumPlayer player, NPC npc, ref Player.HurtModifiers modifiers);

        public delegate void ModifyHitNPCWithItemDelegate(InfernumPlayer player, Item item, NPC target, ref NPC.HitModifiers modifiers);

        public delegate void ModifyHitNPCWithProjDelegate(InfernumPlayer player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers);

        public delegate bool FreeDodgeDelegate(InfernumPlayer player, Player.HurtInfo info);

        public delegate bool PreKillDelegate(InfernumPlayer player, double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource);

        public delegate void OnHitByNPCDelegate(InfernumPlayer player, NPC npc, Player.HurtInfo hurtInfo);

        public delegate void KillDelegate(InfernumPlayer player, double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource);
        #endregion

        #region Events
        public static event PlayerActionDelegate ResetEffectsEvent;

        public static event PlayerActionDelegate UpdateDeadEvent;

        public static event DataDelegate LoadDataEvent;

        public static event DataDelegate SaveDataEvent;

        public static event PlayerActionDelegate PreUpdateEvent;

        public static event PlayerActionDelegate AccessoryUpdateEvent;

        public static event PlayerActionDelegate MovementUpdateEvent;

        public static event PlayerActionDelegate PostUpdateEvent;

        public static event ModifyHurtDelegate ModifyHurtEvent;

        public static event ModifyHitByProjectileDelegate ModifyHitByProjectileEvent;

        public static event ModifyHitByNPCDelegate ModifyHitByNPCEvent;

        public static event ModifyHitNPCWithItemDelegate ModifyHitNPCWithItemEvent;

        public static event ModifyHitNPCWithProjDelegate ModifyHitNPCWithProjEvent;

        public static event FreeDodgeDelegate FreeDodgeEvent;

        public static event PreKillDelegate PreKillEvent;

        public static event PlayerActionDelegate UpdateLifeRegenEvent;

        public static event OnHitByNPCDelegate OnHitByNPCEvent;

        public static event PlayerActionDelegate OnEnterWorldEvent;

        public static event KillDelegate KillEvent;
        #endregion

        #region Overrides
        public override void Unload()
        {
            if (ResetEffectsEvent != null)
                foreach (var subscription in ResetEffectsEvent.GetInvocationList())
                    ResetEffectsEvent -= (PlayerActionDelegate)subscription;

            if (UpdateDeadEvent != null)
                foreach (var subscription in UpdateDeadEvent.GetInvocationList())
                    UpdateDeadEvent -= (PlayerActionDelegate)subscription;

            if (LoadDataEvent != null)
                foreach (var subscription in LoadDataEvent.GetInvocationList())
                    LoadDataEvent -= (DataDelegate)subscription;

            if (SaveDataEvent != null)
                foreach (var subscription in SaveDataEvent.GetInvocationList())
                    SaveDataEvent -= (DataDelegate)subscription;

            if (PreUpdateEvent != null)
                foreach (var subscription in PreUpdateEvent.GetInvocationList())
                    PreUpdateEvent -= (PlayerActionDelegate)subscription;

            if (AccessoryUpdateEvent != null)
                foreach (var subscription in AccessoryUpdateEvent.GetInvocationList())
                    AccessoryUpdateEvent -= (PlayerActionDelegate)subscription;

            if (MovementUpdateEvent != null)
                foreach (var subscription in MovementUpdateEvent.GetInvocationList())
                    MovementUpdateEvent -= (PlayerActionDelegate)subscription;

            if (PostUpdateEvent != null)
                foreach (var subscription in PostUpdateEvent.GetInvocationList())
                    PostUpdateEvent -= (PlayerActionDelegate)subscription;

            if (ModifyHurtEvent != null)
                foreach (var subscription in ModifyHurtEvent.GetInvocationList())
                    ModifyHurtEvent -= (ModifyHurtDelegate)subscription;

            if (ModifyHitByProjectileEvent != null)
                foreach (var subscription in ModifyHitByProjectileEvent.GetInvocationList())
                    ModifyHitByProjectileEvent -= (ModifyHitByProjectileDelegate)subscription;

            if (ModifyHitByNPCEvent != null)
                foreach (var subscription in ModifyHitByNPCEvent.GetInvocationList())
                    ModifyHitByNPCEvent -= (ModifyHitByNPCDelegate)subscription;

            if (ModifyHitNPCWithItemEvent != null)
                foreach (var subscription in ModifyHitNPCWithItemEvent.GetInvocationList())
                    ModifyHitNPCWithItemEvent -= (ModifyHitNPCWithItemDelegate)subscription;

            if (ModifyHitNPCWithProjEvent != null)
                foreach (var subscription in ModifyHitNPCWithProjEvent.GetInvocationList())
                    ModifyHitNPCWithProjEvent -= (ModifyHitNPCWithProjDelegate)subscription;

            if (FreeDodgeEvent != null)
                foreach (var subscription in FreeDodgeEvent.GetInvocationList())
                    FreeDodgeEvent -= (FreeDodgeDelegate)subscription;

            if (PreKillEvent != null)
                foreach (var subscription in PreKillEvent.GetInvocationList())
                    PreKillEvent -= (PreKillDelegate)subscription;

            if (UpdateLifeRegenEvent != null)
                foreach (var subcription in UpdateLifeRegenEvent.GetInvocationList())
                    UpdateLifeRegenEvent -= (PlayerActionDelegate)subcription;

            if (OnHitByNPCEvent != null)
                foreach (var subcription in OnHitByNPCEvent.GetInvocationList())
                    OnHitByNPCEvent -= (OnHitByNPCDelegate)subcription;

            if (OnEnterWorldEvent != null)
                foreach (var subcription in OnEnterWorldEvent.GetInvocationList())
                    OnEnterWorldEvent -= (PlayerActionDelegate)subcription;

            if (KillEvent != null)
                foreach (var subcription in KillEvent.GetInvocationList())
                    KillEvent -= (KillDelegate)subcription;
        }

        public override void ResetEffects()
        {
            ResetEffectsEvent?.Invoke(this);
        }

        public override void UpdateDead()
        {
            ResetEffectsEvent?.Invoke(this);
            UpdateDeadEvent?.Invoke(this);
        }

        public override void LoadData(TagCompound tag)
        {
            LoadDataEvent?.Invoke(this, tag);
        }

        public override void SaveData(TagCompound tag)
        {
            SaveDataEvent?.Invoke(this, tag);
        }

        public override void PreUpdate()
        {
            PreUpdateEvent?.Invoke(this);
        }

        public override void PreUpdateMovement()
        {
            MovementUpdateEvent?.Invoke(this);
        }

        public override void PostUpdateEquips()
        {
            AccessoryUpdateEvent?.Invoke(this);
        }

        public override void PostUpdate()
        {
            PostUpdateEvent?.Invoke(this);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            ModifyHurtEvent?.Invoke(this, ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            ModifyHitByProjectileEvent?.Invoke(this, proj, ref modifiers);
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            ModifyHitByNPCEvent?.Invoke(this, npc, ref modifiers);
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            ModifyHitNPCWithItemEvent?.Invoke(this, item, target, ref modifiers);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            ModifyHitNPCWithProjEvent?.Invoke(this, proj, target, ref modifiers);
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            bool result = false;
            foreach (var subscription in FreeDodgeEvent?.GetInvocationList())
                result |= ((FreeDodgeDelegate)subscription).Invoke(this, info);
               
            return result;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            bool die = false;

            foreach (var subscription in PreKillEvent?.GetInvocationList())
                die |= ((PreKillDelegate)subscription).Invoke(this, damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
            return die;
        }

        public override void UpdateLifeRegen()
        {
            UpdateLifeRegenEvent?.Invoke(this);
        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            OnHitByNPCEvent?.Invoke(this, npc, hurtInfo);
        }

        public override void OnEnterWorld()
        {
            OnEnterWorldEvent?.Invoke(this);
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            KillEvent?.Invoke(this, damage, hitDirection, pvp, damageSource);
        }
        #endregion

        // These are small things that don't need their own file.
        #region Misc Overrides
        public override bool ModifyNurseHeal(NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText)
        {
            if (InfernumMode.CanUseCustomAIs && CalamityPlayer.areThereAnyDamnBosses)
            {
                chatText = "I cannot help you. Good luck.";
                return false;
            }
            return true;
        }
        #endregion
    }
}
