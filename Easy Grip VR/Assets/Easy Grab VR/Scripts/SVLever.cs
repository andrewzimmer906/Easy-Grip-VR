using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/**
 * Creates a VRLever. Heads up, we're using Euler Angles here so don't try to lever around the 360 angle.  Things will break!
 */ 
[RequireComponent(typeof(HingeJoint))]
public class SVLever : MonoBehaviour {

    public float leverOnAngle = -45;
    public float leverOffAngle = 45;

    public bool leverIsOn = false;
    public bool leverWasSwitched = false;

    private HingeJoint leverHingeJoint;

    private SVGrabbable grabbable;
    private bool wasGrabbed = false;

    private Vector3 startingEuler;

    void Start () {
        leverHingeJoint = GetComponent<HingeJoint>();

        JointLimits limits = leverHingeJoint.limits;
        limits.max = Mathf.Max(leverOnAngle, leverOffAngle);
        limits.min = Mathf.Min(leverOnAngle, leverOffAngle);
        leverHingeJoint.limits = limits;
        leverHingeJoint.useLimits = true;

        // Get a grabbable on the Lever or one of it's children. You could technically have the grabbable outside of the lever
        // And connect it with a fixed joint, if so just set grabbable to public and set it in editor.
        SVGrabbable[] grabbables = (SVGrabbable[])SVUtilities.AllComponentsOfType<SVGrabbable>(gameObject);
        Assert.IsFalse(grabbables.Length > 1, "SVLever only supports one grabbing surface at a time.");
        Assert.IsFalse(grabbables.Length <= 0, "SVLever requires a grabble component on it, or a child object, to function.");
        grabbable = grabbables[0];

        startingEuler = this.transform.localEulerAngles;

        UpdateHingeJoint();


    }

    // Update is called once per frame
    void Update () {
        leverWasSwitched = false;

        float offDistance = Quaternion.Angle(this.transform.localRotation, OffHingeAngle());
        float onDistance = Quaternion.Angle(this.transform.localRotation, OnHingeAngle());

        bool shouldBeOn = (Mathf.Abs(onDistance) < Mathf.Abs(offDistance));
        if (shouldBeOn != leverIsOn) {
            leverIsOn = !leverIsOn;
            leverWasSwitched = true;
            UpdateHingeJoint();
        }

        if (wasGrabbed != grabbable.inHand) {
            wasGrabbed = grabbable.inHand;
            UpdateHingeJoint();
        }
	}

    private void UpdateHingeJoint() {
        JointSpring spring = leverHingeJoint.spring;

        if (grabbable.inHand) {
            leverHingeJoint.useSpring = false;
        } else {
            if (leverIsOn) {
                spring.targetPosition = leverOnAngle;
            } else {
                spring.targetPosition = leverOffAngle;
            }
            leverHingeJoint.useSpring = true;
        }

        leverHingeJoint.spring = spring;
    }

    private Quaternion OnHingeAngle() {
        return Quaternion.Euler(this.leverHingeJoint.axis * leverOnAngle + startingEuler);
    }

    private Quaternion OffHingeAngle() {
        return Quaternion.Euler(this.leverHingeJoint.axis * leverOffAngle + startingEuler);
    }
}
