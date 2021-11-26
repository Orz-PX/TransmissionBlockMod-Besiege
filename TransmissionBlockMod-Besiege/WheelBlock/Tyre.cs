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

    private static ModMesh mesh = mesh ?? ModResource.GetMesh("wheel-obj");

    private GameObject tyre;
    public GameObject[] boxes;
    public Transform parent;
    public Rigidbody connectedBody;

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

        boxes = new GameObject[index];
        //外圈box位置
        for (var i = 0; i < index; i++)
        {
            boxes[i] = createBox(angle * i, radius, offset_forward);
        }

        for (var i = 0; i < index; i++)
        {
            var mc = boxes[i].GetComponent<MeshCollider>();
            for (var j = 0; j < index; j++)
            {
                var mc1 = boxes[j].GetComponent<MeshCollider>();
                Physics.IgnoreCollision(mc, mc1);
            }
        }

        GameObject createBox(float _angle, float _radius, float _offset_forward)
        {
            var box = new GameObject("box");
            var _parent = tyre.transform;
            box.transform.SetParent(_parent);

            var xFactor = Mathf.Sin(_angle * Mathf.Deg2Rad);
            var yFactor = Mathf.Cos(_angle * Mathf.Deg2Rad);
            var vector = new Vector3(_radius * xFactor, _radius * yFactor, _offset_forward);
            box.transform.localPosition = vector;
            box.transform.localScale *= 0.1f;

            var connectedAnchor = Vector3.forward * (_offset_forward);
            box.transform.LookAt(_parent.TransformPoint(connectedAnchor));

            var single = Vector3.Dot(_parent.forward, box.transform.up);
            var _angle1 = Vector3.Angle(_parent.forward, box.transform.right);
            box.transform.Rotate(Vector3.forward * Mathf.Sign(single), _angle1);

            var mf = box.AddComponent<MeshFilter>() ?? box.GetComponent<MeshFilter>();
            var mc = box.AddComponent<MeshCollider>() ?? box.GetComponent<MeshCollider>();
            mf.mesh = mc.sharedMesh = mesh;
            mc.convex = true;
#if DEBUG
            var mr = box.AddComponent<MeshRenderer>() ?? box.GetComponent<MeshRenderer>();
            mr.material.color = Color.red;
#endif

            return box;
        }
    }

    public void SetPhysicMaterail(float bounciness = 0f, float staticFriction = 0.5f, float dynamicFriction = 0.8f, PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum, PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Minimum)
    {
        foreach (var box in boxes)
        {
            var mc = box.GetComponent<MeshCollider>();
            mc.material.bounciness = bounciness;
            mc.material.staticFriction = staticFriction;
            mc.material.dynamicFriction = dynamicFriction;
            mc.material.frictionCombine = frictionCombine;
            mc.material.bounceCombine = bounceCombine;
        }
    }

    public void AddJoint()
    {
        foreach (var box in boxes)
        {
            var cj = box.AddComponent<ConfigurableJoint>();
            cj.autoConfigureConnectedAnchor = false;
            cj.connectedBody = connectedBody;
            cj.enablePreprocessing = false;
            cj.anchor = Vector3.zero;
            cj.axis = Vector3.forward;
            cj.xMotion = ConfigurableJointMotion.Limited;
            cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = cj.zMotion = cj.yMotion = ConfigurableJointMotion.Locked;
        }
    }

    public void SetStroke(float stroke = 0.25f)
    {
        Stroke = stroke;

        foreach (var box in boxes)
        {
            //radius
            //set connected anchor
            var cj = box.GetComponent<ConfigurableJoint>();
            var radius = Radius;
            var connectedAnchor = parent.InverseTransformDirection(box.transform.position - parent.position);
            Debug.Log(connectedAnchor);
            var single = 1f - (stroke * 0.5f) / radius;
            //var single1 = 1f / parent.localScale.z;
            var distance = Vector3.Distance(box.transform.position, parent.position);
            var vector = new Vector3(single * distance, single * distance, 1f);
            //cj.connectedAnchor = Vector3.Scale(vector, connectedAnchor);
            cj.connectedAnchor = connectedAnchor;

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
    }
}



