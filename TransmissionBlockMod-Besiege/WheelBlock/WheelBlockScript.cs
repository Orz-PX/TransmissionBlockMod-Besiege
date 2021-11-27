using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using Modding.Modules;
using Modding.Modules.Official;
using UnityEngine;

class WheelBlockScript : BlockScript
{

    private MKey forwardKey, backwardKey;
    private MSlider speedSlider, springSlider, damperSlider, acceleratedSlider, staticFrictionSlider, dynamicFrictionSlider, bouncinessSlider, massSlider;
    private MToggle ignoreBaseColliderToggle, toggleToggle;
    private float springMultiplier = 500f;
    private float damperMultiplier = 10f;
    private float maxForceMultiplier = 5000f;
    private float maxAngularVelocityMultiplier = 10f;

    private ConfigurableJoint CJ;
    private Tyre tyre;

    public override void SafeAwake()
    {
        forwardKey = AddKey("Forward", "forward", KeyCode.UpArrow);
        backwardKey = AddKey("Backward", "backward", KeyCode.DownArrow);
        speedSlider = AddSlider("Speed", "speed", 1f, 0.1f, 3f);
        springSlider = AddSlider("Spring", "Spring", 1f, 0.1f, 50f);
        damperSlider = AddSlider("Damper", "Damper", 1f, 0.1f, 50f);
        acceleratedSlider = AddSlider("Accelerated", "accelerated", 10f, 0f, 20f);
        acceleratedSlider.maxInfinity = true;
        staticFrictionSlider = AddSlider("Static Friction", "static friction", 0.5f, 0f, 1f);
        dynamicFrictionSlider = AddSlider("Dynamic Friction", "dynamic friction", 0.8f, 0f, 1f);
        bouncinessSlider = AddSlider("Bounciness", "bounciness", 0f, 0f, 1f);
        massSlider = AddSlider("Mass", "mass", 0.25f, 0.05f, 2f);

        toggleToggle = AddToggle("Toggle", "toggle", false);
        ignoreBaseColliderToggle = AddToggle("Ignore Base" + Environment.NewLine + "Collider", "IBC", false);

        Rigidbody.inertiaTensorRotation = new Quaternion(0, 0, 0.4f, 0.9f);
        Rigidbody.inertiaTensor = new Vector3(0.4f, 0.4f, 0.7f);
        Rigidbody.drag = Rigidbody.angularDrag = 0f;
        Rigidbody.solverVelocityIterations = 1;
        Rigidbody.solverIterations = 100;

        CJ = GetComponent<ConfigurableJoint>();
        CJ.breakForce = CJ.breakTorque = Mathf.Infinity;

        tyre = GetComponent<Tyre>() ?? gameObject.AddComponent<Tyre>();
    }

    public override void OnBlockPlaced()
    {
        tyre.CreateBoxes(18f);
    }

    public override void OnSimulateStart()
    {
        Rigidbody.maxAngularVelocity = speedSlider.Value * maxAngularVelocityMultiplier;

        var mass = massSlider.Value;
        var spring = springSlider.Value * springMultiplier;
        var damper = damperSlider.Value * damperMultiplier;
        var maxForce = springSlider.Value * maxForceMultiplier;
        var bounciness = bouncinessSlider.Value;
        var staticFriction = staticFrictionSlider.Value;
        var dynamicFriction = dynamicFrictionSlider.Value;

        tyre.Setup(spring, damper, maxForce, bounciness, staticFriction, dynamicFriction, mass);
        StartCoroutine(ignoreBaseCollider(ignoreBaseColliderToggle.IsActive));

        addDynamicAxis();

        void addDynamicAxis()
        {
            CJ.axis = Vector3.forward;
            CJ.secondaryAxis = Vector3.up;
            CJ.angularXMotion = ConfigurableJointMotion.Free;

            var jd = CJ.angularXDrive;
            jd.maximumForce = 1000f;
            //jd.positionDamper = 5000f;
            CJ.angularXDrive = jd;

        }

        IEnumerator ignoreBaseCollider(bool active)
        {
            if (active)
            {
                yield return new WaitUntil(() => CJ.connectedBody != null);
                tyre.IgnorBaseBlockCollider();
            }
            yield break;
        }
    }
    float input = 0f, single = 0f, single1 = 0f;
    public override void SimulateFixedUpdateAlways()
    {
        if (CJ == null || CJ?.connectedBody == null) return;

        if (!toggleToggle.IsActive)
        {
            input = 0f;
            if (forwardKey.IsHeld)
            {
                input += 1f;
            }
            if (backwardKey.IsHeld)
            {
                input += -1f;
            }
        }
        else
        {
            if (forwardKey.IsPressed)
            {
                input = input != 1f ? 1f : 0f;
            }
            if (backwardKey.IsPressed)
            {
                input = input != -1f ? -1f : 0f;
            }
        }

        if (input == 0f)
        {
            Rigidbody.WakeUp();
            single = 0f;
            single1 = Mathf.MoveTowards(single1, 750f, 10f * Time.deltaTime);
            var jd = CJ.angularXDrive;
            jd.positionDamper = single1;
            CJ.angularXDrive = jd;
        }
        else
        {
            Rigidbody.WakeUp();
            var jd = CJ.angularXDrive;
            jd.positionDamper = 0f;
            CJ.angularXDrive = jd;
            single1 = 0;
            single = Mathf.MoveTowards(single, 11.5f, input == 0f ? 0f : acceleratedSlider.Value * Time.deltaTime * 10f);
            Rigidbody.AddRelativeTorque(Vector3.forward * (Flipped ? -1f : 1f) * input * speedSlider.Value * single, ForceMode.VelocityChange);
        }
    }
    public override void SimulateLateUpdateAlways()
    {
        base.SimulateLateUpdateAlways();

        tyre.RefreshCenterOfMass(0.95f);
    }
}


