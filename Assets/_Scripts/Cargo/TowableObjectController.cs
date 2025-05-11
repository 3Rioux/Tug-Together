using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all the cargo barge states and information storage 
/// </summary>
public class TowableObjectController : MonoBehaviour
{
    [Tooltip("Stores all the attache point transforms ")]
    [SerializeField] private List<Transform> towAttachentPointList = new List<Transform>();

    public List<Transform> TowPointList { get => towAttachentPointList; set => towAttachentPointList = value; }


   
}
