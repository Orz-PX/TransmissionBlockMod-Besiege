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
    private float deltaMultiplier;
    private bool hasStarted = false;
    private bool forceReset;

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
        this.forwardKey = forwardKey;
        this.backwardKey = backwardKey;
        this.speedSlider = speedSlider;
        this.accSlider = accSlider;
        this.automaticToggle = automaticToggle;
        this.toggleMode = toggleMode;
        this.autoBreakMode = autoBreakMode;
        this.rigidbody = rigidbody;
        noRigidbody = (Rigidbody != null);

        //Rigidbody.inertiaTensorRotation = new Quaternion(0, 0, 0.4f, 0.9f);
        //Rigidbody.inertiaTensor = new Vector3(0.4f, 0.4f, 0.7f);
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

        setFalseOnStart();

        void setFalseOnStart()
        {
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
    public void UpdateBlock_Emulation()
    {
        if (allowControl)
        {
            CheckKeys(forwardKey.EmulationPressed(), backwardKey.EmulationPressed(), forwardKey.EmulationHeld(true), backwardKey.EmulationHeld(true), 1f, -1f, forwardKey.IsHeld, backwardKey.IsHeld);
        }
    }
    //float single = 0f, single1 = 0f;
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


        if (!hasStarted)
        {
            if (!noRigidbody)
            {
                Rigidbody.maxAngularVelocity = maxAngularVel;
            }
            deltaMultiplier = degreesPerSecond * 80f * -(float)((!flipped) ? -1f : 1f);
            hasStarted = true;
        }
        if (myJoint == null || myJoint.connectedBody == null)
        {
            return;
        }
        Velocity = input * speedSlider.Value;
        if (input == 0f)
        {
            motor.maximumForce = float.PositiveInfinity;
            forceReset = true;
        }
        else
        {
            if (motor.maximumForce == float.PositiveInfinity && forceReset)
            {
                motor.maximumForce = accSlider.Value;
                forceReset = false;
            }
            if (motor.maximumForce != float.PositiveInfinity && !forceReset)
            {
                motor.maximumForce += accSlider.Value * Time.deltaTime * 10f;
                if (motor.maximumForce > accInfinity)
                {
                    motor.maximumForce = float.PositiveInfinity;
                }
                myJoint.angularXDrive = motor;
            }
        }
        //if (!isUsingMotor)
        //{
        //    myJoint.angularXMotion = ConfigurableJointMotion.Free;
        //    isUsingMotor = true;
        //}
        float num = Velocity * deltaMultiplier;
        float num2 = lastVelocity + (num - lastVelocity) * Time.deltaTime * speedLerpSmooth;
    
        if (allowControl && !autoBreakMode.IsActive && ((input >= 0f) ? input : (-input)) < 0.05f)
        {
            float num3 = 0.01f;
            if (lastVelocity > 0f)
            {
                num2 = ((num2 <= num3) ? num2 : num3);
            }
            else if (lastVelocity < 0f)
            {
                num2 = ((num2 <= -num3) ? (-num3) : num2);
            }
        }
        float num4 = num2 - lastVelocity;
        if (((num4 >= 0f) ? num4 : (-num4)) > Mathf.Epsilon)
        {
            if (!noRigidbody && Rigidbody.IsSleeping())
            {
                Rigidbody.WakeUp();
            }
            if (myJoint.connectedBody.IsSleeping())
            {
                myJoint.connectedBody.WakeUp();
            }
            //motor.targetVelocity = num2;
            //Rigidbody.AddRelativeTorque(Vector3.forward * num2, ForceMode.VelocityChange);
            lastVelocity = num2;
            myJoint.angularXDrive = motor;
        }
        Rigidbody.AddRelativeTorque(Vector3.forward * num2 * 50f, ForceMode.VelocityChange);
    }

    private void OnDestroy()
    {
        noRigidbody = true;
    }
}

