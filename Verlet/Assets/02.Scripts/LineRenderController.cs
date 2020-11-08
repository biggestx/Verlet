using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRenderController : MonoBehaviour
{
    [Serializable]
    public class Pair
    {
        public Transform From;
        public Transform To;
    }

    [SerializeField]
    public List<Pair> Pairs = null;

    private void OnDrawGizmos()
    {
        if (Pairs != null)
        {
            Pairs.ForEach(pV =>
            {
                Gizmos.DrawLine(pV.From.position, pV.To.position);
            });
        }

    }

}
