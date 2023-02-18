namespace InfernumMode.Core.TrackedMusic
{
    // Sometimes a song might have too much energy to it, and using a full beat for the head-bobbing effect might result in the player character looking like they're
    // having a seizure. As such, a song may choose which form it wants to have that effect in.
    public enum BPMHeadBobState
    {
        Full,
        Half,
        Quarter
    }
}
