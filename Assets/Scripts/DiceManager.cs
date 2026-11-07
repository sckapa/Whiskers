using System.Collections.Generic;
using UnityEngine;

public class DiceManager : MonoBehaviour
{
    public static DiceManager instance;

    public Dictionary<int, Dice> diceDatabase { get; private set; }

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

        diceDatabase = new();
    }

    public void CreateNormalDice(int ID, DiceSize diceSize)
    {
        NormalDice normalDice = new NormalDice(diceSize);
        diceDatabase.Add(ID, normalDice);
    }
}
