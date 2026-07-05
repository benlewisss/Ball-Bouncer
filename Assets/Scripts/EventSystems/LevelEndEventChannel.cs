using System;
using UnityEngine;

/// <summary>
/// Event channel for a level end condition.
/// </summary>
[CreateAssetMenu(fileName = "LevelEndEventChannel", menuName = "Events/LevelEndEventChannel")]
public class LevelEndEventChannel : ScriptableObject
{
    /// <summary>
    /// Event triggered when the level ends
    /// Outcome: true = Win, false = Lose.
    /// </summary>
    public event Action<bool> OnLevelEnded;

    /// <summary>
    /// Broadcasts the level end event
    /// </summary>
    /// <param name="isWin">True if the player won the level, false if the player lost.</param>
    public void RaiseEvent(bool isWin)
    {
        OnLevelEnded?.Invoke(isWin);
    }
}