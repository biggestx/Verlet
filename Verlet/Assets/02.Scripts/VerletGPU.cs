using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VerletGPU : Verlet
{
    public struct NodeData
    {
        public Vector3 PrevPos;
        public Vector3 CurPos;

        public int HasPinPoint;
        public Vector3 PinPoint;

        public int Node0;
        public int Node1;
        public int Node2;
        public int Node3;


    }

    [SerializeField]
    private ComputeShader CShader;

    private const int BLOCK_SIZE = 256;
    private const int MAX_OBJECT_COUNT = 400;

    private NodeData[] NodeDatas;
    private ComputeBuffer NodeDataBuffer;



    public override void Init()
    {
        NodeDataBuffer = new ComputeBuffer(MAX_OBJECT_COUNT, Marshal.SizeOf(typeof(NodeData)));
        NodeDatas = new NodeData[MAX_OBJECT_COUNT];
        for (int i = 0; i < Parameters.Nodes.Count; ++i)
        {
            var node = Parameters.Nodes[i];
            var data = new NodeData();
            data.CurPos = node.transform.position;
            data.PrevPos = node.transform.position;

            if (node.PinPoint != null)
                data.PinPoint = node.PinPoint.transform.position;
            data.HasPinPoint = data.PinPoint == null ? 0 : 1;

            data.Node0 = -1;
            data.Node1 = -1;
            data.Node2 = -1;
            data.Node3 = -1;

            if (node.NodeIndices.Count > 1)
                data.Node0 = node.NodeIndices[0];

            if (node.NodeIndices.Count > 2)
                data.Node1 = node.NodeIndices[1];

            if (node.NodeIndices.Count > 3)
                data.Node2 = node.NodeIndices[2];

            if (node.NodeIndices.Count > 4)
                data.Node3 = node.NodeIndices[3];

            NodeDatas[i] = data;
        }
        NodeDataBuffer.SetData(NodeDatas);

    }
    private void UpdateGPU()
    {

        ComputeShader cs = CShader;
        cs.SetInt("NodeCount", Parameters.Nodes.Count);
        cs.SetFloat("Distance", Parameters.Distance);
        cs.SetFloat("Movement", Parameters.Movement);
        cs.SetFloat("DeltaTime", Time.deltaTime);
        cs.SetVector("Gravity", Parameters.Gravity);
        cs.SetInt("IterationCount", Parameters.IterationCount);

        int threadGroupSize = Parameters.Nodes.Count / BLOCK_SIZE;

        NodeDataBuffer.SetData(NodeDatas);

        var simulate = cs.FindKernel("Simulate");
        cs.SetBuffer(simulate, "NodeDatas", NodeDataBuffer);
        cs.Dispatch(simulate, threadGroupSize, 1, 1);

        var nodedatas = new NodeData[MAX_OBJECT_COUNT];

        var applyConstraint = cs.FindKernel("ApplyConstraint");
        cs.SetBuffer(applyConstraint, "NodeDatas", NodeDataBuffer);
        cs.Dispatch(applyConstraint, BLOCK_SIZE, 1, 1);

        NodeDataBuffer.GetData(nodedatas);

        for (int i = 0; i < nodedatas.Length; ++i)
        {
            //if(float.IsNaN(nodedatas[i].CurPos.x) == false &&
            //    float.IsNaN(nodedatas[i].CurPos.y) == false &&
            //    float.IsNaN(nodedatas[i].CurPos.z) == false)
            Parameters.Nodes[i].transform.position = nodedatas[i].CurPos;
        }

        NodeDatas = nodedatas;
    }

}
