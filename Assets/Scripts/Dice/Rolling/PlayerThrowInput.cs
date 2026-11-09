using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Whiskers.Core;
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

            // Paused: an aim in progress is kept and continues on resume, new aims can't start
            if (GamePause.IsPaused)
            {
                return;
            }

            if (!isAiming)
            {
                if (pointer.press.wasPressedThisFrame && !IsPointerOverUI() && display.TryGetNormalizedPoint(screenPosition, boardCamera, out Vector2 point))
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

        // Clicks on buttons (pause etc.) must not start a throw
        private static bool IsPointerOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
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
