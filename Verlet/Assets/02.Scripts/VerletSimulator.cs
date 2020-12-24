using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VerletSimulator : MonoBehaviour 
{

    [SerializeField]
    private VerletNode Res = null;

    [SerializeField]
    private List<VerletNode> Nodes = null;

    [SerializeField]
    private List<Transform> PinPoints = null;

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
    private bool ApplyGPU = false;

    private int WIDTH = 20;
    private int HEIGHT = 20;

    void Start()
    {
        Init();

        if(ApplyGPU == true)
            InitBuffer();
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

        StartPosition = PinPoints[0].position;

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
                    var idx = GetIndex(j - 1, i);
                    var left = GetNode(idx);
                    if (left != null)
                    {
                        node.Nodes.Add(left);
                        node.NodeIndices.Add(idx);
                    }
                }

                if (i != 0)
                {
                    var idx = GetIndex(j, i - 1);
                    var up = GetNode(GetIndex(j, i - 1));
                    if (up != null)
                    {
                        node.Nodes.Add(up);
                        node.NodeIndices.Add(idx);
                    }
                }

                if (i != WIDTH)
                {
                    var idx = GetIndex(j + 1, i);
                    var right = GetNode(idx);
                    if (right != null)
                    {
                        node.Nodes.Add(right);
                        node.NodeIndices.Add(idx);
                    }
                }

                if (j != HEIGHT)
                {
                    var idx = GetIndex(j, i + 1);
                    var down = GetNode(idx);
                    if (down != null)
                    {
                        node.Nodes.Add(down);
                        node.NodeIndices.Add(idx);
                    }
                }


                Nodes.Add(node);
            }
        }

        PrevObjPos = this.transform.position;

        Nodes[GetIndex(0, 0)].PinPoint = PinPoints[0];
        Nodes[GetIndex(0, HEIGHT- 1)].PinPoint = PinPoints[1];
        Nodes[GetIndex(WIDTH - 1, 0)].PinPoint = PinPoints[2];
        Nodes[GetIndex(WIDTH - 1, HEIGHT -1 )].PinPoint = PinPoints[3];


    }


    Vector3 PrevObjPos = Vector3.zero;
    private void ValidateDistance()
    {
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
        var nodeCount = Nodes.Count;
        for (int i = 0; i < nodeCount; ++i)
        {
            var node = Nodes[i];
            if (node.PinPoint != null)
                node.transform.position = node.PinPoint.position;

            foreach (var adj in node.Nodes)
                Calculate(node, adj);
        }

    }

    public void RemovePinPoint()
    {
        Nodes.ForEach(v => v.PinPoint = null);
    }

    private void Update()
    {
        if(ApplyGPU == false)
            UpdateCPU();
        else
            UpdateGPU();
    }



    private void UpdateCPU()
    {
        //ValidateDistance();
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
                    node.transform.position = newPos;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            RemovePinPoint();
        }

    }


    ////////////////////////// GPU


    [Serializable]
    public struct NodeData
    {
        public Vector3 PrevPos;
        public Vector3 CurPos;

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

    private void InitBuffer()
    {
        NodeDataBuffer = new ComputeBuffer(MAX_OBJECT_COUNT, Marshal.SizeOf(typeof(NodeData)));
        NodeDatas = new NodeData[MAX_OBJECT_COUNT];
        for (int i = 0; i < Nodes.Count; ++i)
        {
            var node = Nodes[i];
            var data = new NodeData();
            data.CurPos = node.transform.position;
            data.PrevPos = node.transform.position;

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
        List<Vector3> positions = new List<Vector3>();
        foreach (var node in Nodes)
        {
            positions.Add(node.transform.position);
        }

        ComputeShader cs = CShader;

        int threadGroupSize = Nodes.Count / BLOCK_SIZE;

        var kernel = cs.FindKernel("Simulate");
        cs.SetBuffer(kernel, "NodeDatas", NodeDataBuffer);
        cs.Dispatch(kernel, threadGroupSize, 1, 1);

        var nodedatas = new NodeData[MAX_OBJECT_COUNT];
        NodeDataBuffer.GetData(nodedatas);

        for (int i = 0; i < nodedatas.Length; ++i)
        {
            Nodes[i].transform.position = nodedatas[i].CurPos;
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
