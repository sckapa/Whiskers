namespace Whiskers.Core
{
    // Hands out sequential ids. Sequential (not random) ids keep identical game states deterministic,
    // which tie-breaking in the interaction queue relies on.
    public sealed class UniqueIdGenerator
    {
        private const int FirstId = 1;

        private int nextId = FirstId;

        public int Next()
        {
            return nextId++;
        }

        public void Reset()
        {
            nextId = FirstId;
        }
    }
}
