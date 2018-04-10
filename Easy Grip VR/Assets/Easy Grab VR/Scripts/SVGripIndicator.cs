using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Abstract class. Use SVOutlineIndicator or SVGlowIndicator instead.
 */
public class SVAbstractGripIndicator : MonoBehaviour {

    [HideInInspector]
    public float indicatorActive = 0; // value between 0 and 1
    protected float lastIsActive = -1;
}
