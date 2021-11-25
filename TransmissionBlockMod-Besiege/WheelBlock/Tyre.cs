using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Tyre : MonoBehaviour
{
    public float Radius { get; private set; }
    public float Stroke { get; set; } = 0.25f;

    private static ModMesh mesh;

    private GameObject tyre;
    private GameObject[] boxes;
    private Transform parent;
    private Rigidbody connectedBody;

    public void CreateBoxes(float angle, float radius = 1.5f, float offset_forward = 0.5f)
    {
        this.Radius = radius;
        this.parent = transform.parent;
        this.connectedBody = gameObject.GetComponent<Rigidbody>();

        tyre = new GameObject("Boxes");
        tyre.transform.SetParent(parent);
        tyre.transform.position = parent.position;
        tyre.transform.rotation = parent.rotation;

        int index = (int)(360f / angle);

        boxes = new GameObject[index];
        //外圈box位置
        for (var i = 0; i < index; i++)
        {
            // boxes[i] = new Box(tyre.transform, connectedBody, angle * i, offset_forward, Radius, Stroke);
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

        GameObject createBox(float _angle, float _radius,float _offset_forward)
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
            mf.mesh = mc.sharedMesh = mesh = mesh ?? ModResource.GetMesh("wheel-obj");
            mc.convex = true;
#if DEBUG
            var mr = box.AddComponent<MeshRenderer>() ?? box.GetComponent<MeshRenderer>();
            mr.material.color = Color.red;
#endif

            return box;
        }
    }

}

