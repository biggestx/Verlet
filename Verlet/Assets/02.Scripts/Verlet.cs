using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Verlet : MonoBehaviour
{

    protected VerletSimulator Parameters = null;

    public void Bind(VerletSimulator @param)
    {
        Parameters = param;
    }

    public virtual void Init()
    {

    }

    public virtual void UpdateVerlet()
    {

    }

}
