using UnityEngine;

namespace ArmorTheVehicle.Car
{
    /// Spins a set of wheel transforms around their local axle (local X) at a rate derived
    /// from the car's current forward speed and a configured wheel radius. Missing wheels
    /// (null entries, e.g. if the model changes) are skipped rather than throwing.
    internal sealed class CarWheelSpinner
    {
        private static readonly string[] WheelNames = { "WheelFL", "WheelFR", "WheelRL", "WheelRR" };

        private readonly Transform[] _wheels;
        private readonly float _radius;

        private CarWheelSpinner(Transform[] wheels, float radius)
        {
            _wheels = wheels;
            _radius = Mathf.Max(radius, 0.01f); // guards against a zero/negative configured radius
        }

        /// Uses `assignedWheels` directly if the caller already has them (e.g. wired up by
        /// hand in the Inspector) — otherwise falls back to locating the four wheels by name
        /// under the car's model root. The by-name lookup exists because the wheels live
        /// inside a nested FBX-model prefab instance with no stable fileIDs to reference from
        /// a scene/prefab file directly; assigning `assignedWheels` once in the Inspector
        /// (after this ships) skips that lookup entirely.
        public static CarWheelSpinner FromModel(Transform model, Transform[] assignedWheels, float radius)
        {
            if (assignedWheels != null && assignedWheels.Length > 0)
            {
                return new CarWheelSpinner(assignedWheels, radius);
            }

            var wheels = new Transform[WheelNames.Length];
            if (model != null)
            {
                for (int i = 0; i < WheelNames.Length; i++)
                {
                    wheels[i] = model.Find(WheelNames[i]);
                }
            }
            return new CarWheelSpinner(wheels, radius);
        }

        public void Tick(float forwardSpeed, float deltaTime)
        {
            float degreesPerSecond = (forwardSpeed / _radius) * Mathf.Rad2Deg;

            foreach (Transform wheel in _wheels)
            {
                if (wheel == null) continue;
                wheel.Rotate(Vector3.right, degreesPerSecond * deltaTime, Space.Self);
            }
        }
    }
}
