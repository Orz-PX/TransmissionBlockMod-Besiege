using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;

class TransmissionBlockScript : BlockScript
{
    MKey UpKey, DownKey,BackKey;
    MKey ClutchKey;
    MSlider StrengthSlider;
    MSlider RatioSlider;

    GameObject OutAxis;
    ConfigurableJoint CJ,CJ_Axis;
    Rigidbody parentRigidbody,axisRigidbody;

    public float AngularVelocity { get; private set; } = 0f;
    public float ParentAngularVelocity { get; private set; } = 0f;

    public bool Clutch { get { return !ClutchKey.IsDown; } set { Clutch = value; } }
    public float Strength { get; set; } = 1f;
    public float Ratio { get; set; } = 1f;

    public int Input { get; set; } = 1;


    private float deltaAngularVelocity = 0f;

    public override void SafeAwake()
    {
        UpKey = AddKey("加挡", "Up", KeyCode.U);
        DownKey = AddKey("减挡", "Down", KeyCode.J);
        BackKey = AddKey("倒挡", "Back", KeyCode.K);
        ClutchKey = AddKey("离合", "Clutch", KeyCode.C);
        StrengthSlider = AddSlider("马力", "Force", Strength, 0, 10f);
        RatioSlider = AddSlider("比例", "Ratio", Ratio, 0f, 2f);

        StrengthSlider.ValueChanged += (value) => { Strength = value; };
        RatioSlider.ValueChanged += (value) => { Ratio = value; };

        OutAxis = null;
        foreach (var go in GetComponentsInChildren<Transform>())
        {
            if (go.name == "OutAxis") { OutAxis = go.gameObject; }
        }
        
        if (OutAxis == null)
        {
            OutAxis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            OutAxis.name = "OutAxis";
            OutAxis.transform.SetParent(transform);
            OutAxis.transform.position = transform.TransformPoint(transform.InverseTransformPoint(transform.position) + Vector3.forward * 0.75f);
            OutAxis.transform.rotation = transform.rotation;
            OutAxis.transform.localEulerAngles = Vector3.right * 90f;
            OutAxis.transform.localScale = Vector3.one * 0.2f;

            CJ_Axis = OutAxis.AddComponent<ConfigurableJoint>();
            CJ_Axis.axis = Vector3.up;CJ_Axis.secondaryAxis = Vector3.right;
            CJ_Axis.connectedBody = Rigidbody;
            CJ_Axis.angularYMotion = CJ_Axis.angularZMotion = ConfigurableJointMotion.Locked;
            CJ_Axis.xMotion = CJ_Axis.yMotion = CJ_Axis.zMotion = ConfigurableJointMotion.Locked;
            CJ_Axis.rotationDriveMode = RotationDriveMode.XYAndZ;
 
            
            AddPoint(OutAxis, Vector3.up * -1.25f, Vector3.right * 90f, true);         
        }
        else
        {
            CJ_Axis = OutAxis.GetComponent<ConfigurableJoint>();
        }
        axisRigidbody = OutAxis.GetComponent<Rigidbody>();

        CJ = GetComponent<ConfigurableJoint>();
    }

    public override void OnSimulateStart()
    {    
        StartCoroutine(GetParentRigidbody());
    }

    public override void SimulateUpdateAlways()
    {
        if (parentRigidbody != null)
        {
            ParentAngularVelocity = Vector3.Scale(transform.InverseTransformVector(parentRigidbody.angularVelocity), Vector3.forward).z;
            AngularVelocity = Vector3.Scale(transform.InverseTransformVector(Rigidbody.angularVelocity), Vector3.forward).z;

            deltaAngularVelocity = (AngularVelocity - ParentAngularVelocity) * (Flipped ? 1 : -1);

            int sign = 0;
            if (UpKey.IsPressed) sign = 1;
            if (DownKey.IsPressed) sign = -1;
            if (sign != 0) Input = Mathf.Clamp(Input + sign, 1, 4);
            if (BackKey.IsPressed) Input = -1 ;

            float angle = 0f;
            Vector3 axis = Vector3.right;
            CJ_Axis.targetRotation.ToAngleAxis(out angle, out axis);

            float angle1 = 0f;
            Vector3 axis1 = Vector3.right;
            CJ_Axis.transform.rotation.ToAngleAxis(out angle1, out axis1);

            Debug.Log(angle + "   " + (angle1) + "  " + (angle + angle1));

            if (Clutch)
            {
                CJ_Axis.angularXDrive = new JointDrive { maximumForce = Strength * 1000f, positionDamper = 50f, positionSpring = Strength * 50000f };

                float feedSpeed = deltaAngularVelocity * Ratio * Input * 57.5f * 0.4f * Time.deltaTime;       

                if (feedSpeed + angle > 360f)
                {
                    angle -= 360f;
                }
                else if (feedSpeed + angle < 0f)
                {
                    angle += 360f;
                }
                CJ_Axis.targetRotation = Quaternion.AngleAxis(angle + feedSpeed, axis);
            }
            else
            {
                CJ_Axis.angularXDrive = new JointDrive { maximumForce = 0, positionDamper = 0, positionSpring = 0 };
                CJ_Axis.targetRotation = Quaternion.AngleAxis(angle1 /*+ 180f*/, axis);
            }
        }

    }


    private IEnumerator GetParentRigidbody()
    {
        while (true)
        {
            parentRigidbody = CJ.connectedBody;
            if (parentRigidbody != null)
            {
                yield break;
            }
            yield return 0;
        }
    }

    private void AddPoint(GameObject parentObject,Vector3 offset, Vector3 rotation,bool stickiness = false)
    {
        GameObject point = new GameObject("Adding Point");
        point.transform.SetParent(parentObject.transform);
        point.transform.position = parentObject.transform.position;
        point.transform.rotation = parentObject.transform.rotation * Quaternion.Euler(rotation.x, rotation.y, rotation.z);
        point.transform.localPosition = Vector3.zero + offset;
        //point.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
 
        point.layer = stickiness ? 14 : 12;

        BoxCollider boxCollider = point.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, 0, -0.5f);
        boxCollider.size = new Vector3(0.6f, 0.6f, 0);
        boxCollider.isTrigger = true;
    }
}

