using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletCPU : Verlet
{

    private void ValidateDistance()
    {
        var dist = transform.position - Parameters.PrevObjPos;
        if (dist.magnitude > Parameters.DistanceLimitation)
        {
            foreach (var node in Parameters.Nodes)
            {
                node.transform.position += dist;
                node.PrevPos += dist;
            }
        }

        Parameters.PrevObjPos = this.transform.position;
    }


    private void Simulate()
    {
        for (int i = 0; i < Parameters.Nodes.Count; ++i)
        {
            var node = Parameters.Nodes[i];

            Vector3 velocity = node.transform.position - node.PrevPos;
            node.PrevPos = node.transform.position;

            Vector3 newPos = node.transform.position + velocity;
            newPos += Parameters.Gravity * Time.deltaTime;

            Vector3 dir = node.transform.position - newPos;

            node.transform.position = newPos;
        }
    }

    private void Calculate(VerletNode a, VerletNode b)
    {
        var node1 = a;
        var node2 = b;

        float curDistance = (node1.transform.position - node2.transform.position).magnitude;
        float diff = Mathf.Abs(curDistance - Parameters.Distance);

        var dir = Vector3.zero;

        if (curDistance > Parameters.Distance)
            dir = (node1.transform.position - node2.transform.position).normalized;
        else if (curDistance < Parameters.Distance)
            dir = (node2.transform.position - node1.transform.position).normalized;

        Vector3 movement = dir * diff;

        node1.transform.position -= (movement * Parameters.Movement);
        node2.transform.position += (movement * Parameters.Movement);
    }

    private void ApplyConstraint()
    {
        var nodeCount = Parameters.Nodes.Count;
        for (int i = 0; i < nodeCount; ++i)
        {
            var node = Parameters.Nodes[i];
            if (node.PinPoint != null)
                node.transform.position = node.PinPoint.position;

            foreach (var adj in node.Nodes)
                Calculate(node, adj);
        }

    }


    public void RemovePinPoint()
    {
        Parameters.Nodes.ForEach(v => v.PinPoint = null);
    }

    public override void UpdateVerlet()
    {
        ValidateDistance();
        Simulate();

        for (int i = 0; i < Parameters.IterationCount; ++i)
        {
            ApplyConstraint();

            if (i % 2 == 1)
            {
                for (int j = 0; j < Parameters.Nodes.Count; ++j)
                {
                    var node = Parameters.Nodes[j];

                    var newPos = node.transform.position;
                    node.transform.position = newPos;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            RemovePinPoint();
        }


    }



}
