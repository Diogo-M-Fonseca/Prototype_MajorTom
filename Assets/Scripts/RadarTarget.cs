using System.Collections.Generic;
using UnityEngine;

public class RadarTarget : MonoBehaviour
{
    public static readonly List<RadarTarget> All = new();

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

}
