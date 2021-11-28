using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Tyre : MonoBehaviour
{
    public bool Suspension { get; set; } = false;
    public float Radius { get; private set; }
    public float Stroke { get; set; } = 0.25f;

    [SerializeField]
    private GameObject tyre;
    [SerializeField]
    private TyreCollider[] boxes;
    [SerializeField]
    private Transform parent;
    [SerializeField]
    private Rigidbody connectedBody;
    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private MeshRenderer meshRenderer;
    public void CreateBoxes(float angle, float radius = 1.5f, float offset_forward = 0.5f)
    {
        this.Radius = radius;
        this.parent = transform;
        this.connectedBody = gameObject.GetComponent<Rigidbody>();

        tyre = new GameObject("Tyre");
        tyre.transform.SetParent(parent);
        tyre.transform.position = parent.position;
        tyre.transform.rotation = parent.rotation;

        int index = (int)(360f / angle);

        boxes = new TyreCollider[index];
        //外圈box位置
        for (var i = 0; i < index; i++)
        {
            var box = boxes[i] = new GameObject("Tyre Collider " + i).AddComponent<TyreCollider>();
            box.transform.SetParent(tyre.transform);
            box.CreateBox(angle * i, radius, offset_forward);
        }

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material.color = Color.green;
    }
    public void Setup(float spring,float damper,float maximumForce,float bounciness,float staticFriction,float dynamicFriction,float mass)
    {
        SetPhysicMaterail(bounciness,staticFriction,dynamicFriction);
        AddBoxesJoint();
        SetBoxesStroke(Stroke);
        SetBoxesJointDrive(spring,damper,maximumForce);
        SetBoxesBodyAttribute(mass);
    }
    private void SetPhysicMaterail(float bounciness = 0f, float staticFriction = 0.5f, float dynamicFriction = 0.8f, PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum, PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Minimum)
    {
        var index = boxes.Length;
        for (var i = 0; i < index; i++)
        {
            var mc = boxes[i].GetComponent<MeshCollider>();
            for (var j = 0; j < index; j++)
            {
                var mc1 = boxes[j].GetComponent<MeshCollider>();
                Physics.IgnoreCollision(mc, mc1);
            }
        }

        foreach (var box in boxes)
        {
            //var mc = box.GetComponent<MeshCollider>();
            //mc.material.bounciness = bounciness;
            //mc.material.staticFriction = staticFriction;
            //mc.material.dynamicFriction = dynamicFriction;
            //mc.material.frictionCombine = frictionCombine;
            //mc.material.bounceCombine = bounceCombine;
            box.SetPhysicMaterail(bounciness,staticFriction,dynamicFriction);
        }
    }
    private void AddBoxesJoint()
    {
        foreach (var box in boxes)
        {
            box.AddJoint();
        }
    }
    public void SetBoxesStroke(float stroke = 0.25f)
    {
        Stroke = stroke;

        foreach (var box in boxes)
        {
            box.SetStroke(stroke);
        }
    }
    public void SetBoxesJointDrive(float spring, float damper, float maximumForce)
    {
        foreach (var box in boxes)
        {
            box.SetJointDrive(spring, damper, maximumForce);
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
    public void IgnorBaseBlockCollider()
    {
        foreach (var col in connectedBody.gameObject.GetComponent<ConfigurableJoint>()?.connectedBody?.gameObject.GetComponentsInChildren<Collider>())
        {
            foreach (var box in boxes)
            {
                Physics.IgnoreCollision(box.GetComponent<MeshCollider>(), col);
            }
        }
    }
    public Vector3[] GetAllVertices()
    {
        var index = boxes.Length;
        var vectors = new Vector3[index * 2];

        var j = 0;
        for (var i = 0; i < index; i++)
        {
            vectors[j++] = transform./*parent.*/TransformPoint(boxes[i].GetVertices()[0]);
            vectors[j++] = transform./*parent.*/TransformPoint(boxes[i].GetVertices()[1]);
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
}


public class TyreCollider :MonoBehaviour
{
    private static ModMesh mesh = mesh ?? ModResource.GetMesh("wheel-obj");
    public float Stroke { get; private set; }
    public float Radius { get; private set; }
    [SerializeField]
    private ConfigurableJoint configurableJoint;
    [SerializeField]
    private Vector3 connectedAnchor;

    public void CreateBox(float angle, float radius, float offset_forward)
    {
        var parent = transform.parent;

        var xFactor = Mathf.Sin(angle * Mathf.Deg2Rad);
        var yFactor = Mathf.Cos(angle * Mathf.Deg2Rad);
        var vector = new Vector3(radius * xFactor, radius * yFactor, offset_forward);
        transform.localPosition = vector;
        transform.localScale *= 0.1f;

        var connectedAnchor = Vector3.forward * (offset_forward);
        transform.LookAt(parent.TransformPoint(connectedAnchor));

        var single = Vector3.Dot(parent.forward, transform.up);
        var angle1 = Vector3.Angle(parent.forward, transform.right);
        transform.Rotate(Vector3.forward * Mathf.Sign(single), angle1);

        var mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        mf.mesh = mc.sharedMesh = mesh;
        mc.convex = true;
#if DEBUG
        var mr = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        mr.material.color = Color.red;
#endif

        this.connectedAnchor = parent.InverseTransformDirection(transform.position - parent.position);

    }
    public void AddJoint()
    {
        addjoint( transform.parent.parent.GetComponent<Rigidbody>());
        SetJointAttribute();

        void addjoint(Rigidbody connectedBody)
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
    }
    public void SetStroke(float stroke )
    {
        //radius
        //set connected anchor
        var cj = GetComponent<ConfigurableJoint>();
        var vector = new Vector2(connectedAnchor.x, connectedAnchor.y);
        vector = Vector2.ClampMagnitude(vector, connectedAnchor.magnitude - stroke);
        var vector1 = new Vector3(vector.x, vector.y, connectedAnchor.z);
        cj.connectedAnchor = vector1;

        //stroke
        //set linear limit
        var _stroke = stroke;
        _stroke = _stroke * 0.5f;

        var softJointLimit = cj.linearLimit;
        softJointLimit.limit = _stroke;
        softJointLimit.contactDistance = _stroke;
        cj.linearLimit = softJointLimit;
        cj.targetPosition = new Vector3(_stroke, 0f, 0f);
    }
    //public void SetStroke(float stroke)
    //{
    //    Stroke = stroke;

    //    //radius
    //    //set connected anchor
    //    var cj = configurableJoint;
    //    var parent = cj.connectedBody.transform;
    //    var radius = Radius /** parent.localScale.x*/;
    //    var connectedAnchor = parent.InverseTransformDirection(gameObject.transform.position - parent.position);
    //    var single = 1f - (stroke * 0.5f) / radius;
    //    var single1 = 1f / parent.localScale.z;
    //    var vector = Vector3.Scale(new Vector3(single, single, single1), parent.parent.localScale);
    //    //cj.connectedAnchor = anchor ;
    //    cj.connectedAnchor = Vector3.Scale(vector, connectedAnchor);

    //    //stroke
    //    //set linear limit
    //    //var _stroke = stroke * gameObject.transform.parent.transform.localScale.x;
    //    var _stroke = stroke;
    //    _stroke = _stroke * 0.5f;

    //    var softJointLimit = configurableJoint.linearLimit;
    //    softJointLimit.limit = _stroke;
    //    softJointLimit.contactDistance = _stroke;
    //    configurableJoint.linearLimit = softJointLimit;
    //    configurableJoint.targetPosition = new Vector3(_stroke, 0f, 0f);
    //}
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
        var mc = GetComponent<MeshCollider>();
        if (mc == null) return;

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
#if COM //render center of mass
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

