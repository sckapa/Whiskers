using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Rolling
{
    // Drag speed over the last moments of the drag
    public sealed class DragVelocityTracker
    {
        private const int MinSamplesForVelocity = 2;

        private readonly float window;
        private readonly List<Sample> samples = new List<Sample>();

        public DragVelocityTracker(float window)
        {
            this.window = window;
        }

        public void AddSample(Vector2 point, float time)
        {
            samples.Add(new Sample(point, time));

            while (samples.Count > MinSamplesForVelocity && samples[1].Time <= time - window)
            {
                samples.RemoveAt(0);
            }
        }

        public Vector2 GetVelocity()
        {
            if (samples.Count < MinSamplesForVelocity)
            {
                return Vector2.zero;
            }

            Sample first = samples[0];
            Sample last = samples[samples.Count - 1];
            float duration = last.Time - first.Time;

            return duration > 0f ? (last.Point - first.Point) / duration : Vector2.zero;
        }

        public void Reset()
        {
            samples.Clear();
        }

        private readonly struct Sample
        {
            public Vector2 Point { get; }
            public float Time { get; }

            public Sample(Vector2 point, float time)
            {
                Point = point;
                Time = time;
            }
        }
    }
}
