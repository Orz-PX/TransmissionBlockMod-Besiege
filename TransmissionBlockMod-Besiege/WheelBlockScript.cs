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

        Debug.Log(transform.InverseTransformVector(transform.forward));
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

        Debug.Log(Vector3.Angle(CJ.axis, transform.forward));
    

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

        var go = transform.FindChild("Boxs");
        var index = go.childCount;
        for (int i = 0; i < index; i++)
        {
            go.GetChild(i).Rotate(Vector3.forward, 10f * Time.deltaTime);
        }
       Debug.Log(Vector3.Dot(transform.right, go.GetChild(0).transform.right) + "  " + Vector3.Angle(transform.forward, go.GetChild(0).transform.right));
    }

    private void SetCollidersState(bool enabled)
    {
        Transform boxes = transform.FindChild("Boxs");
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
        Transform boxes = transform.FindChild("Boxs");
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

    private void AddColliders()
    {
        var boxs = new GameObject("Boxs");
        boxs.transform.SetParent(transform);
        boxs.transform.position = transform.position;
        boxs.transform.rotation = transform.rotation;

        var offect_forward = 0.5f;
        var origin = boxs.transform.localPosition;
        //圆半径和旋转角
        float radius = 1.45f, angle = 24f;

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
            go.transform.SetParent(boxs.transform);
            go.transform.position = boxs.transform.position;
            go.transform.rotation = boxs.transform.rotation;

            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.transform.LookAt(transform.TransformPoint (boxs.transform.localPosition + Vector3.forward * offect_forward));

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

