namespace Whiskers.Dice.Rolling
{
    public sealed class DieSettleTracker
    {
        private readonly float maxLinearSpeed;
        private readonly float maxAngularSpeed;
        private readonly float requiredStillTime;
        private float stillTime;

        public DieSettleTracker(float maxLinearSpeed, float maxAngularSpeed, float requiredStillTime)
        {
            this.maxLinearSpeed = maxLinearSpeed;
            this.maxAngularSpeed = maxAngularSpeed;
            this.requiredStillTime = requiredStillTime;
        }

        public bool Update(float linearSpeed, float angularSpeed, float deltaTime)
        {
            bool isStill = linearSpeed <= maxLinearSpeed && angularSpeed <= maxAngularSpeed;
            stillTime = isStill ? stillTime + deltaTime : 0f;
            return stillTime >= requiredStillTime;
        }

        public void Reset()
        {
            stillTime = 0f;
        }
    }
}
