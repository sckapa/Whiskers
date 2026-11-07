using System.Collections.Generic;
using UnityEngine;

// This is only a data container. Describing what to do with the data is the child class' responsibility
public abstract class Dice : MonoBehaviour
{
    protected abstract DiceSize diceSize { get; set; }
    protected abstract List<DiceFace> diceFaces { get; set; }
}

public enum DiceSize
{
    D4,
    D6,
    D8,
    D9,
    D10,
    D12,
    D20
}

public class DiceFace
{
    public FaceType faceType { get; private set; }
    public int value { get; private set; }
    public SpecialFaceEffects specialFaceEffect { get; private set; }
    public Color faceColor { get; private set; }

    public DiceFace(FaceType type, Color color, int val = 0)
    {
        faceType = type;
        value = val;
        faceColor = color;
    }

    public DiceFace(FaceType type, Color color, SpecialFaceEffects faceEffect = SpecialFaceEffects.None)
    {
        faceType = type;
        specialFaceEffect = faceEffect;
        faceColor = color;
    }
}

public enum FaceType
{
    Normal,
    Special
}

public enum SpecialFaceEffects
{
    None,
    AddHealth,
    DoubleDamage,
    SkipEnemyTurn
}