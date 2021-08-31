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
    MSlider speedSlider;

    static ModMesh mesh;
    ConfigurableJoint CJ;

    public override void SafeAwake()
    {
        forwardKey = AddKey("Forward", "Forward", KeyCode.UpArrow);
        backwardKey = AddKey("Backward", "Backward", KeyCode.DownArrow);
        speedSlider = AddSlider("Speed", "Speed", 1f, 0.1f, 3f);


        mesh = ModResource.GetMesh("wheel-obj");
        //Rigidbody.mass = 6f;

        CJ = GetComponent<ConfigurableJoint>();
    }

    public override void OnBlockPlaced()
    {
        //addCollider(new Vector3(0f, -1f, 0.35f));
        addColliders();

    }

    public override void OnSimulateStart()
    {

        CJ.axis = Vector3.forward;
        CJ.secondaryAxis = Vector3.up;
        CJ.angularXMotion = ConfigurableJointMotion.Free;
        //CJ.rotationDriveMode = RotationDriveMode.Slerp;

        //var sd = CJ.slerpDrive;
        //sd.maximumForce = 5000f;
        //CJ.slerpDrive = sd;
        var jd = CJ.angularXDrive;
        jd.maximumForce = 5000f;
        jd.positionDamper = 50f;
        CJ.angularXDrive = jd;
    }

    public override void SimulateUpdateAlways()
    {
        base.SimulateUpdateAlways();
        float input = 0f;
        if (forwardKey.IsHeld)
        {
            input = 1f;
        }

        if (backwardKey.IsHeld)
        {
            input = -1f;
        }


        CJ.targetAngularVelocity = Vector3.right * (Flipped ? -1f : 1f) * input * speedSlider.Value * 2f * 5f;
    }

    private void addColliders()
    {
        var boxs = new GameObject("Boxs");
        boxs.transform.SetParent(transform);
        boxs.transform.position = transform.position;
        boxs.transform.rotation = transform.rotation;

        var offect_forward = 0.5f;
        var origin = boxs.transform.localPosition + boxs.transform.forward * offect_forward;
        //圆半径、角度差和旋转角
        float radius = 1.45f, angle =24f;

        var positions = new Vector3[30];
        //外圈box位置
        for (var i = 0; i < 15; i++)
        {
            positions[i] = new Vector3(
                                                origin.x - radius * Mathf.Sin(angle * i * Mathf.Deg2Rad),
                                                origin.y + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad),
                                                offect_forward
                                             );

            addCollider(positions[i], 0f, 0.5f, 0.8f);
        }

        //addCollider(new Vector3(0f, -1f, 0.25f), 0f);

        void addCollider(Vector3 localPosition,float bounciness,float staticFriction,float dynamicFriction)
        {
            var go = new GameObject("box");
            go.transform.SetParent(boxs.transform);
            go.transform.position = boxs.transform.position;
            go.transform.rotation = boxs.transform.rotation;

            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.transform.LookAt(boxs.transform.position + boxs.transform.forward * localPosition.z /*new Vector3(0f, 0f, locationPosition.z)*/);
            go.transform.RotateAround(go.transform.position, go.transform.forward, (-Mathf.Sign( Vector3.Dot(transform.forward,go.transform.forward)) *Vector3.Angle(transform.forward, go.transform.right)));

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

            addJoint(Vector3.Scale(localPosition, Vector3.forward), radius, 5000f, 500f, 500f);

            void addJoint(Vector3 anchor, float _radius, float spring, float damper,float maxForce)
            {
                var cj = go.AddComponent<ConfigurableJoint>();
                cj.connectedBody = Rigidbody;
                cj.autoConfigureConnectedAnchor = false;

                //var anchor = new Vector3(0f, 0f, /*go.transform.*/localPosition.z/* * 0.5f*/);
                cj.connectedAnchor = anchor;
                cj.anchor = Vector3.zero;
                cj.axis = /*Vector3.Scale(Vector3.Normalize(boxs.transform.position - go.transform.position), new Vector3(1f, 1f, 0f));*/Vector3.forward;
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
                cj.enableCollision =true;
                cj.projectionMode = JointProjectionMode.PositionAndRotation;
                cj.projectionDistance = 0f;
                cj.projectionAngle = 1.5f;

                var rigi = go.GetComponent<Rigidbody>();
                rigi.useGravity = false;
                rigi.mass = 0.35f;
                rigi.angularDrag = rigi.drag = 0.01f * 0f;
                rigi.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        
        }
    }

  

}

