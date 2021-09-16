using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;


class HighSpeedWheelBloclScript : BlockScript
{
    private MKey forwardKey, backwardKey;
    private MSlider speedSlider, springSlider, damperSlider, acceleratedSlider, staticFrictionSlider, dynamicFrictionSlider, bouncinessSlider, massSlider;
    private MToggle ignoreBaseColliderToggle, toggleToggle;

    private ConfigurableJoint CJ;
    private Vector3 lastScale;

    public HighWheel HighWheel;

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

        lastScale = transform.localScale;

        Rigidbody.inertiaTensorRotation = new Quaternion(0, 0, 0.4f, 0.9f);
        Rigidbody.inertiaTensor = new Vector3(0.4f, 0.4f, 0.7f);
        Rigidbody.drag = Rigidbody.angularDrag = 0f;

        Rigidbody.solverIterations = 100;
        Rigidbody.mass = 1f;

        CJ = GetComponent<ConfigurableJoint>();
        CJ.breakForce = CJ.breakTorque = Mathf.Infinity;
    }

    private event Action<Vector3, Vector3> onScale;
    //public override void BuildingUpdate()
    //{
    //    if (transform.localScale != lastScale)
    //    {
    //        onScale?.Invoke(transform.localScale, lastScale);
    //        setScale(transform.localScale, lastScale);

    //        lastScale = transform.localScale;
    //    }

    //    void setScale(Vector3 currentScale, Vector3 lastScale)
    //    {
    //        var single = 0f;
    //        if (currentScale.x != lastScale.x)
    //        {
    //            single = currentScale.x;
    //            //var slider = BlockBehaviour.Sliders.ToList().Find(match => match.Key == "y-scale");
    //            //slider?.SetValue(single);
    //        }
    //        else if (currentScale.y != lastScale.y)
    //        {
    //            single = currentScale.y;
    //            //var slider = BlockBehaviour.Sliders.ToList().Find(match => match.Key == "x-scale");
    //            //slider?.SetValue(single);
    //        }
    //        else
    //        {
    //            return;
    //        }
    //        //transform.localScale = new Vector3(single, single, currentScale.z);
    //        //HighWheel.SetRadius(HighWheel.Radius * single);
    //    }
    //}

    public override void OnSimulateStart()
    {
        Rigidbody.maxAngularVelocity = 50f * speedSlider.Value;

        Destroy(transform.FindChild("Boxes")?.gameObject);
        HighWheel = new HighWheel(transform, Rigidbody);
        HighWheel.SetJointDrive(springSlider.Value * 500f, damperSlider.Value, 5000f * springSlider.Value);
        HighWheel.SetPhysicMaterail(bouncinessSlider.Value, staticFrictionSlider.Value, dynamicFrictionSlider.Value);
        HighWheel.SetBodyAttribute(true,massSlider.Value);
        if (ignoreBaseColliderToggle.IsActive)
        {
            StartCoroutine(ignore());
        }

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

        IEnumerator ignore()
        {
            yield return new WaitUntil(() => CJ.connectedBody != null);
            //HighWheel.IgnorBaseBlockCollider();
        }
    }

    float input = 0f, single = 0f, single1 = 0f;
    public override void SimulateUpdateAlways()
    {
        if (!toggleToggle.IsActive)
        {
            input = 0f;
            if (forwardKey.IsHeld)
            {
                input += 1f;
                Rigidbody.WakeUp();
            }
            if (backwardKey.IsHeld)
            {
                input += -1f;
                Rigidbody.WakeUp();
            }
        }
        else
        {
            if (forwardKey.IsPressed)
            {
                input = input != 1f ? 1f : 0f;
                Rigidbody.WakeUp();
            }
            if (backwardKey.IsPressed)
            {
                input = input != -1f ? -1f : 0f;
                Rigidbody.WakeUp();
            }
        }

        if (input == 0f)
        {
            single1 = Mathf.MoveTowards(single1, 750f, 10f * Time.deltaTime);
            var jd = CJ.angularXDrive;
            jd.positionDamper = single1;
            CJ.angularXDrive = jd;
        }
        else
        {
            var jd = CJ.angularXDrive;
            jd.positionDamper = 0f;
            CJ.angularXDrive = jd;
            single = Mathf.MoveTowards(single, 11.5f, input == 0f ? 0f : acceleratedSlider.Value * Time.deltaTime * 10f);
            //CJ.targetAngularVelocity = Vector3.right * (Flipped ? -1f : 1f) * (CJ.swapBodies ? -1f : 1f) * input * speedSlider.Value * single;
            Rigidbody.AddRelativeTorque(Vector3.forward * (Flipped ? -1f : 1f) /** (CJ.swapBodies ? -1f : 1f)*/ * input * speedSlider.Value * single, ForceMode.VelocityChange);
        }


        //Boxes.refreshVertices();
    }
    public override void SimulateFixedUpdateAlways()
    {
 
    }
}
class HighWheel
{
    public GameObject gameObject;
    private static ModMesh mesh;
    public MeshCollider meshCollider;
    public ConfigurableJoint CJ;
    public Rigidbody Rigidbody;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Transform parent;
    private Rigidbody connectedBody;
    public HighWheel(Transform parent, Rigidbody connectedBody)
    {
        this.parent = parent;
        this.connectedBody = connectedBody;

        gameObject = new GameObject("High Wheel");
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = parent.position;
        gameObject.transform.rotation = parent.rotation;
        gameObject.transform.localScale = Vector3.one;
        gameObject.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
        gameObject.transform.localPosition = new Vector3(1.5f, 0f, 0.5f);

        var offect_forward = 0.5f;
        var origin = gameObject.transform.localPosition;
        var anchor = Vector3.forward * offect_forward;

        var mf = gameObject.AddComponent<MeshFilter>() ?? gameObject.GetComponent<MeshFilter>();
#if DEBUG
        var mr = gameObject.AddComponent<MeshRenderer>() ?? gameObject.GetComponent<MeshRenderer>();
        mr.material.color = Color.red;
#endif
        var mc = meshCollider = gameObject.AddComponent<MeshCollider>() ?? gameObject.GetComponent<MeshCollider>();
        mf.mesh = mc.sharedMesh = mesh = mesh ?? ModResource.GetMesh("hswheel-obj");
        mc.convex = true;

        addJoint(connectedBody);

    }

    private void addJoint(Rigidbody connectedBody)
    {
        CJ = gameObject.AddComponent<ConfigurableJoint>();
        Rigidbody = gameObject.GetComponent<Rigidbody>();

        CJ.autoConfigureConnectedAnchor = false;
        CJ.connectedBody = connectedBody;
        CJ.connectedAnchor = new Vector3(0f, 0f, 0.5f);
        CJ.anchor = new Vector3(0f, 0f, 1.5f);
        CJ.axis = Vector3.up;
        CJ.secondaryAxis = Vector3.forward;
        CJ.xMotion = CJ.yMotion = ConfigurableJointMotion.Limited;
        CJ.angularXMotion = CJ.angularYMotion = CJ.angularZMotion = CJ.zMotion =ConfigurableJointMotion.Locked;

        SetRadius(0.35f * parent.localScale.x);
        SetJointDrive();
        SetJointAttribute();
        SetBodyAttribute();
    }
    public void SetRadius(float radius)
    {
        var lls = CJ.linearLimit;
        lls.limit = radius;
        lls.bounciness = 0f;
        lls.contactDistance = 0f;
        CJ.linearLimit = lls;
    }
    public void SetJointDrive(float spring = 400f, float damper = 50f, float maximumForce = 5000f)
    {
        var jointDrive = CJ.xDrive;
        jointDrive.positionSpring = spring;
        jointDrive.positionDamper = damper;
        jointDrive.maximumForce = maximumForce;
        CJ.xDrive = CJ.yDrive = jointDrive;
    }
    public void SetJointAttribute(float breakForce = Mathf.Infinity, float breakTorque = Mathf.Infinity, bool enableCollision = false, bool enablePreprocessing = false, JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation, float projectionDistance = 0.001f, float projectionAngle = 3f)
    {
        var cj = CJ;
        cj.breakForce = breakForce;
        cj.breakTorque = breakTorque;
        cj.enableCollision = enableCollision;
        cj.enablePreprocessing = enablePreprocessing;
        cj.projectionMode = projectionMode;
        cj.projectionDistance = projectionDistance;
        cj.projectionAngle = projectionAngle;
    }
    public void SetPhysicMaterail(float bounciness = 0f, float staticFriction = 0.5f, float dynamicFriction = 0.8f, PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum, PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Minimum)
    {
        var mc = meshCollider;
        mc.material.bounciness = bounciness;
        mc.material.staticFriction = staticFriction;
        mc.material.dynamicFriction = dynamicFriction;
        mc.material.frictionCombine = frictionCombine;
        mc.material.bounceCombine = bounceCombine;
    }
    public void SetBodyAttribute(bool useGravity = true, float mass = 0.5f, float drag = 0f, float angularDrag = 0f, CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete)
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        rb.useGravity = useGravity;
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.collisionDetectionMode = collisionDetectionMode;
        //rb.maxAngularVelocity = 50f;
        rb.solverIterations = 200;
    }
    //public void SetBoxesColliderState(bool enabled)
    //{
    //    foreach (var box in boxes)
    //    {
    //        var rb = box.rigidbody;
    //        rb.detectCollisions = enabled;
    //        rb.isKinematic = !enabled;
    //    }
    //}
    //public void RefreshBoxesCollider(float spring, float damper, float maximumForce)
    //{
    //    foreach (var box in boxes)
    //    {
    //        box.SetJointDrive(spring, damper, maximumForce);
    //    }
    //}
    //public void SetBoxesPhysicMaterail(float bounciness, float staticFriction, float dynamicFriction)
    //{
    //    foreach (var box in boxes)
    //    {
    //        box.SetPhysicMaterail(bounciness, staticFriction, dynamicFriction);
    //    }
    //}

    //public void SetBoxesBodyAttribute(float mass)
    //{
    //    foreach (var box in boxes)
    //    {
    //        box.SetBodyAttribute(true, mass);
    //    }
    //}
    //public Vector3[] GetAllVertices()
    //{
    //    var index = boxes.Length;
    //    var vectors = new Vector3[index * 2];

    //    var j = 0;
    //    for (var i = 0; i < index; i++)
    //    {
    //        vectors[j++] = gameObject.transform./*parent.*/TransformPoint(boxes[i].GetVertices()[0]);
    //        vectors[j++] = gameObject.transform./*parent.*/TransformPoint(boxes[i].GetVertices()[1]);
    //    }
    //    return vectors;
    //}
    //public void refreshVertices()
    //{
    //    meshFilter.mesh.vertices = GetAllVertices();
    //    var index = GetAllVertices().Length;
    //    var uvs = new Vector2[index];
    //    var tris = new int[(index - 2) * 3];
    //    var j = 0;
    //    for (var i = 0; i < index; i++)
    //    {
    //        uvs[i] = new Vector2(1.0f * i / index, 1);
    //        if (j < tris.Length)
    //        {
    //            if (i % 2 != 0)
    //            {
    //                tris[j++] = i;
    //                tris[j++] = i + 1;
    //                tris[j++] = i + 2 > index ? 0 : i + 2;
    //            }
    //            else
    //            {
    //                tris[j++] = i + 2 > index ? i - 8 : i + 2;
    //                tris[j++] = i + 1 > index ? 0 : i + 1;
    //                tris[j++] = i;
    //            }
    //        }
    //    }

    //    meshFilter.mesh.uv = uvs;
    //    meshFilter.mesh.triangles = tris;
    //    meshFilter.mesh.RecalculateBounds();
    //    meshFilter.mesh.RecalculateNormals();
    //}

    //public void IgnorBaseBlockCollider()
    //{
    //    foreach (var col in connectedBody.gameObject.GetComponent<ConfigurableJoint>().connectedBody?.gameObject.GetComponentsInChildren<Collider>())
    //    {
    //        foreach (var box in boxes)
    //        {
    //            Physics.IgnoreCollision(box.meshCollider, col);
    //        }
    //    }
    //}
}


