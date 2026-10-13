using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public event Action<int> OnScoreChanged;
    public int CurrentScore { get; private set; }

    public void AddScore(int points)
    {
        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);

        Debug.Log($"ScoreManager: Added {points} points. Current score: {CurrentScore}");
    }
}
