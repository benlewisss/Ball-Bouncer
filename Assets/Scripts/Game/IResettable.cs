/// <summary>
/// An interface for objects that can be reset to their initial state
/// </summary>
public interface IResettable
{
    /// <summary>
    /// Captures and stores the starting variables and state of the object in case they need to be recalled
    /// </summary>
    void RecordInitialState();

    /// <summary>
    /// Resets the object to a specific state - this may be the same as a hard reset, or it may persist some data 
    /// </summary>
    void SoftReset();

    /// <summary>
    /// Completely resets the object to its initial state
    /// </summary>
    void HardReset();
}