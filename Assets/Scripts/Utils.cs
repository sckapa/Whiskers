using UnityEngine;

public class Utils : MonoBehaviour
{
    public static Utils instance;

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
    }


    public int GetDiceFaceCount(DiceSize diceSize)
    {
        switch (diceSize)
        {
            case DiceSize.D4:
                return 4;
            case DiceSize.D6:
                return 6;
            case DiceSize.D8:
                return 8;
            case DiceSize.D9:
                return 9;
            case DiceSize.D10:
                return 10;
            case DiceSize.D12:
                return 12;
            case DiceSize.D20:
                return 20;
            default:
                Debug.LogError("Dice Size not found");
                return 0;
        }
    }
}
