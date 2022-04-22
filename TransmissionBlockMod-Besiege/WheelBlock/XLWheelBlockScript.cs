using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using Modding.Modules;
using Modding.Modules.Official;
using UnityEngine;

class XLWheelBlockScript : BlockScript
{

    private MKey forwardKey, backwardKey;
    private MSlider massSlider;
    private MSlider speedSlider, acceleratedSlider;
    private MSlider springSlider, damperSlider;
    private MSlider staticFrictionSlider, dynamicFrictionSlider, bouncinessSlider;
    private MToggle ignoreBaseColliderToggle, toggleToggle, automaticToggle,autoBreakToggle, suspensionToggle;
    //private float springMultiplier = 500f;
    //private float damperMultiplier = 10f;
    //private float maxForceMultiplier = 5000f;
    //private float maxAngularVelocityMultiplier = 10f;

    private ConfigurableJoint CJ;
    private Tyre tyre;
    private WheelMotorControllerHinge wheelMotor;
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

        automaticToggle = AddToggle("automatic", "automatic", false);
        autoBreakToggle = AddToggle("auto break", "auto break", false);
        suspensionToggle = AddToggle("suspension", "suspension", true);

        CJ = GetComponent<ConfigurableJoint>();
        tyre = GetComponent<Tyre>() ?? gameObject.AddComponent<Tyre>();
    }

    public override void OnBlockPlaced()
    {
        var lastscale = transform.localScale;
        BlockBehaviour.SetScale(Vector3.one);
        tyre.CreateBoxes(18f);

        StartCoroutine(wait());

        IEnumerator wait()
        {
            yield return new WaitUntil(() => tyre.Created);
            BlockBehaviour.SetScale(lastscale);
            yield break;
        }
    }

    public override void OnSimulateStart()
    {
        var mass = massSlider.Value;
        var suspension = suspensionToggle.IsActive;
        var spring = springSlider.Value/* * springMultiplier*/;
        var damper = damperSlider.Value/* * damperMultiplier*/;
        var maxForce = springSlider.Value /** maxForceMultiplier*/;
        var bounciness = bouncinessSlider.Value;
        var staticFriction = staticFrictionSlider.Value;
        var dynamicFriction = dynamicFrictionSlider.Value;
        var ignoreBaseCollider = ignoreBaseColliderToggle.IsActive;

        tyre.Setup(suspension, spring, damper, maxForce, bounciness, staticFriction, dynamicFriction, mass, ignoreBaseCollider);

        //addDynamicAxis();
        wheelMotor = gameObject.AddComponent<WheelMotorControllerHinge>();
        wheelMotor.Setup(forwardKey, backwardKey, speedSlider, acceleratedSlider, automaticToggle, toggleToggle, autoBreakToggle, Rigidbody, CJ);

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
    }
    public override void SimulateUpdateAlways()
    {
        base.SimulateUpdateAlways();
        wheelMotor.UpdateBlock();
    }
    //float input = 0f, single = 0f, single1 = 0f;
    public override void SimulateFixedUpdateAlways()
    {
        //if (CJ == null || CJ?.connectedBody == null) return;

        //if (!toggleToggle.IsActive)
        //{
        //    input = 0f;
        //    if (forwardKey.IsHeld)
        //    {
        //        input += 1f;
        //    }
        //    if (backwardKey.IsHeld)
        //    {
        //        input += -1f;
        //    }
        //}
        //else
        //{
        //    if (forwardKey.IsPressed)
        //    {
        //        input = input != 1f ? 1f : 0f;
        //    }
        //    if (backwardKey.IsPressed)
        //    {
        //        input = input != -1f ? -1f : 0f;
        //    }
        //}

        //if (input == 0f)
        //{
        //    Rigidbody.WakeUp();
        //    single = 0f;
        //    single1 = Mathf.MoveTowards(single1, 750f, 10f * Time.deltaTime);
        //    var jd = CJ.angularXDrive;
        //    jd.positionDamper = single1;
        //    CJ.angularXDrive = jd;
        //}
        //else
        //{
        //    Rigidbody.WakeUp();
        //    var jd = CJ.angularXDrive;
        //    jd.positionDamper = 0f;
        //    CJ.angularXDrive = jd;
        //    single1 = 0;
        //    single = Mathf.MoveTowards(single, 11.5f, input == 0f ? 0f : acceleratedSlider.Value * Time.deltaTime * 10f);
        //    Rigidbody.AddRelativeTorque(Vector3.forward * (Flipped ? -1f : 1f) * input * speedSlider.Value * single, ForceMode.VelocityChange);
        //}

        wheelMotor.FixedUpdateBlock(Flipped);
    }
    public override void SimulateLateUpdateAlways()
    {
        base.SimulateLateUpdateAlways();

        tyre.RefreshCenterOfMass(1f);
    }
    public override void KeyEmulationUpdate()
    {
        base.KeyEmulationUpdate();

        wheelMotor.UpdateBlock_Emulation();
    }
}


