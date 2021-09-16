﻿using System;
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
    private MSlider speedSlider, springSlider, damperSlider, acceleratedSlider, staticFrictionSlider, dynamicFrictionSlider, bouncinessSlider,massSlider;
    private MToggle ignoreBaseColliderToggle, toggleToggle;

    private ConfigurableJoint CJ;
    private Vector3 lastScale;

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

        lastScale = transform.localScale;

        Rigidbody.inertiaTensorRotation = new Quaternion(0, 0, 0.4f, 0.9f);
        Rigidbody.inertiaTensor = new Vector3(0.4f, 0.4f, 0.7f);
        Rigidbody.drag = Rigidbody.angularDrag = 0f;

        Rigidbody.solverIterations = 100;
        Rigidbody.mass =1f;
        
        CJ = GetComponent<ConfigurableJoint>();
        CJ.breakForce = CJ.breakTorque = Mathf.Infinity;
    }

    public override void OnBlockPlaced()
    {
        //if (!transform.FindChild("Boxes"))
        //{
        //    Boxes = new Boxes(transform, Rigidbody);
        //    Boxes.SetBoxesColliderState(false);
        //}
    }

    private event Action<Vector3,Vector3> onScale;
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
            var single = 0f;
            if (currentScale.x != lastScale.x)
            {
                single = currentScale.x;
            }
            else if (currentScale.y != lastScale.y)
            {
                single = currentScale.y;
            }
            else
            {
                return;
            }
            transform.localScale = new Vector3(single, single, currentScale.z);
            Boxes.SetRadius(Boxes.Radius * single);
        }
    }

    public override void OnSimulateStart()
    {
        Rigidbody.maxAngularVelocity = 50f * speedSlider.Value;

        Destroy(transform.FindChild("Boxes")?.gameObject);
        Boxes = new Boxes(transform,Rigidbody);
        Boxes.RefreshBoxesCollider(springSlider.Value * 500f, damperSlider.Value * 250f, 5000f * springSlider.Value);
        Boxes.SetBoxesPhysicMaterail(bouncinessSlider.Value, staticFrictionSlider.Value, dynamicFrictionSlider.Value);
        Boxes.SetBoxesBodyAttribute(massSlider.Value);
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
            Boxes.IgnorBaseBlockCollider();
        }
    }

    float input = 0f,single = 0f,single1 = 0f;
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
        //foreach (var box in Boxes.boxes)
        //{
        //    if (!box.rigidbody.useGravity)
        //    {
        //        box.rigidbody.AddForce(Vector3.down * 10f, ForceMode.Force);
        //    }
        //}
    }
}
class Boxes
{
    public GameObject gameObject;
    public Box[] boxes;
    public float Radius { get; set; } = 1.45f;

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
        gameObject.transform.localScale = parent.localScale;

        var offect_forward = 0.5f;
        var origin = gameObject.transform.localPosition;
        var anchor = Vector3.forward * offect_forward;
        //圆半径和旋转角
        float radius = Radius / gameObject.transform.localScale.x;
        float angle = 18f;
        int index = (int)(360f / angle);

        boxes = new Box[index];
        //外圈box位置
        for (var i = 0; i < index; i++)
        {
            var position = new Vector3(
                                                origin.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad),
                                                origin.x - radius * Mathf.Cos(angle * i * Mathf.Deg2Rad),
                                                offect_forward / gameObject.transform.localScale.z
                                             );

            boxes[i] = new Box(gameObject.transform, position, anchor, connectedBody, Radius);
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
    public void SetRadius(float radius)
    {
        Radius = radius;

        for (int i = 0; i < boxes.Length; i++)
        {
            boxes[i].SetRadius(radius);
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
    public void RefreshBoxesCollider(float spring,float damper,float maximumForce)
    {
        foreach (var box in boxes)
        {
            box.SetJointDrive(spring, damper, maximumForce);
        }
    }
    public void SetBoxesPhysicMaterail(float bounciness,float staticFriction,float dynamicFriction)
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
    public Vector3[] GetAllVertices()
    {
        var index = boxes.Length;
        var vectors = new Vector3[index*2];

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
        var tris = new int[(index-2) * 3];
        var j = 0;
        for (var i = 0; i<index; i++)
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
        foreach (var col in connectedBody.gameObject.GetComponent<ConfigurableJoint>().connectedBody?.gameObject.GetComponentsInChildren<Collider>())
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
    public float Radius { get; set; } 
    public Box(Transform parent, Vector3 localPosition, Vector3 connectedAnchor,Rigidbody connectedBody,float radius)
    {
        gameObject = new GameObject("box");
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = parent.position;
        gameObject.transform.rotation = parent.rotation;

        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = new Vector3(0.1f / parent.localScale.z, 0.1f / parent.localScale.x, 0.1f / parent.localScale.x);
        gameObject.transform.LookAt(parent.parent.TransformPoint(parent.localPosition + connectedAnchor));

        var single = Vector3.Dot(parent.forward, gameObject.transform.up);
        var _angle = Vector3.Angle(parent.forward, gameObject.transform.right);
        gameObject.transform.Rotate(Vector3.forward * Mathf.Sign(single), _angle);

        var mf = gameObject.AddComponent<MeshFilter>() ?? gameObject.GetComponent<MeshFilter>();
#if DEBUG
        var mr = gameObject.AddComponent<MeshRenderer>() ?? gameObject.GetComponent<MeshRenderer>();
        mr.material.color = Color.red;
#endif
        var mc = meshCollider = gameObject.AddComponent<MeshCollider>() ?? gameObject.GetComponent<MeshCollider>();
        mf.mesh = mc.sharedMesh = mesh = mesh ?? ModResource.GetMesh("wheel-obj");
        mc.convex = true;

        SetPhysicMaterail();

        addJoint(connectedAnchor, connectedBody);
        SetRadius(radius);
        SetJointDrive();
        SetJointAttribute();
        SetBodyAttribute();
    }

    private void addJoint(Vector3 anchor , Rigidbody connectedBody)
    {
        var cj = configurableJoint = gameObject.AddComponent<ConfigurableJoint>();
        cj.autoConfigureConnectedAnchor = false;
        cj.connectedBody =connectedBody;
        cj.connectedAnchor = anchor ;
        cj.anchor = Vector3.zero;
        cj.axis = Vector3.forward;
        cj.xMotion = ConfigurableJointMotion.Limited;
        cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = cj.zMotion = cj.yMotion = ConfigurableJointMotion.Locked;
    }

    public void SetRadius(float radius)
    {
        Radius = radius;
        var _radius = radius * gameObject.transform.parent.transform.localScale.x;

        var softJointLimit = configurableJoint.linearLimit;
        softJointLimit.limit = _radius;
        configurableJoint.linearLimit = softJointLimit;

        configurableJoint.targetPosition = new Vector3(_radius, 0f, 0f);
    }

    public void SetJointDrive(float spring = 400f, float damper = 50f, float maximumForce = 500f)
    {
        var jointDrive = configurableJoint.xDrive;
        jointDrive.positionSpring = spring;
        jointDrive.positionDamper = damper;
        jointDrive.maximumForce = maximumForce;
        configurableJoint.xDrive = jointDrive;
    }
    public void SetJointAttribute(float breakForce = Mathf.Infinity,float breakTorque = Mathf.Infinity, bool enableCollision = false,bool enablePreprocessing = false,JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation,float projectionDistance = 0.001f,float projectionAngle = 3f)
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
    public void SetPhysicMaterail(float bounciness = 0f, float staticFriction = 0.5f, float dynamicFriction = 0.8f, PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum,PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Minimum)
    {
        var mc = meshCollider;
        mc.material.bounciness = bounciness;
        mc.material.staticFriction = staticFriction;
        mc.material.dynamicFriction = dynamicFriction;
        mc.material.frictionCombine = frictionCombine;
        mc.material.bounceCombine = bounceCombine;
    }
    public void SetBodyAttribute(bool useGravity = true, float mass = 0.25f, float drag = 0f, float angularDrag = 0f,CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Continuous)
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        rb.useGravity = useGravity;
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag =angularDrag;
        rb.collisionDetectionMode = collisionDetectionMode;
        //rb.maxAngularVelocity = 50f;
        rb.solverIterations = 200;
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
