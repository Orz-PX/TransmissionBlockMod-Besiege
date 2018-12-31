using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;

class TransmissionBlockScript : BlockScript
{
    MKey mKey;

    ConfigurableJoint CJ,outAxisCJ;

    HingeJoint HJ;

    GameObject addingPoint,axis;

    Rigidbody parentRigidbody;

    public float AngularVelocity { get; private set; } = 0f;
    public float ParentAngularVelocity { get; private set; } = 0f;
    public float strength { get; set; } = 1000f;


    private float deltaAngularVelocity = 0f;

    public override void SafeAwake()
    {
        mKey = AddKey("test", "test", KeyCode.B);
       

        //addingPoint = GetComponentsInChildren<Transform>()[0].gameObject;
        //addingPoint.GetComponent<BoxCollider>().enabled = false;
        CJ = GetComponent<ConfigurableJoint>();

        axis = null;
        foreach (var go in GetComponentsInChildren<Transform>())
        {
            if (go.name == "OutAxis")
            {
                axis = go.gameObject;
            }
        }

        if (axis == null)
        {
            axis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            axis.name = "OutAxis";
            axis.transform.SetParent(transform);
            axis.transform.position = transform.TransformPoint(transform.InverseTransformPoint(transform.position) + Vector3.forward);
            axis.transform.rotation = transform.rotation;
            axis.transform.localEulerAngles = Vector3.right * 90f;
            axis.transform.localScale = Vector3.one * 0.2f;

            outAxisCJ = axis.AddComponent<ConfigurableJoint>();
            outAxisCJ.axis = Vector3.up;
            outAxisCJ.connectedBody = Rigidbody;
            outAxisCJ.xMotion = outAxisCJ.yMotion = outAxisCJ.zMotion = ConfigurableJointMotion.Locked;
            //outAxisCJ.angularXMotion = outAxisCJ.angularYMotion = outAxisCJ.angularZMotion = ConfigurableJointMotion.Locked;
            ////outAxisCJ.angularXDrive = new JointMotor {   };
            ////outAxisCJ.rotationDriveMode = RotationDriveMode.Slerp;
            //outAxisCJ.slerpDrive = new JointDrive { maximumForce = 1000f };
            //axis.GetComponent<Rigidbody>().isKinematic = true;
            //axis.AddComponent<Rigidbody>().isKinematic = true;
            //axis.AddComponent<Rigidbody>();

            //HJ = axis.AddComponent<HingeJoint>();
            //HJ.connectedBody = Rigidbody;
            //HJ.motor = new JointMotor { targetVelocity = 5000f, freeSpin = true, force = 10000f };
            //HJ.axis = axis.transform.forward;
            //HJ.useMotor = false;
            AddPoint(axis);
            //addingPoint.transform.SetParent(axis.transform);
            //GameObject point = (GameObject)Instantiate(addingPoint);
            //point.transform.SetParent(axis.transform);
            //axis.layer = 12;
       
        }
        else
        {
            outAxisCJ = axis.GetComponent<ConfigurableJoint>();
            //HJ = axis.GetComponent<HingeJoint>();
        }


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

            //addingPoint.transform.RotateAround(addingPoint.transform.position, addingPoint.transform.forward, deltaAngularVelocity * Time.deltaTime);


            //axis.GetComponent<Rigidbody>().AddRelativeTorque(axis.transform.position * 60 * Time.deltaTime, ForceMode.Impulse);

            //axis.transform.localEulerAngles += Vector3.right  * 10f;
        }

        //Debug.Log(axis == null);
       
    }

    public override void SimulateFixedUpdateAlways()
    {
        //axis.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * strength, ForceMode.Force);
        //outAxisCJ.targetAngularVelocity = Vector3.up * 1000f;
        //outAxisCJ.targetRotation = Quaternion.AngleAxis(10, Vector3.up);
        //HJ.useMotor = true;
        axis.GetComponent<Rigidbody>().MoveRotation(Quaternion.AngleAxis(10f, axis.transform.forward));
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

    private void AddPoint(GameObject parentObject)
    {
        GameObject point = new GameObject("Adding Point")/*GameObject.CreatePrimitive(PrimitiveType.Cube)*/;
        point.transform.SetParent(parentObject.transform);
        point.transform.position = parentObject.transform.position;
        //point.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        point.layer = 12;

        BoxCollider boxCollider = point.AddComponent<BoxCollider>()/*point.GetComponent<BoxCollider>()*/ ;
        boxCollider.center = new Vector3(0, 0, -0.5f);
        boxCollider.size = new Vector3(0.6f, 0.6f, 0);
        boxCollider.isTrigger = true;

        //point.AddComponent<MeshFilter>();
        //point.AddComponent<MeshRenderer>().material.color = Color.red;
        //point.GetComponent<MeshRenderer>().material.color = Color.red;
    }
}

