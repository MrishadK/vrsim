using UnityEngine;
using UnityEngine.InputSystem;

namespace A350NeoConcept
{
    /// <summary>Simple showcase controls for the A350-900 neo concept model.</summary>
    public sealed class A350NeoConceptControls : MonoBehaviour
    {
        [Header("View")]
        public GameObject fuselageSkin;
        public GameObject cabinLining;
        [Header("Moving parts")]
        public GameObject landingGear;
        public Transform[] passengerDoors;
        public Transform[] flapHinges;
        public Transform[] aileronHinges;
        public Transform[] spoilerHinges;
        public Transform[] elevatorHinges;
        public Transform rudderHinge;

        private bool cutaway;
        private bool doorsOpen;
        private bool gearDown = true;
        private bool flapsDown;
        private bool spoilersUp;
        private Quaternion[] doorRest;
        private Quaternion[] flapRest;
        private Quaternion[] aileronRest;
        private Quaternion[] spoilerRest;
        private Quaternion[] elevatorRest;
        private Quaternion rudderRest;
        private float pitchInput;
        private float rollInput;
        private float yawInput;

        private void Awake()
        {
            doorRest = Capture(passengerDoors);
            flapRest = Capture(flapHinges);
            aileronRest = Capture(aileronHinges);
            spoilerRest = Capture(spoilerHinges);
            elevatorRest = Capture(elevatorHinges);
            if (rudderHinge != null) rudderRest = rudderHinge.localRotation;
                if (fuselageSkin != null) fuselageSkin.SetActive(true);
                if (cabinLining != null) cabinLining.SetActive(true);
            if (landingGear != null) landingGear.SetActive(true);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.cKey.wasPressedThisFrame)
            {
                cutaway = !cutaway;
                if (fuselageSkin != null) fuselageSkin.SetActive(!cutaway);
                if (cabinLining != null) cabinLining.SetActive(!cutaway);
            }
            if (keyboard.dKey.wasPressedThisFrame) doorsOpen = !doorsOpen;
            if (keyboard.gKey.wasPressedThisFrame)
            {
                gearDown = !gearDown;
                if (landingGear != null) landingGear.SetActive(gearDown);
            }
            if (keyboard.fKey.wasPressedThisFrame) flapsDown = !flapsDown;
            if (keyboard.sKey.wasPressedThisFrame) spoilersUp = !spoilersUp;

            pitchInput = Axis(keyboard.upArrowKey.isPressed, keyboard.downArrowKey.isPressed);
            rollInput = Axis(keyboard.leftArrowKey.isPressed, keyboard.rightArrowKey.isPressed);
            yawInput = Axis(keyboard.qKey.isPressed, keyboard.eKey.isPressed);

            AnimateDoors();
            AnimateFlaps();
            AnimateAilerons();
            AnimateSpoilers();
            AnimateTail();
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private void AnimateDoors()
        {
            if (passengerDoors == null) return;
            for (int i = 0; i < passengerDoors.Length; i++)
            {
                var hinge = passengerDoors[i];
                if (hinge == null) continue;
                float outward = hinge.localPosition.x < 0f ? 1f : -1f;
                Quaternion target = doorRest[i] * Quaternion.Euler(0f, doorsOpen ? outward * 92f : 0f, 0f);
                hinge.localRotation = Quaternion.Slerp(hinge.localRotation, target, Time.deltaTime * 3.5f);
            }
        }

        private void AnimateFlaps()
        {
            if (flapHinges == null) return;
            for (int i = 0; i < flapHinges.Length; i++)
            {
                var hinge = flapHinges[i];
                if (hinge == null) continue;
                Quaternion target = flapRest[i] * Quaternion.Euler(flapsDown ? -22f : 0f, 0f, 0f);
                hinge.localRotation = Quaternion.Slerp(hinge.localRotation, target, Time.deltaTime * 2.5f);
            }
        }

        private void AnimateAilerons()
        {
            if (aileronHinges == null) return;
            for (int i = 0; i < aileronHinges.Length; i++)
            {
                var hinge = aileronHinges[i];
                if (hinge == null) continue;
                float side = Mathf.Sign(hinge.localPosition.x);
                Quaternion target = aileronRest[i] * Quaternion.Euler(-rollInput * side * 17f, 0f, 0f);
                hinge.localRotation = Quaternion.Slerp(hinge.localRotation, target, Time.deltaTime * 4f);
            }
        }

        private void AnimateSpoilers()
        {
            if (spoilerHinges == null) return;
            for (int i = 0; i < spoilerHinges.Length; i++)
            {
                var hinge = spoilerHinges[i];
                if (hinge == null) continue;
                Quaternion target = spoilerRest[i] * Quaternion.Euler(spoilersUp ? 42f : 0f, 0f, 0f);
                hinge.localRotation = Quaternion.Slerp(hinge.localRotation, target, Time.deltaTime * 4f);
            }
        }

        private void AnimateTail()
        {
            if (elevatorHinges != null)
            {
                for (int i = 0; i < elevatorHinges.Length; i++)
                {
                    var hinge = elevatorHinges[i];
                    if (hinge == null) continue;
                    Quaternion target = elevatorRest[i] * Quaternion.Euler(pitchInput * 16f, 0f, 0f);
                    hinge.localRotation = Quaternion.Slerp(hinge.localRotation, target, Time.deltaTime * 4f);
                }
            }
            if (rudderHinge != null)
            {
                Quaternion target = rudderRest * Quaternion.Euler(0f, yawInput * 20f, 0f);
                rudderHinge.localRotation = Quaternion.Slerp(rudderHinge.localRotation, target, Time.deltaTime * 4f);
            }
        }

        private static Quaternion[] Capture(Transform[] parts)
        {
            if (parts == null) return new Quaternion[0];
            var values = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                values[i] = parts[i] != null ? parts[i].localRotation : Quaternion.identity;
            return values;
        }
    }
}
