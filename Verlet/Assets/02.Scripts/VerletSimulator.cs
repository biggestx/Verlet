using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletSimulator : MonoBehaviour 
{

    [SerializeField]
    private VerletNode Res = null;

    [SerializeField]
    private List<VerletNode> Nodes = null;

    [SerializeField]
    private Transform PinPoint = null;

    [SerializeField]
    private Transform PinPoint2 = null;

    [SerializeField]
    private Transform PinPoint3 = null;

    [SerializeField]
    private Transform PinPoint4 = null;

    [SerializeField]
    private Transform PinPoint5 = null;

    [SerializeField]
    private Vector3 StartPosition = Vector3.zero;

    [SerializeField]
    private float Distance = 0.5f;

    [SerializeField]
    private Vector3 Gravity = new Vector3(0, -0.5f, 0);

    [SerializeField]
    private int IterationCount = 80;

    [SerializeField]
    private float Movement = 0.5f;

    [SerializeField]
    private float DistanceLimitation = 1f;

    [SerializeField]
    private Transform ConstraintTarget = null;

    private int WIDTH = 5;
    private int HEIGHT = 5;

    void Start()
    {
        Init();
    }

    private void InitOrigin()
    {
        StartPosition = PinPoint.position;
        foreach (var node in Nodes)
        {
            node.transform.SetParent(null);
            node.transform.position = StartPosition;
            node.PrevPos = StartPosition;

            StartPosition.y -= Distance;
        }

        PrevObjPos = this.transform.position;
    }

    private VerletNode GetNode(int index)
    {
        if (index < 0 ||
            index > Nodes.Count - 1)
        {
            return null;
        }

        return Nodes[index];
    }

    private int GetIndex(int x, int y)
    {
        return HEIGHT * y + x;
    }


    private void Init()
    {

        StartPosition = PinPoint.position;

        for (int i = 0; i < HEIGHT; ++i)
        {
            for (int j = 0; j < WIDTH; ++j)
            {
                var index = HEIGHT * i + j;

                
                var node = Instantiate(Res);
                node.transform.position = StartPosition;
                node.PrevPos = StartPosition;

                if (j != 0)
                {
                    var left = GetNode(GetIndex(j-1,i));
                    if (left != null)
                        node.Nodes.Add(left);
                }

                if (i != 0)
                {
                    var up = GetNode(GetIndex(j, i - 1));
                    if (up != null)
                        node.Nodes.Add(up);
                }

                if (i != WIDTH)
                {
                    var right = GetNode(GetIndex(j + 1, i));
                    if (right != null)
                        node.Nodes.Add(right);
                }

                if (j != HEIGHT)
                {
                    var down = GetNode(GetIndex(j, i +1));
                    if (down != null)
                        node.Nodes.Add(down);
                }


                Nodes.Add(node);
            }
        }

        PrevObjPos = this.transform.position;

        Nodes[0].PinPoint = PinPoint;
        Nodes[1].PinPoint = PinPoint2;
        Nodes[2].PinPoint = PinPoint3;
        Nodes[3].PinPoint = PinPoint4;
        Nodes[4].PinPoint = PinPoint5;

    }


    Vector3 PrevObjPos = Vector3.zero;
    private void ValidateDistance()
    {
        var curPos = transform.position;

        var dist = transform.position - PrevObjPos;
        if (dist.magnitude > DistanceLimitation)
        {
            foreach (var node in Nodes)
            {
                node.transform.position += dist;
                node.PrevPos += dist;
            }
        }

        PrevObjPos = this.transform.position;
    }


    private void Simulate()
    {
        for(int i=0;i < Nodes.Count; ++i)
        {
            var node = Nodes[i];

            Vector3 velocity = node.transform.position - node.PrevPos;
            node.PrevPos = node.transform.position;

            Vector3 newPos = node.transform.position + velocity;
            newPos += Gravity * Time.deltaTime;

            Vector3 dir = node.transform.position - newPos;

            if (ConstraintTarget != null)
            {
                if (newPos.y < ConstraintTarget.position.y)
                    newPos.y = ConstraintTarget.position.y;
            }

            node.transform.position = newPos;
        }
    }

    private void Calculate(VerletNode a, VerletNode b)
    {
        var node1 = a;
        var node2 = b;

        float curDistance = (node1.transform.position - node2.transform.position).magnitude;
        float diff = Mathf.Abs(curDistance - Distance);

        var dir = Vector3.zero;

        if (curDistance > Distance)
            dir = (node1.transform.position - node2.transform.position).normalized;
        else if (curDistance < Distance)
            dir = (node2.transform.position - node1.transform.position).normalized;

        Vector3 movement = dir * diff;

        node1.transform.position -= (movement * Movement);
        node2.transform.position += (movement * Movement);
    }

    private void ApplyConstraint()
    {

        for (int i = 0; i < WIDTH; ++i)
        {
            Nodes[i].transform.position = PinPoint.position + new Vector3(i,0,0);
        }

        

        var nodeCount = Nodes.Count;
        for (int i = 0; i < nodeCount; ++i)
        {

            var node = Nodes[i];
            if (node.PinPoint != null)
            {
                node.transform.position = node.PinPoint.position;
                continue;
            }

            foreach (var adj in node.Nodes)
            {
                Calculate(node, adj);
            }

            
            //var node1 = Nodes[i];
            //var node2 = Nodes[i + 1];

            //float curDistance = (node1.transform.position - node2.transform.position).magnitude;
            //float diff = Mathf.Abs(curDistance - Distance);

            //var dir = Vector3.zero;

            //if (curDistance > Distance)
            //    dir = (node1.transform.position - node2.transform.position).normalized;
            //else if (curDistance < Distance)
            //    dir = (node2.transform.position - node1.transform.position).normalized;


            //Vector3 movement = dir * diff;

            //node1.transform.position -= (movement * Movement);
            //node2.transform.position += (movement * Movement);
        }

    }


    void Update()
    {
        ValidateDistance();
        Simulate();

        for (int i = 0; i < IterationCount; ++i)
        {
            ApplyConstraint();

            if (i % 2 == 1)
            {
                for (int j = 0; j < Nodes.Count; ++j)
                {
                    var node = Nodes[j];

                    var newPos = node.transform.position;

                    if (ConstraintTarget != null)
                    {
                        if (newPos.y < ConstraintTarget.position.y)
                            newPos.y = ConstraintTarget.position.y;
                    }

                    node.transform.position = newPos;
                }
            }
        }

    }


    void OnDrawGizmos()
    {
        foreach (var node in Nodes)
        {
            foreach (var n in node.Nodes)
            {
                Gizmos.DrawLine(node.transform.position, n.transform.position);
            }
        }
    }

}
