using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class WheelMotorControllerHinge : MonoBehaviour
{
    public bool noRigidbody;
    public float Velocity;
    public bool allowControl = true;
    public float degreesPerSecond = 1f;
    public float maxAngularVel = 50f;
    public Transform rotationArrow;
    public float minAcc = 0.1f;
    public float maxAcc = 20f;
    public float accInfinity = 600f;
    public float defaultSpeed = 1f;
    public float minSpeed;
    public float maxSpeed = 2f;
    public float speedLerpSmooth = 26f;
    public JointDrive motor;
    private ConfigurableJoint myJoint;
    private float input;
    private MToggle automaticToggle;
    private MToggle toggleMode;
    private MToggle autoBreakMode;
    private MKey forwardKey;
    private MKey backwardKey;
    public MSlider speedSlider;
    private MSlider accSlider;
    private Rigidbody rigidbody;
    private float lastVelocity;

    public MSlider AccelerationSlider { get { return accSlider; } }
    public MToggle AutoBreakToggle { get { return autoBreakMode; } }
    public MToggle AutomaticToggle { get { return automaticToggle; } }
    public MKey BackwardKey { get { return backwardKey; } }
    public MKey ForwardKey { get { return forwardKey; } }
    public float Input { get { return input; } set { input = value; } }
    public MSlider SpeedSlider { get { return speedSlider; } }
    public MToggle ToggleModeToggle { get { return toggleMode; } }
    public Rigidbody Rigidbody { get { return rigidbody; } }

    public void Setup(MKey forwardKey, MKey backwardKey, MSlider speedSlider, MSlider accSlider, MToggle automaticToggle, MToggle toggleMode, MToggle autoBreakMode, Rigidbody rigidbody, ConfigurableJoint configurableJoint)
    {
        //forwardKey = forwardKey;
        //backwardKey = backwardKey;
        //speedSlider = speedSlider;
        //accSlider = accSlider;
        //automaticToggle = automaticToggle;
        //toggleMode = toggleMode;
        //autoBreakMode = autoBreakMode;
        //rigidbody = rigidbody;
        noRigidbody = Rigidbody != null;

        Rigidbody.inertiaTensorRotation = new Quaternion(0, 0, 0.4f, 0.9f);
        Rigidbody.inertiaTensor = new Vector3(0.4f, 0.4f, 0.7f);
        Rigidbody.drag = Rigidbody.angularDrag = 0f;
        Rigidbody.solverVelocityIterations = 10;
        Rigidbody.solverIterations = 100;

        myJoint = configurableJoint;
        myJoint.breakForce = myJoint.breakTorque = Mathf.Infinity;
        myJoint.axis = Vector3.forward;
        myJoint.secondaryAxis = Vector3.up;
        myJoint.angularXMotion = ConfigurableJointMotion.Free;
        myJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
        motor = myJoint.angularXDrive;
        //motor.maximumForce = 1000f;
        //myJoint.angularXDrive = motor;
    }
    protected void CheckKeys(bool forwardPress, bool backwardPress, bool forwardHeld, bool backwardHeld, float forwardVal, float backwardVal, bool altForwardHeld, bool altBackwardHeld)
    {
        if (myJoint == null || !noRigidbody && Rigidbody.isKinematic && myJoint.connectedBody && myJoint.connectedBody.isKinematic)
        {
            input = 0f;
            return;
        }
        if (automaticToggle.IsActive)
        {
            input = 1f;
        }
        else if (toggleMode.IsActive)
        {
            if (forwardPress)
            {
                if (input <= 0.9f)
                {
                    input = 1f;
                }
                else
                {
                    input = 0f;
                }
            }
            if (backwardPress)
            {
                if (input >= -0.9f)
                {
                    input = -1f;
                }
                else
                {
                    input = 0f;
                }
            }
        }
        else if (forwardHeld)
        {
            input = forwardVal;
        }
        else if (!altForwardHeld)
        {
            if (backwardHeld)
            {
                input = backwardVal;
            }
            else if (!altBackwardHeld)
            {
                input = 0f;
            }
        }
    }
    public void setFalseOnStart()
    {
        Rigidbody.maxAngularVelocity = maxAngularVel * SpeedSlider.Value;

        StartCoroutine(wait());
        IEnumerator wait()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return 0;
            }
            myJoint.swapBodies = false;
        }
    }
    public void UpdateBlock()
    {
        if (allowControl)
        {
            CheckKeys(forwardKey.IsPressed, backwardKey.IsPressed, forwardKey.IsHeld, backwardKey.IsHeld, forwardKey.Value, -backwardKey.Value, forwardKey.EmulationHeld(true), backwardKey.EmulationHeld(true));
        }
        else if (myJoint == null || !noRigidbody && Rigidbody.isKinematic && myJoint.connectedBody && myJoint.connectedBody.isKinematic)
        {
            input = 0f;
        }
        else
        {
            input = 1f;
        }
    }
    public void EmulationUpdateBlock()
    {
        if (allowControl)
        {
            CheckKeys(forwardKey.EmulationPressed(), backwardKey.EmulationPressed(), forwardKey.EmulationHeld(true), backwardKey.EmulationHeld(true), 1f, -1f, forwardKey.IsHeld, backwardKey.IsHeld);
        }
    }
    float single = 0f, single1 = 0f;
    public void FixedUpdateBlock(bool flipped)
    {
        //if (myJoint == null || myJoint.connectedBody == null)
        //{
        //    return;
        //}
        //Velocity = input * speedSlider.Value * (flipped ? -1f : 1f);

        //if (input == 0f)
        //{
        //    single = 0f;
        //    if (autoBreakMode.IsActive)
        //    {
        //        single1 = Mathf.MoveTowards(single1, 1750f, 10f * Time.deltaTime);
        //        motor.positionDamper = single1;
        //        myJoint.angularXDrive = motor;
        //    }
        //}
        //else
        //{
        //    single1 = 0f;
        //    Rigidbody.WakeUp();
        //    motor.positionDamper = 0f;
        //    myJoint.angularXDrive = motor;
        //    single = Mathf.MoveTowards(single, 17.5f, input == 0f ? 0f : accSlider.Value * Time.deltaTime * 10f);
        //    Rigidbody.AddRelativeTorque(Vector3.forward * Velocity * single, ForceMode.VelocityChange);
        //}


        if (!this.hasStarted)
        {
            if (!this.noRigidbody)
            {
                this.Rigidbody.maxAngularVelocity = this.maxAngularVel;
            }
            this.deltaMultiplier = this.degreesPerSecond * 80f * (float)(-(float)this.FlipInvert);
            this.hasStarted = true;
        }
        if (this.myJoint == null || this.myJoint.connectedBody == null)
        {
            return;
        }
        this.Velocity = this.input * this.speedSlider.Value;
        if (this.input == 0f)
        {
            this.motor.force = float.PositiveInfinity;
            this.forceReset = true;
        }
        else
        {
            if (this.motor.force == float.PositiveInfinity && this.forceReset)
            {
                this.motor.force = this.accSlider.Value;
                this.forceReset = false;
            }
            if (this.motor.force != float.PositiveInfinity && !this.forceReset)
            {
                this.motor.force = this.motor.force + this.accSlider.Value * Time.deltaTime * 10f;
                if (this.motor.force > this.accInfinity)
                {
                    this.motor.force = float.PositiveInfinity;
                }
                this.myJoint.motor = this.motor;
            }
        }
        if (!this.isUsingMotor)
        {
            this.myJoint.useMotor = true;
            this.isUsingMotor = true;
        }
        float num = this.Velocity * this.deltaMultiplier;
        float num2 = this.lastVelocity + (num - this.lastVelocity) * Time.deltaTime * this.speedLerpSmooth;
        if (this.allowControl && !this.autoBreakMode.IsActive && ((this.input >= 0f) ? this.input : (-this.input)) < 0.05f)
        {
            float num3 = 0.01f;
            if (this.lastVelocity > 0f)
            {
                num2 = ((num2 <= num3) ? num2 : num3);
            }
            else if (this.lastVelocity < 0f)
            {
                num2 = ((num2 <= -num3) ? (-num3) : num2);
            }
        }
        float num4 = num2 - this.lastVelocity;
        if (((num4 >= 0f) ? num4 : (-num4)) > Mathf.Epsilon)
        {
            if (!this.noRigidbody && this.Rigidbody.IsSleeping())
            {
                this.Rigidbody.WakeUp();
            }
            if (this.myJoint.connectedBody.IsSleeping())
            {
                this.myJoint.connectedBody.WakeUp();
            }
            this.motor.targetVelocity = num2;
            this.lastVelocity = num2;
            this.myJoint.motor = this.motor;
        }
    }

    private void OnDestroy()
    {
        noRigidbody = true;
    }
}

