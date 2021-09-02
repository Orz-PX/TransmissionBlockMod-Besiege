using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;

class WheelBlockScript : BlockScript
{

    MKey forwardKey, backwardKey;
    MSlider speedSlider, springSlider, damperSlider;

  
    static ModMesh mesh;
    ConfigurableJoint CJ;
    float radius = 1.45f;

    public override void SafeAwake()
    {
        forwardKey = AddKey("Forward", "forward", KeyCode.UpArrow);
        backwardKey = AddKey("Backward", "backward", KeyCode.DownArrow);
        speedSlider = AddSlider("Speed", "speed", 1f, 0.1f, 3f);
        springSlider = AddSlider("Spring", "Spring", 1f, 0.1f, 50f);
        damperSlider = AddSlider("Damper", "Damper", 1f, 0.1f, 50f);

        mesh = ModResource.GetMesh("wheel-obj");
        Rigidbody.mass = 0.5f;

        CJ = GetComponent<ConfigurableJoint>();
    }

    public override void OnBlockPlaced()
    {
        AddColliders();
        SetCollidersState(false);
    }

    private Vector3 lastScale = Vector3.one;
    private event Action<Vector3,Vector3> onScale;
    public override void BuildingUpdate()
    {
        if (transform.localScale != lastScale)
        {
         
            onScale?.Invoke(transform.localScale,lastScale);
            setScale(transform.localScale,lastScale);

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
            transform.localScale = new Vector3(single, single, currentScale.z);
            setBoxesRadius(radius * single);
        }
    }

    public override void OnSimulateStart()
    {
        SetCollidersState(true);
        addDynamicAxis();
        RefreshColliders();
       

        void addDynamicAxis()
        {
            CJ.axis = Vector3.forward;
            CJ.secondaryAxis = Vector3.up;
            CJ.angularXMotion = ConfigurableJointMotion.Free;
            //CJ.autoConfigureConnectedAnchor = false;
            //CJ.connectedAnchor = Vector3.forward * -0.5f;
            //CJ.anchor = Vector3.zero;

            var jd = CJ.angularXDrive;
            jd.maximumForce = 5000f;
            jd.positionDamper = 50f;
            CJ.angularXDrive = jd;
        }

    }

    public override void OnSimulateStop()
    {
        base.OnSimulateStop();
        SetCollidersState(false);
    }

    public override void SimulateUpdateAlways()
    {

        float input = 0f;
        if (forwardKey.IsHeld)
        {
            input = 1f;
            Rigidbody.WakeUp();
        }

        if (backwardKey.IsHeld)
        {
            input = -1f;
            Rigidbody.WakeUp();
        }

        CJ.targetAngularVelocity = Vector3.right * (Flipped ? -1f : 1f) * input * speedSlider.Value * 2f * 5f;

        var go = transform.FindChild("Boxes");
        var index = go.childCount;
        for (int i = 0; i < index; i++)
        {
            go.GetChild(i).Rotate(Vector3.forward, 10f * Time.deltaTime);
        }
    }

    private void SetCollidersState(bool enabled)
    {
        Transform boxes = transform.FindChild("Boxes");
        if (boxes == null) return;
        foreach (Transform child in boxes)
        {
            var rb = child.gameObject.GetComponent<Rigidbody>();
            if (rb == null) continue;
            rb.detectCollisions = enabled;
            rb.isKinematic = !enabled;
        }
    }

    private void RefreshColliders()
    {
        Transform boxes = transform.FindChild("Boxes");
        if (boxes == null) return;
        foreach (Transform child in boxes)
        {
            var cj = child.gameObject.GetComponent<ConfigurableJoint>();
            if (cj == null) continue;
            var jointDrive = cj.xDrive;
            jointDrive.positionSpring = springSlider.Value * 100;
            jointDrive.positionDamper = damperSlider.Value * 10;
            cj.xDrive = jointDrive;
        }
    }

    private void setBoxesRadius(float radius)
    {

        var go = transform.FindChild("Boxes");
        if (go == null) return;
        var index = go.childCount;
        for (int i = 0; i < index; i++)
        {
            var cj = go.GetChild(i).gameObject.GetComponent<ConfigurableJoint>();
            if (cj == null) continue;

            var softJointLimit = cj.linearLimit;
            softJointLimit.limit = radius;
            cj.linearLimit = softJointLimit;

            cj.targetPosition = new Vector3(radius, 0f, 0f);
        }
    }

    private void AddColliders()
    {
        var Boxes = new GameObject("Boxes");
        Boxes.transform.SetParent(transform);
        Boxes.transform.position = transform.position;
        Boxes.transform.rotation = transform.rotation;

        var offect_forward = 0.5f;
        var origin = Boxes.transform.localPosition;
        //圆半径和旋转角
        float radius = this.radius, angle = 24f;

        var positions = new Vector3[30];
        //外圈box位置
        for (var i = 0; i < 15; i++)
        {
            positions[i] = new Vector3(
                                                origin.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad),
                                                origin.x - radius * Mathf.Cos(angle * i * Mathf.Deg2Rad),
                                                offect_forward
                                             );

            AddCollider(positions[i], 0f, 0.5f, 0.8f);
        }

        void AddCollider(Vector3 localPosition, float bounciness, float staticFriction, float dynamicFriction)
        {
            var go = new GameObject("box");
            go.transform.SetParent(Boxes.transform);
            go.transform.position = Boxes.transform.position;
            go.transform.rotation = Boxes.transform.rotation;

            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.transform.LookAt(transform.TransformPoint (Boxes.transform.localPosition + Vector3.forward * offect_forward));

            var single = Vector3.Dot(transform.forward, go.transform.up);
            var _angle = Vector3.Angle(transform.forward, go.transform.right);
            go.transform.Rotate(Vector3.forward * Mathf.Sign(single), _angle);

            var mf = go.AddComponent<MeshFilter>() ?? go.GetComponent<MeshFilter>();
            var mc = go.AddComponent<MeshCollider>() ?? go.GetComponent<MeshCollider>();
            mf.mesh = mc.sharedMesh = mesh;
            mc.convex = true;
            mc.material.staticFriction = staticFriction;
            mc.material.dynamicFriction = dynamicFriction;
            mc.material.bounciness = bounciness;
            mc.material.frictionCombine = PhysicMaterialCombine.Maximum;
#if DEBUG
            var mr = go.AddComponent<MeshRenderer>() ?? go.GetComponent<MeshRenderer>();
            mr.material.color = Color.red;
#endif

            AddJoint(Vector3.forward * offect_forward, radius, springSlider.Value * 100f, damperSlider.Value * 10f, 500f);

            void AddJoint(Vector3 anchor, float _radius, float spring, float damper, float maxForce)
            {
                var cj = go.AddComponent<ConfigurableJoint>();
                cj.connectedBody = Rigidbody;
                cj.autoConfigureConnectedAnchor = false;

                cj.connectedAnchor = anchor;
                cj.anchor = Vector3.zero;
                cj.axis = Vector3.forward;
                cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = cj.zMotion = cj.yMotion = ConfigurableJointMotion.Locked;
                cj.xMotion = ConfigurableJointMotion.Limited;
                var softJointLimit = cj.linearLimit;
                softJointLimit.limit = _radius;
                cj.linearLimit = softJointLimit;

                var jointDrive = cj.xDrive;
                jointDrive.positionSpring = spring;
                jointDrive.positionDamper = damper;
                jointDrive.maximumForce = maxForce;
                cj.xDrive = jointDrive;

                cj.targetPosition = new Vector3(_radius, 0f, 0f);
                cj.enablePreprocessing = false;
                cj.enableCollision = true;
                cj.projectionMode = JointProjectionMode.PositionAndRotation;
                cj.projectionDistance = 0f;
                cj.projectionAngle = 1.5f;

                var rb = go.GetComponent<Rigidbody>();
                rb.useGravity = true;
                rb.mass = 0.35f;
                rb.angularDrag = rb.drag = 0.01f * 0f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

        }
    }



}

