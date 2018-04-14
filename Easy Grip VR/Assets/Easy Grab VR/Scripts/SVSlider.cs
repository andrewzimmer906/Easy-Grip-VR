using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class SVSlider : MonoBehaviour {
    public float value;


    private Vector3 onPosition;
    private Vector3 offPosition;
    private float totalDistance;
    private ConfigurableJoint sj;

	void Start () {
        sj = GetComponent<ConfigurableJoint>();

        Vector3 startingPosition = this.transform.localPosition;
        if (sj.xMotion == ConfigurableJointMotion.Limited) {
            onPosition = startingPosition + Vector3.right * sj.linearLimit.limit;
            offPosition = startingPosition - Vector3.right * sj.linearLimit.limit;
        } else if (sj.yMotion == ConfigurableJointMotion.Limited) {
            onPosition = startingPosition + Vector3.up * sj.linearLimit.limit;
            offPosition = startingPosition - Vector3.up * sj.linearLimit.limit;
        } else if (sj.zMotion == ConfigurableJointMotion.Limited) {
            onPosition = startingPosition + Vector3.forward * sj.linearLimit.limit;
            offPosition = startingPosition - Vector3.forward * sj.linearLimit.limit;
        }

        totalDistance = (onPosition - offPosition).magnitude;
    }
	
	// Update is called once per frame
	void Update () {
        value = Mathf.Min(Mathf.Max((onPosition - this.transform.localPosition).magnitude / totalDistance, 0f), 1f);
	}
}
