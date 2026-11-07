using UnityEngine;
using UnityEngine.InputSystem;
using Whiskers.Core.Events;

namespace Whiskers.Dice.Rolling
{
    // Press and drag on the dice area to throw. Mouse and touch.
    [RequireComponent(typeof(DiceRollDisplay))]
    public sealed class PlayerThrowInput : MonoBehaviour
    {
        [SerializeField] private Camera boardCamera = null;

        [SerializeField] private DiceRollSettings settings = null;

        private DiceRollDisplay display;
        private DragVelocityTracker velocityTracker;
        private bool isAiming;
        private Vector2 pressPoint;
        private Vector2 lastPoint;

        private void Awake()
        {
            display = GetComponent<DiceRollDisplay>();
            velocityTracker = new DragVelocityTracker(settings.DragVelocityWindow);

            if (boardCamera == null)
            {
                boardCamera = Camera.main;
            }
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;

            if (pointer == null)
            {
                return;
            }

            Vector2 screenPosition = pointer.position.ReadValue();

            if (!isAiming)
            {
                if (pointer.press.wasPressedThisFrame && display.TryGetNormalizedPoint(screenPosition, boardCamera, out Vector2 point))
                {
                    TryBeginAim(point);
                }

                return;
            }

            lastPoint = display.GetNormalizedPoint(screenPosition, boardCamera);
            velocityTracker.AddSample(lastPoint, Time.unscaledTime);

            bool released = pointer.press.wasReleasedThisFrame || !pointer.press.isPressed;
            bool draggedFarEnough = (lastPoint - pressPoint).magnitude >= settings.AutoThrowDragDistance;

            if (released || draggedFarEnough)
            {
                EndAim();
            }
        }

        private static bool CanAim()
        {
            DiceRollStage stage = DiceRollStage.Instance;
            return stage != null && stage.Round != null && stage.Round.IsWaitingForPlayer;
        }

        private void TryBeginAim(Vector2 point)
        {
            if (!CanAim())
            {
                return;
            }

            isAiming = true;
            pressPoint = point;
            lastPoint = point;
            velocityTracker.Reset();
            velocityTracker.AddSample(point, Time.unscaledTime);
            EventBus.Publish(new PlayerThrowAimStartedEvent());
        }

        private void EndAim()
        {
            isAiming = false;
            Vector2 drag = lastPoint - pressPoint;

            if (drag.magnitude < settings.MinDragDistance)
            {
                EventBus.Publish(new PlayerThrowAimCanceledEvent());
                return;
            }

            // Whole drag direction if it stopped moving
            Vector2 velocity = velocityTracker.GetVelocity();
            Vector2 direction = velocity.sqrMagnitude > 0f ? velocity.normalized : drag.normalized;
            EventBus.Publish(new PlayerThrowReleasedEvent(direction, velocity.magnitude));
        }
    }
}
