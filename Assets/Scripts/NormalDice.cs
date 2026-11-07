using System.Collections.Generic;
using UnityEngine;

public class NormalDice : Dice
{
    public static NormalDice instance;

    protected override DiceSize diceSize { get; set; }
    protected override List<DiceFace> diceFaces { get; set; }

    public NormalDice(DiceSize size)
    {
        diceSize = size;

        int diceFaceCount = Utils.instance.GetDiceFaceCount(size);

        for (int i = 0; i <= diceFaceCount; i++)
        {
            DiceFace face = new DiceFace(FaceType.Normal, Color.white, i);
            diceFaces.Add(face);
        }
    }

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

        diceFaces = new();
    }
}
