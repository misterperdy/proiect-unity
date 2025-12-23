using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelingSystem : MonoBehaviour
{
    [Header("Stats")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpRequiredForNextLevel;


    public int currentAmountGained = 0;

    [Header("Debug")]
    public int previousLevelCost = 0; // Tracks "Cost(n-1)" for formula

    void Start()
    {
        // Initial setup for Level 1 -> 2
        // Formula: currentLevel(1) * 10 + previous(0) = 10
        CalculateNextLevelCost();
    }

    public void GainXP(int amount)
    {
        currentXP += amount;

        currentAmountGained = amount;

        // Check for Level Up (While loop in case we get huge XP and skip multiple levels)
        while (currentXP >= xpRequiredForNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentXP -= xpRequiredForNextLevel; // Carry over excess XP
        currentLevel++;

        // Save the cost of the level we just finished to use in the formula
        previousLevelCost = xpRequiredForNextLevel;

        CalculateNextLevelCost();

        Debug.Log("LEVEL UP! New Level: " + currentLevel);
        // TODO: Trigger UI update, particle effects, stat increase
    }

    void CalculateNextLevelCost()
    {
        // Formula: currentLevel * 10 + previousLevelCost
        xpRequiredForNextLevel = (currentLevel * 10) + previousLevelCost;
    }
}