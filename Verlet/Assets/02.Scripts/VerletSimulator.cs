using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VerletSimulator : MonoBehaviour 
{
    public enum SimulationType
    {
        kCPU,
        kGPU,
    }

    private VerletCPU Cpu = null;
    private VerletGPU Gpu = null;

    [SerializeField]
    private SimulationType TargetType = SimulationType.kCPU;

    private Verlet TargetVerlet = null;

    public VerletNode Res = null;
    public List<VerletNode> Nodes = null;

    public List<Transform> PinPoints = null;

    public Vector3 StartPosition = Vector3.zero;
    public float Distance = 0.5f;

    public Vector3 Gravity = new Vector3(0, -0.5f, 0);
    public int IterationCount = 80;
    public float Movement = 0.5f;
    public float DistanceLimitation = 1f;
    public bool ApplyGPU = false;

    public Vector3 PrevObjPos = Vector3.zero;

    public const int WIDTH = 20;
    public const int HEIGHT = 20;

    void Start()
    {
        Init();
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
        Cpu = this.gameObject.GetComponent<VerletCPU>();
        Gpu = this.gameObject.GetComponent<VerletGPU>();
        Cpu.Bind(this);
        Gpu.Bind(this);
        TargetVerlet = TargetType == SimulationType.kCPU ? (Verlet)Cpu : (Verlet)Gpu;

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


    private void Update()
    {
        if (TargetVerlet != null)
            TargetVerlet.UpdateVerlet();

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
