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
    MMenu ModelMenu;
    MSlider StrengthSlider;
    MSlider RatioSlider;
    

    GameObject OutAxis;
    ConfigurableJoint CJ, CJ_Axis;
    Rigidbody parentRigidbody, axisRigidbody;

    /// <summary>变速箱角速度</summary>
    public float AngularVelocity { get; private set; } = 0f;
    /// <summary>输入轴角速度</summary>
    public float ParentAngularVelocity { get; private set; } = 0f;

    /// <summary>离合</summary>
    public bool Clutch { get { return !ClutchKey.IsDown; } set { Clutch = value; } }
    /// <summary>马力</summary>
    public float Strength { get; set; } = 1f;
    /// <summary>变速比例</summary>
    public float Ratio { get; set; } = 1f;
    /// <summary>总档数</summary>
    public int Gears { get; set; } = 5;

    public enum model
    {
        speed = 0,
        angle = 1,
        transform=2
    }
    /// <summary>工作模式</summary>
    public model Model { get; set; } = model.angle;
    /// <summary>输入量 当前档位</summary>
    public int Input { get; set; } = 1;

    /// <summary>输入轴和变速箱的角速度差</summary>
    private float deltaAngularVelocity = 0f;
    private float feedSpeed = 0f;

    public override void SafeAwake()
    {
        UpKey = AddKey(LanguageManager.Instance.CurrentLanguage.UpKey, "Up", KeyCode.U);
        DownKey = AddKey(LanguageManager.Instance.CurrentLanguage.DownKey, "Down", KeyCode.J);
        BackKey = AddKey(LanguageManager.Instance.CurrentLanguage.BackKey, "Back", KeyCode.K);
        ClutchKey = AddKey(LanguageManager.Instance.CurrentLanguage.ClutchKey, "Clutch", KeyCode.C);
        ModelMenu = AddMenu("Model", 0, LanguageManager.Instance.CurrentLanguage.Model);
        StrengthSlider = AddSlider(LanguageManager.Instance.CurrentLanguage.Strength, "Force", Strength, 0, 10f);
        RatioSlider = AddSlider(LanguageManager.Instance.CurrentLanguage.Ratio, "Ratio", Ratio, 0f, 2f);

        ModelMenu.ValueChanged += (value) => 
        {
            Model = (model)ModelMenu.Value;
            DisplayInMapper();            
        };
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
            CJ_Axis.axis = Vector3.up; CJ_Axis.secondaryAxis = Vector3.right;
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
        axisRigidbody.isKinematic = true;

        CJ = GetComponent<ConfigurableJoint>();

        DisplayInMapper();
    }

    void DisplayInMapper()
    {
        ClutchKey.DisplayInMapper = (Model == model.speed);
    }

    public override void OnSimulateStart()
    {
        StartCoroutine(GetParentRigidbody());
        axisRigidbody.isKinematic = false;
        Debug.Log(Model);
        if (Model == model.transform)
        {
            axisRigidbody.constraints = RigidbodyConstraints.FreezeRotationY;
        }
        else
        {
            axisRigidbody.constraints = RigidbodyConstraints.None;
        }
    }

    public override void SimulateUpdateAlways()
    {
        if (parentRigidbody != null)
        {
            ParentAngularVelocity = Vector3.Scale(transform.InverseTransformVector(parentRigidbody.angularVelocity), Vector3.forward).z;
            AngularVelocity = Vector3.Scale(transform.InverseTransformVector(Rigidbody.angularVelocity), Vector3.forward).z;

            deltaAngularVelocity = (AngularVelocity - ParentAngularVelocity) * (Flipped ? 1 : -1);

            feedSpeed = deltaAngularVelocity * Ratio * Input * 57.5f * 0.3f * Time.deltaTime; ;

            int sign = 0;
            if (UpKey.IsPressed) sign = 1;
            if (DownKey.IsPressed) sign = -1;
            if (sign != 0) Input = Mathf.Clamp(Input + sign, 1, Gears);
            if (BackKey.IsPressed) Input = -1;

            if (Model == model.speed)
            {
                if (Clutch)
                {
                    CJ_Axis.angularXDrive = new JointDrive { maximumForce = Strength * 10000f, positionDamper = 50f, positionSpring = 0 };
                    CJ_Axis.targetAngularVelocity = Vector3.right * feedSpeed * 2f;
                }  
                else
                {
                    CJ_Axis.angularXDrive = new JointDrive { maximumForce = 0, positionDamper = 0, positionSpring = 0 };
                }
            }
            else if(Model == model.angle)
            {
                CJ_Axis.angularXDrive = new JointDrive { maximumForce = Strength * 1000f, positionDamper = 50f, positionSpring = Strength * 50000f };
                CJ_Axis.targetRotation *= Quaternion.Euler(feedSpeed, 0, 0);
            }

           
        }
    }

    public override void SimulateFixedUpdateAlways()
    {
        if (parentRigidbody != null)
        {
            if (Model == model.transform)
            {
                axisRigidbody.WakeUp();
                axisRigidbody.MoveRotation(axisRigidbody.rotation * Quaternion.AngleAxis(feedSpeed, transform.TransformDirection(transform.InverseTransformDirection(Vector3.up))));
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

    //添加连接点
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

