using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VerletSimulatorGPU : MonoBehaviour
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
    private ComputeShader CShader;

    private const int WIDTH = 20;
    private const int HEIGHT = 20;
    private const int BLOCK_SIZE = 256;
    private const int MAX_OBJECT_COUNT = 100;

    Vector3 PrevObjPos = Vector3.zero;

    private ComputeBuffer PositionBuffer;

    void Start()
    {
        Init();
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
                    var left = GetNode(GetIndex(j - 1, i));
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
                    var down = GetNode(GetIndex(j, i + 1));
                    if (down != null)
                        node.Nodes.Add(down);
                }


                Nodes.Add(node);
            }
        }

        PrevObjPos = this.transform.position;

        Nodes[GetIndex(0, 0)].PinPoint = PinPoints[0];
        Nodes[GetIndex(0, HEIGHT - 1)].PinPoint = PinPoints[1];
        Nodes[GetIndex(WIDTH - 1, 0)].PinPoint = PinPoints[2];
        Nodes[GetIndex(WIDTH - 1, HEIGHT - 1)].PinPoint = PinPoints[3];


    }



    private void InitBuffer()
    {
        PositionBuffer = new ComputeBuffer(MAX_OBJECT_COUNT, Marshal.SizeOf(typeof(Vector3)));
        var arr = new Vector3[MAX_OBJECT_COUNT];
        for (int i = 0; i < Nodes.Count; ++i)
        {
            arr[i] = Vector3.zero;
        }
    }

    private void Update()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var node in Nodes)
        {
            positions.Add(node.transform.position);
        }

        ComputeShader cs = CShader;


        int threadGroupSize = Nodes.Count / BLOCK_SIZE;

        var kernel = cs.FindKernel("ValidateDistance");
        cs.SetBuffer(kernel, "PositionsWrite", PositionBuffer);
        cs.Dispatch(kernel, threadGroupSize, 1, 1);

        Debug.LogError("aa");
    }

}
