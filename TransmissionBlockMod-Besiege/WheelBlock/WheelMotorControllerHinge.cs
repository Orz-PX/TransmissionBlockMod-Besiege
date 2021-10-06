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
    private bool forwardPressed;
    private bool backwardPressed;
    private bool forwardHeld;
    private bool backwardHeld;
    private bool emuForwardPressed;
    private bool emuBackwardPressed;
    private bool emuForwardHeld;
    private bool emuBackwardHeld;

    public MSlider AccelerationSlider { get { return this.accSlider; } }
    public MToggle AutoBreakToggle { get { return this.autoBreakMode; } }
    public MToggle AutomaticToggle { get { return this.automaticToggle; } }
    public MKey BackwardKey { get { return this.backwardKey; } }
    public MKey ForwardKey { get { return this.forwardKey; } }
    public float Input { get { return this.input; } set { this.input = value; } }
    public MSlider SpeedSlider { get { return this.speedSlider; } }
    public MToggle ToggleModeToggle { get { return this.toggleMode; } }
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
        this.noRigidbody = this.Rigidbody != null;

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
        if (this.myJoint == null || !this.noRigidbody && this.Rigidbody.isKinematic && this.myJoint.connectedBody && this.myJoint.connectedBody.isKinematic)
        {
            this.input = 0f;
            return;
        }
        if (this.automaticToggle.IsActive)
        {
            this.input = 1f;
        }
        else if (this.toggleMode.IsActive)
        {
            if (forwardPress)
            {
                if (this.input <= 0.9f)
                {
                    this.input = 1f;
                }
                else
                {
                    this.input = 0f;
                }
            }
            if (backwardPress)
            {
                if (this.input >= -0.9f)
                {
                    this.input = -1f;
                }
                else
                {
                    this.input = 0f;
                }
            }
        }
        else if (forwardHeld)
        {
            this.input = forwardVal;
        }
        else if (!altForwardHeld)
        {
            if (backwardHeld)
            {
                this.input = backwardVal;
            }
            else if (!altBackwardHeld)
            {
                this.input = 0f;
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
        if (this.allowControl)
        {
            this.forwardPressed = this.forwardKey.IsPressed;
            this.backwardPressed = this.backwardKey.IsPressed;
            this.forwardHeld = this.forwardKey.IsHeld;
            this.backwardHeld = this.backwardKey.IsHeld;
            this.CheckKeys(this.forwardPressed, this.backwardPressed, this.forwardHeld, this.backwardHeld, this.forwardKey.Value, -this.backwardKey.Value, this.emuForwardHeld, this.emuBackwardHeld);
        }
        else if (this.myJoint == null || !this.noRigidbody && this.Rigidbody.isKinematic && this.myJoint.connectedBody && this.myJoint.connectedBody.isKinematic)
        {
            this.input = 0f;
        }
        else
        {
            this.input = 1f;
        }
    }
    public void EmulationUpdateBlock()
    {
        if (this.allowControl)
        {
            this.emuForwardPressed = this.forwardKey.EmulationPressed();
            this.emuBackwardPressed = this.backwardKey.EmulationPressed();
            this.emuForwardHeld = this.forwardKey.EmulationHeld(true);
            this.emuBackwardHeld = this.backwardKey.EmulationHeld(true);
            this.CheckKeys(this.emuForwardPressed, this.emuBackwardPressed, this.emuForwardHeld, this.emuBackwardHeld, 1f, -1f, this.forwardHeld, this.backwardHeld);
        }
    }
    float single = 0f, single1 = 0f;
    public void FixedUpdateBlock(bool flipped)
    {
        if (this.myJoint == null || this.myJoint.connectedBody == null)
        {
            return;
        }
        this.Velocity = this.input * this.speedSlider.Value * (flipped ? -1f : 1f);

        if (input == 0f)
        {
            single = 0f;
            if (autoBreakMode.IsActive)
            {
                single1 = Mathf.MoveTowards(single1, 1750f, 10f * Time.deltaTime);
                motor.positionDamper = single1;
                myJoint.angularXDrive = motor;
            }
        }
        else
        {
            single1 = 0f;
            Rigidbody.WakeUp();
            motor.positionDamper = 0f;
            myJoint.angularXDrive = motor;
            single = Mathf.MoveTowards(single, 17.5f, input == 0f ? 0f : accSlider.Value * Time.deltaTime * 10f);
            Rigidbody.AddRelativeTorque(Vector3.forward * Velocity * single, ForceMode.VelocityChange);
        }
    }

    void OnDestroy()
    {
        this.noRigidbody = true;
    }
}

