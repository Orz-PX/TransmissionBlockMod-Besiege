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

    HingeJoint HJ;
    GameObject OutAxis;
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
            OutAxis.transform.position = transform.TransformPoint(transform.InverseTransformPoint(transform.position) + Vector3.forward);
            OutAxis.transform.rotation = transform.rotation;
            OutAxis.transform.localEulerAngles = Vector3.right * 90f;
            OutAxis.transform.localScale = Vector3.one * 0.2f;

            HJ = OutAxis.AddComponent<HingeJoint>();
            HJ.axis = OutAxis.transform.forward;
            HJ.connectedBody = Rigidbody;     
            HJ.useMotor = false;

            AddPoint(OutAxis);         
        }
        else
        {
            HJ = OutAxis.GetComponent<HingeJoint>();
        }
        axisRigidbody = OutAxis.GetComponent<Rigidbody>();
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

            deltaAngularVelocity = AngularVelocity - ParentAngularVelocity;

            int sign = 0;
            if (UpKey.IsPressed) sign = 1;
            if (DownKey.IsPressed) sign = -1;
            if (sign != 0) Input = Mathf.Clamp(Input + sign, 1, 4);      
            if (BackKey.IsPressed) Input = -1;

            if (Clutch)
            {
                if (Mathf.Abs(deltaAngularVelocity) < 0.1f)
                {
                    if (!HJ.useLimits)
                    {
                        float angle = HJ.angle;

                        HJ.limits = new JointLimits { min = angle - 0.5f, max = angle + 0.5f, contactDistance = 0.5f, bounciness = 0f, bounceMinVelocity = 0f };
                        HJ.useLimits = true;
                    }
                }
                else
                {
                    HJ.useLimits = false;

                    HJ.motor = new JointMotor { force = Strength * 10000f, freeSpin = false, targetVelocity = (deltaAngularVelocity * 57.5f) * Ratio * Input };
                    HJ.useMotor = true;
                }
            }
            else
            {
                HJ.useMotor = false;
                HJ.useLimits = false;
            }           
        }


        GetComponent<ConfigurableJoint>().targetRotation = Quaternion.Euler(GetComponent<ConfigurableJoint>().axis * 10f);
    }


    private IEnumerator GetParentRigidbody()
    {
        while (true)
        {
            parentRigidbody = GetComponent<ConfigurableJoint>().connectedBody;
            if (parentRigidbody != null)
            {
                yield break;
            }
            yield return 0;
        }
    }

    private void AddPoint(GameObject parentObject)
    {
        GameObject point = new GameObject("Adding Point")/*GameObject.CreatePrimitive(PrimitiveType.Cube)*/;
        point.transform.SetParent(parentObject.transform);
        point.transform.position = parentObject.transform.position;
        //point.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        point.layer = 14;

        BoxCollider boxCollider = point.AddComponent<BoxCollider>()/*point.GetComponent<BoxCollider>()*/ ;
        boxCollider.center = new Vector3(0, 0, -0.5f);
        boxCollider.size = new Vector3(0.6f, 0.6f, 0);
        boxCollider.isTrigger = true;

        //point.AddComponent<MeshFilter>();
        //point.AddComponent<MeshRenderer>().material.color = Color.red;
        //point.GetComponent<MeshRenderer>().material.color = Color.red;
    }
}

