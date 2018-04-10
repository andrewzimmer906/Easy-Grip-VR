using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SVGrabbable))]
[RequireComponent(typeof(SVControllerInput))]
public class SVGrabHaptics : MonoBehaviour {

    public bool hapticsOnGrab = true;
    public bool hapticsOnCollision = true;
    public float vibrationLengthInSeconds = 0.25f;

    private bool inHand = false;
    private SVGrabbable grabbable;
    private SVControllerInput input;
    void Start () {
        grabbable = this.GetComponent<SVGrabbable>();
        input = this.GetComponent<SVControllerInput>();
    }
	
	// Update is called once per frame
	void Update () {
		if (grabbable.inHand && !inHand && hapticsOnGrab) {
            input.RumbleActiveController(vibrationLengthInSeconds);
        }

        inHand = grabbable.inHand;
    }

    private void OnCollisionEnter(Collision collision) {
        if (grabbable.inHand && hapticsOnCollision) {
            input.RumbleActiveController(vibrationLengthInSeconds);
        }
    }
}
