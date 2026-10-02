namespace Echo.Gameplay
{
    /// <summary>
    /// A level object the run can use up (a cell taken, a barrier broken, a machine destroyed, a
    /// console used) and that a checkpoint or a restart puts back. LevelController finds every
    /// one in the scene and remembers which were spent when a checkpoint was reached.
    /// </summary>
    public interface IRestorable
    {
        bool IsSpent { get; }

        /// <summary>Back to the untouched state. Does nothing if not spent.</summary>
        void Restore();
    }
}
