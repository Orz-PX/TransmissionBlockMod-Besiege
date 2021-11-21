using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using Modding.Modules;
using Modding.Modules.Official;
using UnityEngine;

class WheelBlockScript : BlockScript
{

    private MKey forwardKey, backwardKey;
    private MSlider speedSlider, springSlider, damperSlider, acceleratedSlider, staticFrictionSlider, dynamicFrictionSlider, bouncinessSlider, massSlider;
    private MToggle ignoreBaseColliderToggle, toggleToggle;
    private float springMultiplier = 500f;
    private float damperMultiplier = 10f;
    private float maxForceMultiplier = 5000f;
    private float maxAngularVelocityMultiplier = 10f;

    private ConfigurableJoint CJ;
    //private Vector3 lastScale;

    public Boxes Boxes;

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

        //lastScale = transform.localScale;

        Rigidbody.inertiaTensorRotation = new Quaternion(0, 0, 0.4f, 0.9f);
        Rigidbody.inertiaTensor = new Vector3(0.4f, 0.4f, 0.7f);
        Rigidbody.drag = Rigidbody.angularDrag = 0f;
        Rigidbody.solverVelocityIterations = 1;
        Rigidbody.solverIterations = 100;

        CJ = GetComponent<ConfigurableJoint>();
        CJ.breakForce = CJ.breakTorque = Mathf.Infinity;
    }

    private event Action<Vector3,Vector3> onScale;
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
    //        }
    //        else if (currentScale.y != lastScale.y)
    //        {
    //            single = currentScale.y;
    //        }
    //        else
    //        {
    //            return;
    //        }
    //        transform.localScale = new Vector3(single, single, currentScale.z);
    //        Boxes.SetStroke(Boxes.Stroke * single);
    //    }
    //}
    GameObject[] gos = new GameObject[50];
    public override void OnSimulateStart()
    {
        Rigidbody.maxAngularVelocity = speedSlider.Value * maxAngularVelocityMultiplier;

        Destroy(transform.FindChild("Boxes")?.gameObject);
        Boxes = new Boxes(transform, Rigidbody);
        Boxes.SetJointDrive(springSlider.Value * springMultiplier, damperSlider.Value * damperMultiplier, springSlider.Value * maxForceMultiplier);
        Boxes.SetBoxesPhysicMaterail(bouncinessSlider.Value, staticFrictionSlider.Value, dynamicFrictionSlider.Value);
        Boxes.SetBoxesBodyAttribute(massSlider.Value);
        StartCoroutine(ignoreBaseCollider(ignoreBaseColliderToggle.IsActive));

        addDynamicAxis();

        //foreach (var box in Boxes.boxes)
        //{
        //    var index = Boxes.boxes.ToList().IndexOf(box);
        //    var go = gos[index] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    go.transform.position = box.gameObject.transform.TransformPoint(box.rigidbody.centerOfMass) - transform.position;
        //    go.transform.localScale = Vector3.one * 0.4f;
        //    go.GetComponent<Collider>().isTrigger = true;

        //    go.AddComponent<DestroyIfEditMode>();
        //}

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

        IEnumerator ignoreBaseCollider(bool active)
        {
            if (active)
            {
                yield return new WaitUntil(() => CJ.connectedBody != null);
                Boxes.IgnorBaseBlockCollider();
            }
            yield break;
        }
    }
    public override void SimulateUpdateAlways()
    {
        //foreach (var box in Boxes.boxes)
        //{
        //    var index = Boxes.boxes.ToList().IndexOf(box);
        //    var go = gos[index];
        //    go.transform.position = Boxes.gameObject.transform.TransformPoint(box.rigidbody.centerOfMass);

        //}

        //Boxes.refreshVertices();
        //Boxes.RefreshCenterOfMass();
    }
    float input = 0f, single = 0f, single1 = 0f;
    public override void SimulateFixedUpdateAlways()
    {
        if (CJ == null || CJ?.connectedBody == null) return;

        if (!toggleToggle.IsActive)
        {
            input = 0f;
            if (forwardKey.IsHeld)
            {
                input += 1f;
            }
            if (backwardKey.IsHeld)
            {
                input += -1f;
            }
        }
        else
        {
            if (forwardKey.IsPressed)
            {
                input = input != 1f ? 1f : 0f;
            }
            if (backwardKey.IsPressed)
            {
                input = input != -1f ? -1f : 0f;
            }
        }

        if (input == 0f)
        {
            Rigidbody.WakeUp();
            single = 0f;
            single1 = Mathf.MoveTowards(single1, 750f, 10f * Time.deltaTime);
            var jd = CJ.angularXDrive;
            jd.positionDamper = single1;
            CJ.angularXDrive = jd;
        }
        else
        {
            Rigidbody.WakeUp();
            var jd = CJ.angularXDrive;
            jd.positionDamper = 0f;
            CJ.angularXDrive = jd;
            single1 = 0;
            single = Mathf.MoveTowards(single, 11.5f, input == 0f ? 0f : acceleratedSlider.Value * Time.deltaTime * 10f);
            Rigidbody.AddRelativeTorque(Vector3.forward * (Flipped ? -1f : 1f) * input * speedSlider.Value * single, ForceMode.VelocityChange);
        }
    }
    public override void SimulateLateUpdateAlways()
    {
        base.SimulateLateUpdateAlways();

        Boxes.RefreshCenterOfMass(0.95f);
    }
}
class Boxes
{
    public GameObject gameObject;
    public Box[] boxes;
    public float Stroke { get; set; } = 0.25f;
    public float Radius { get; set; } = 1.5f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Transform parent;
    private Rigidbody connectedBody;
    public Boxes(Transform parent, Rigidbody connectedBody)
    {
        this.parent = parent;
        this.connectedBody = connectedBody;

        gameObject = new GameObject("Boxes");
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = parent.position;
        gameObject.transform.rotation = parent.rotation;
        //gameObject.transform.localScale = parent.localScale;

        var offset_forward = 0.5f;
        //var origin = gameObject.transform.localPosition;
        //var origin = Vector3.zero;
        var anchor = Vector3.forward * offset_forward;
        Stroke /= gameObject.transform.localScale.x;
        //圆半径和旋转角
        //float radius = Stroke / gameObject.transform.localScale.x;
        //float radius = Radius / gameObject.transform.localScale.x;
        float angle = 18f;
        int index = (int)(360f / angle) /** 0+1*/;

        boxes = new Box[index];
        //外圈box位置
        for (var i = 0; i < index; i++)
        {
            //var position = new Vector3(
            //                                    /*origin.x - */Radius / gameObject.transform.localScale.x * Mathf.Sin(angle * i * Mathf.Deg2Rad),
            //                                    /*origin.y +*/ Radius / gameObject.transform.localScale.y * Mathf.Cos(angle * i * Mathf.Deg2Rad),
            //                                    offset_forward / gameObject.transform.localScale.z
            //                                 );

            boxes[i] = new Box(gameObject.transform, connectedBody, angle * i, offset_forward, Radius, Stroke);
        }

        for (var i = 0; i < index; i++)
        {
            for (var j = 0; j < index; j++)
            {
                Physics.IgnoreCollision(boxes[i].meshCollider, boxes[j].meshCollider);
            }
        }

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material.color = Color.green;


    }
    public void SetStroke(float stroke)
    {
        Stroke = stroke;

        for (int i = 0; i < boxes.Length; i++)
        {
            boxes[i].SetStroke(stroke);
        }
    }
    public void SetBoxesColliderState(bool enabled)
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
    public void SetBoxesPhysicMaterail(float bounciness, float staticFriction, float dynamicFriction)
    {
        foreach (var box in boxes)
        {
            box.SetPhysicMaterail(bounciness, staticFriction, dynamicFriction);
        }
    }

    public void SetBoxesBodyAttribute(float mass)
    {
        foreach (var box in boxes)
        {
            box.SetBodyAttribute(true, mass);
        }
    }
    public void RefreshCenterOfMass(float offset = 1f)
    {
        foreach (var box in boxes)
        {
            box.RefreshCenterOfMass(offset);
        }
    }
    public Vector3[] GetAllVertices()
    {
        var index = boxes.Length;
        var vectors = new Vector3[index * 2];

        var j = 0;
        for (var i = 0; i < index; i++)
        {
            vectors[j++] = gameObject.transform./*parent.*/TransformPoint(boxes[i].GetVertices()[0]);
            vectors[j++] = gameObject.transform./*parent.*/TransformPoint(boxes[i].GetVertices()[1]);
        }
        return vectors;
    }
    public void refreshVertices()
    {
        meshFilter.mesh.vertices = GetAllVertices();
        var index = GetAllVertices().Length;
        var uvs = new Vector2[index];
        var tris = new int[(index - 2) * 3];
        var j = 0;
        for (var i = 0; i < index; i++)
        {
            uvs[i] = new Vector2(1.0f * i / index, 1);
            if (j < tris.Length)
            {
                if (i % 2 != 0)
                {
                    tris[j++] = i;
                    tris[j++] = i + 1;
                    tris[j++] = i + 2 > index ? 0 : i + 2;
                }
                else
                {
                    tris[j++] = i + 2 > index ? i - 8 : i + 2;
                    tris[j++] = i + 1 > index ? 0 : i + 1;
                    tris[j++] = i;
                }
            }
        }

        meshFilter.mesh.uv = uvs;
        meshFilter.mesh.triangles = tris;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
    }

    public void IgnorBaseBlockCollider()
    {
        foreach (var col in connectedBody.gameObject.GetComponent<ConfigurableJoint>()?.connectedBody?.gameObject.GetComponentsInChildren<Collider>())
        {
            foreach (var box in boxes)
            {
                Physics.IgnoreCollision(box.meshCollider, col);
            }
        }
    }
}

class Box
{
    public GameObject gameObject;
    private ConfigurableJoint configurableJoint;

    private static ModMesh mesh;
    public MeshCollider meshCollider;
    public Rigidbody rigidbody { get { return gameObject.GetComponent<Rigidbody>(); } }
    public float Stroke { get; private set; }
    public float Radius { get; private set; }
    public Box(Transform parent, Rigidbody connectedBody,float angle, float offset_forward, float radius, float stroke)
    {
        Radius = radius;

        gameObject = new GameObject("box");
        gameObject.transform.SetParent(parent);

        var xFactor = Mathf.Sin(angle * Mathf.Deg2Rad);
        var yFactor = Mathf.Cos(angle * Mathf.Deg2Rad);
        var vector = new Vector3(Radius * xFactor, Radius * yFactor, offset_forward);
        var vetcor1 = new Vector3(1f / parent.localScale.x, 1f / parent.localScale.y, 1f / parent.localScale.z); ;
        gameObject.transform.localPosition = Vector3.Scale(vector, vetcor1);

        var connectedAnchor = Vector3.forward * (offset_forward / parent.localScale.z);
        gameObject.transform.LookAt(parent.TransformPoint(connectedAnchor));

        var single = Vector3.Dot(parent.forward, gameObject.transform.up);
        var _angle = Vector3.Angle(parent.forward, gameObject.transform.right);
        gameObject.transform.Rotate(Vector3.forward * Mathf.Sign(single), _angle);

        //gameObject.transform.localScale = new Vector3(0.1f / parent.localScale.z, 0.1f / parent.localScale.x, 0.1f / parent.localScale.x);
        //model scale factor
        var factor = 0.1f;
        var localScale = parent.parent.localScale;
        gameObject.transform.localScale = new Vector3(localScale.z, localScale.y + Mathf.Abs(xFactor) * (1f - localScale.y), localScale.x + Mathf.Abs(yFactor) * (1f - localScale.x)) * factor;

        var mf = gameObject.AddComponent<MeshFilter>() ?? gameObject.GetComponent<MeshFilter>();
        var mc = meshCollider = gameObject.AddComponent<MeshCollider>() ?? gameObject.GetComponent<MeshCollider>();
        mf.mesh = mc.sharedMesh = mesh = mesh ?? ModResource.GetMesh("wheel-obj");
        mc.convex = true;
#if DEBUG
        var mr = gameObject.AddComponent<MeshRenderer>() ?? gameObject.GetComponent<MeshRenderer>();
        mr.material.color = Color.red;
#endif

        //SetPhysicMaterail();

        //addJoint(connectedAnchor, connectedBody);
        //SetStroke(stroke);
        ////SetRadiusAndStroke(radius);
        ////SetStroke(stroke);
        //SetJointDrive();
        //SetJointAttribute();
        //SetBodyAttribute();
    }
    private void addJoint(Vector3 anchor, Rigidbody connectedBody)
    {
        var cj = configurableJoint = gameObject.AddComponent<ConfigurableJoint>();
        cj.autoConfigureConnectedAnchor = false;
        cj.connectedBody = connectedBody;
        cj.enablePreprocessing = false;
        cj.anchor = Vector3.zero;
        cj.axis = Vector3.forward;
        cj.xMotion = ConfigurableJointMotion.Limited;
        cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = cj.zMotion = cj.yMotion = ConfigurableJointMotion.Locked;
    }
    public void SetStroke(float stroke)
    {
        Stroke = stroke;

        //radius
        var cj = configurableJoint;
        var parent = cj.connectedBody.transform;
        var radius = Radius /** parent.localScale.x*/;
        var connectedAnchor = parent.InverseTransformDirection(gameObject.transform.position - parent.position);
        var single = 1f - (stroke * 0.5f) / radius;
        var single1 = 1f / parent.localScale.z;
        var vector = Vector3.Scale(new Vector3(single, single, single1),parent.parent.localScale);
        //cj.connectedAnchor = anchor ;
        cj.connectedAnchor = Vector3.Scale(vector, connectedAnchor);

        //stroke
        //var _stroke = stroke * gameObject.transform.parent.transform.localScale.x;
        var _stroke = stroke;
        _stroke = _stroke * 0.5f;

        var softJointLimit = configurableJoint.linearLimit;
        softJointLimit.limit = _stroke;
        softJointLimit.contactDistance = _stroke;
        configurableJoint.linearLimit = softJointLimit;
        configurableJoint.targetPosition = new Vector3(_stroke, 0f, 0f);
    }
    //public void SetStroke(float stroke)
    //{
    //    //var _stroke = stroke * gameObject.transform.parent.transform.localScale.x;
    //    var _stroke = stroke;
    //    _stroke = _stroke * 0.5f;

    //    var softJointLimit = configurableJoint.linearLimit;
    //    softJointLimit.limit = _stroke;
    //    softJointLimit.contactDistance = _stroke;
    //    configurableJoint.linearLimit = softJointLimit;
    //    configurableJoint.targetPosition = new Vector3(_stroke, 0f, 0f); 
    //}
    public void SetJointDrive(float spring = 400f, float damper = 50f, float maximumForce = 500f)
    {
        if (configurableJoint == null) return;

        var jointDrive = configurableJoint.xDrive;
        jointDrive.positionSpring = spring;
        jointDrive.positionDamper = damper;
        jointDrive.maximumForce = maximumForce;
        configurableJoint.xDrive = jointDrive;
    }
    public void SetJointAttribute(float breakForce = Mathf.Infinity, float breakTorque = Mathf.Infinity, bool enableCollision = false, bool enablePreprocessing = false, JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation, float projectionDistance = 0.001f, float projectionAngle = 3f)
    {
        if (configurableJoint == null) return;

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
    public void SetBodyAttribute(bool useGravity = true, float mass = 0.15f, float drag = 0f, float angularDrag = 0f, CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete)
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.angularDrag = angularDrag;
        rb.useGravity = useGravity;
        rb.mass = mass;
        rb.drag = drag;
        rb.solverIterations = 100;
        rb.maxAngularVelocity = 100f;
        rb.solverVelocityIterations = 1;
        //rb.collisionDetectionMode = collisionDetectionMode;

        RefreshCenterOfMass(1f);
#if DEBUG //render center of mass
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var parent = gameObject.transform;
        go.transform.SetParent(parent);
        go.GetComponent<Collider>().isTrigger = true;
        go.transform.rotation = parent.rotation;
        go.transform.position = parent.TransformDirection(rb.centerOfMass + Vector3.right * 0.5f) + parent.position;
        go.transform.localScale *= 0.5f;
#endif
    }
    public void RefreshCenterOfMass(float offset = 1f)
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        var distance = gameObject.transform.InverseTransformDirection(gameObject.transform.parent.position - gameObject.transform.position);
        if (rb != null)
        {
            rb.centerOfMass = Vector3.Scale(Vector3.forward * offset, distance);
        }
    }
    public Vector3[] GetVertices()
    {
        var vertices = new Vector3[2] { Vector3.zero, Vector3.zero };
        var offect = 1f;
        var direction = gameObject.transform.right;

        var position = gameObject.transform.localPosition;
        vertices[0] = position + direction * offect;
        vertices[1] = position - direction * offect;
        return vertices;
    }
}

