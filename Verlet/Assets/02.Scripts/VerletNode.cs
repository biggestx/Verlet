using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletNode : MonoBehaviour
{
    public Vector3 PrevPos;

    public Transform PinPoint;

    public List<VerletNode> Nodes = new List<VerletNode>();
    public List<int> NodeIndices = new List<int>();
}
