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
        Rigidbody.solverVelocityIterations = 20;
        Rigidbody.solverIterations = 200;

        CJ = GetComponent<ConfigurableJoint>();
        CJ.breakForce = CJ.breakTorque = Mathf.Infinity;
    }

    private event Action<Vector3, Vector3> onScale;
    public override void BuildingUpdate()
    {
        if (transform.localScale != lastScale)
        {
            onScale?.Invoke(transform.localScale, lastScale);
            setScale(transform.localScale, lastScale);

            lastScale = transform.localScale;
        }

        void setScale(Vector3 currentScale, Vector3 lastScale)
        {
            if (currentScale.x != lastScale.x || currentScale.y != lastScale.y)
            {
                HighWheel.SetStroke((currentScale.x + currentScale.y) * 0.5f);
            }
        }
    }

    public override void OnSimulateStart()
    {
        Rigidbody.maxAngularVelocity = 50f * speedSlider.Value;

        Destroy(transform.FindChild("Boxes")?.gameObject);
        HighWheel = new HighWheel(transform, Rigidbody);
        HighWheel.SetJointDrive(springSlider.Value * 500f, damperSlider.Value, 5000f * springSlider.Value);
        HighWheel.SetPhysicMaterail(bouncinessSlider.Value, staticFrictionSlider.Value, dynamicFrictionSlider.Value);
        HighWheel.SetBodyAttribute(massSlider.Value);
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
            HighWheel.IgnorBaseBlockCollider();
        }
    }

    float input = 0f, single = 0f, single1 = 0f;
    public override void SimulateUpdateAlways()
    {
    


        //Boxes.refreshVertices();
    }
    public override void SimulateFixedUpdateAlways()
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
    }
}
class HighWheel
{
    public GameObject gameObject;
    public WheelBox[] boxes;
    public float Stroke { get; set; } = 0.2f;

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
        gameObject.transform.localScale = parent.transform.localScale;

        var offect_forward = 0.5f;
        var origin = gameObject.transform.localPosition;
        var anchor = Vector3.forward * offect_forward;

        float angle = 18f;
        int index = (int)(180f / angle);

        boxes = new WheelBox[index];
        //外圈box位置
        for (var i = 0; i < index; i++)
        {
            boxes[i] = new WheelBox(gameObject.transform, angle * i, anchor, connectedBody, Stroke);
        }

        for (var i = 0; i < index; i++)
        {
            for (var j = 0; j < index; j++)
            {
                Physics.IgnoreCollision(boxes[i].meshCollider, boxes[j].meshCollider);
            }
        }
    }

    public void SetStroke(float stroke)
    {
        Stroke = stroke;

        for (int i = 0; i < boxes.Length; i++)
        {
            boxes[i].SetStroke(stroke);
        }
    }
    public void SetColliderState(bool enabled)
    {
        foreach (var box in boxes)
        {
            var rb = box.rigidbody;
            rb.detectCollisions = enabled;
            rb.isKinematic = !enabled;
        }
    }
    public void SetJointDrive(float spring, float damper, float maximumForce)
    {
        foreach (var box in boxes)
        {
            box.SetJointDrive(spring, damper, maximumForce);
        }
    }
    public void SetPhysicMaterail(float bounciness, float staticFriction, float dynamicFriction)
    {
        foreach (var box in boxes)
        {
            box.SetPhysicMaterail(bounciness, staticFriction, dynamicFriction);
        }
    }

    public void SetBodyAttribute(float mass)
    {
        foreach (var box in boxes)
        {
            box.SetBodyAttribute(true, mass);
        }
    }
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

    public void IgnorBaseBlockCollider()
    {
        foreach (var col in connectedBody.gameObject.GetComponent<ConfigurableJoint>().connectedBody?.gameObject.GetComponentsInChildren<Collider>())
        {
            foreach (var box in boxes)
            {
                Physics.IgnoreCollision(box.meshCollider, col);
            }
        }
    }

    public class WheelBox 
    {
        public GameObject gameObject;
        private ConfigurableJoint configurableJoint;

        private static ModMesh mesh;
        public MeshCollider meshCollider;
        public Rigidbody rigidbody { get { return gameObject.GetComponent<Rigidbody>(); } }
        public float Stroke { get; set; }
        public WheelBox(Transform parent, float angle, Vector3 connectedAnchor, Rigidbody connectedBody, float stroke)
        {
            gameObject = new GameObject("box");
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = parent.position;
            gameObject.transform.rotation = parent.rotation;
            gameObject.transform.localPosition = connectedAnchor * 1f / parent.localScale.z;
            gameObject.transform.localScale = new Vector3(1f / parent.localScale.z, 1f / parent.localScale.x, 1f / parent.localScale.x);
            gameObject.transform.localEulerAngles = new Vector3(90f, 0f, 90f);
            gameObject.transform.Rotate(Vector3.right, angle);

            var mf = gameObject.AddComponent<MeshFilter>() ?? gameObject.GetComponent<MeshFilter>();
#if DEBUG
            var mr = gameObject.AddComponent<MeshRenderer>() ?? gameObject.GetComponent<MeshRenderer>();
            mr.material.color = Color.red;
#endif
            var mc = meshCollider = gameObject.AddComponent<MeshCollider>() ?? gameObject.GetComponent<MeshCollider>();
            mf.mesh = mc.sharedMesh = mesh = mesh ?? ModResource.GetMesh("hswheel-obj");
            mc.convex = true;

            SetPhysicMaterail();

            addJoint(connectedAnchor, connectedBody);
            SetStroke(stroke);
            SetJointDrive();
            SetJointAttribute();
            SetBodyAttribute();
        }
        private void addJoint(Vector3 anchor, Rigidbody connectedBody)
        {
            var cj = configurableJoint = gameObject.AddComponent<ConfigurableJoint>();
            cj.autoConfigureConnectedAnchor = false;
            cj.connectedBody = connectedBody;
            cj.connectedAnchor = anchor;
            cj.anchor = Vector3.zero;
            cj.axis = Vector3.forward;
            cj.xMotion = ConfigurableJointMotion.Limited;
            cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = cj.zMotion = cj.yMotion = ConfigurableJointMotion.Locked;
        }

        public void SetStroke(float stroke,float targetPosition = 0f)
        {
            Stroke = stroke;
            var _radius = stroke * gameObject.transform.parent.transform.localScale.x;

            var softJointLimit = configurableJoint.linearLimit;
            softJointLimit.limit = _radius;
            configurableJoint.linearLimit = softJointLimit;

            configurableJoint.targetPosition = new Vector3(targetPosition, 0f, 0f);
        }

        public void SetJointDrive(float spring = 400f, float damper = 50f, float maximumForce = 500f)
        {
            var jointDrive = configurableJoint.xDrive;
            jointDrive.positionSpring = spring;
            jointDrive.positionDamper = damper;
            jointDrive.maximumForce = maximumForce;
            configurableJoint.xDrive = jointDrive;
        }
        public void SetJointAttribute(float breakForce = Mathf.Infinity, float breakTorque = Mathf.Infinity, bool enableCollision = false, bool enablePreprocessing = false, JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation, float projectionDistance = 0.001f, float projectionAngle = 3f)
        {
            var cj = configurableJoint;
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
        public void SetBodyAttribute(bool useGravity = true, float mass = 0.25f, float drag = 0f, float angularDrag = 0f, CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete)
        {
            var rb = gameObject.GetComponent<Rigidbody>();
            rb.useGravity = useGravity;
            rb.mass = mass;
            rb.drag = drag;
            rb.angularDrag = angularDrag;
            rb.collisionDetectionMode = collisionDetectionMode;
            //rb.maxAngularVelocity = 50f;
            rb.solverIterations = 200;
            rb.solverVelocityIterations = 20;
        }
    }
}


