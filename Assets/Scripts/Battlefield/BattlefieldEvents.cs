using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core.Events;

namespace Whiskers.Battlefield
{
    // Events the interaction queue and graph listen to, so they only recalculate what actually changed

    public readonly struct BattlefieldObjectRegisteredEvent : IGameEvent
    {
        public BattlefieldObject Object { get; }

        public BattlefieldObjectRegisteredEvent(BattlefieldObject battlefieldObject)
        {
            Object = battlefieldObject;
        }
    }

    public readonly struct BattlefieldObjectMovedEvent : IGameEvent
    {
        public BattlefieldObject Object { get; }
        public Vector2 PreviousPosition { get; }

        public BattlefieldObjectMovedEvent(BattlefieldObject battlefieldObject, Vector2 previousPosition)
        {
            Object = battlefieldObject;
            PreviousPosition = previousPosition;
        }
    }

    public readonly struct BattlefieldObjectValueChangedEvent : IGameEvent
    {
        public BattlefieldObject Object { get; }
        public int PreviousValue { get; }

        public BattlefieldObjectValueChangedEvent(BattlefieldObject battlefieldObject, int previousValue)
        {
            Object = battlefieldObject;
            PreviousValue = previousValue;
        }
    }

    // Active <-> AwaitingInteraction only
    public readonly struct BattlefieldObjectStateChangedEvent : IGameEvent
    {
        public BattlefieldObject Object { get; }
        public BattlefieldObjectState PreviousState { get; }

        public BattlefieldObjectStateChangedEvent(BattlefieldObject battlefieldObject, BattlefieldObjectState previousState)
        {
            Object = battlefieldObject;
            PreviousState = previousState;
        }
    }

    // The object was destroyed or consumed and left active calculations for good
    public readonly struct BattlefieldObjectRemovedEvent : IGameEvent
    {
        public BattlefieldObject Object { get; }

        public BattlefieldObjectRemovedEvent(BattlefieldObject battlefieldObject)
        {
            Object = battlefieldObject;
        }
    }

    public readonly struct BattlefieldObjectsMergedEvent : IGameEvent
    {
        public IReadOnlyList<BattlefieldObject> MergedObjects { get; }
        public BattlefieldObject Result { get; }

        public BattlefieldObjectsMergedEvent(IReadOnlyList<BattlefieldObject> mergedObjects, BattlefieldObject result)
        {
            MergedObjects = mergedObjects;
            Result = result;
        }
    }
}
