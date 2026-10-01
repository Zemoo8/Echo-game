namespace Echo.Player
{
    /// <summary>
    /// Anything a dash can hit: breakable barriers, the boss core, dash switches.
    /// PlayerDash calls this just before the player reaches the target's collider.
    /// </summary>
    public interface IDashTarget
    {
        void OnDashHit(PlayerDash dash);
    }
}
