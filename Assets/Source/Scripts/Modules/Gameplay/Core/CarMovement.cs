using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Core
{
    public class CarMovement
    {
        private readonly Rigidbody rb;
        private readonly float maxSpeed;
        private readonly float accelForce;
        private readonly float turnTorque;
        private readonly AnimationCurve steerCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);

        private const float LateralDamping = 4f;
        private const float AngularDamping = 4f;

        public CarMovement(Rigidbody rb, float maxSpeed, float accelForce, float turnTorque)
        {
            this.rb = rb;
            this.maxSpeed = maxSpeed;
            this.accelForce = accelForce;
            this.turnTorque = turnTorque;

            this.rb.interpolation = RigidbodyInterpolation.Interpolate;
            this.rb.angularDrag = AngularDamping;
        }

        public void Move(float throttle, float steer)
        {
            HandleThrottle(throttle);
            HandleSteering(steer);
            ApplyLateralFriction();
        }

        private void HandleThrottle(float throttle)
        {
            if (Mathf.Abs(throttle) <= 0.01f)
                return;

            var velocity = rb.velocity;
            var forwardDot = Vector3.Dot(velocity, rb.transform.forward);
            var isOpposite = !Mathf.Approximately(Mathf.Sign(throttle), Mathf.Sign(forwardDot));
            var underMaxSpeed = velocity.magnitude < maxSpeed;

            if (underMaxSpeed || isOpposite)
            {
                var force = rb.transform.forward * throttle * accelForce;
                rb.AddForce(force, ForceMode.Acceleration);
            }
        }

        private void HandleSteering(float steer)
        {
            if (Mathf.Abs(steer) <= 0.01f)
                return;

            var speedFactor = Mathf.InverseLerp(0f, maxSpeed, rb.velocity.magnitude);
            var steerFactor = steerCurve.Evaluate(speedFactor);
            var torque = Vector3.up * steer * turnTorque * steerFactor;

            rb.AddTorque(torque, ForceMode.Acceleration);
        }

        private void ApplyLateralFriction()
        {
            var lateralVelocity = Vector3.Project(rb.velocity, rb.transform.right);
            rb.AddForce(-lateralVelocity * LateralDamping, ForceMode.Acceleration);
        }
    }
}