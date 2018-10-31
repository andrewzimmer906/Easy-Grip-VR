using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SVGrabbable))]
public class SVButton : MonoBehaviour {

    public float buttonDepressDistance = 0.05f;
    public float buttonPressDuration = 0.5f;

    public bool buttonPressed = false;
    public bool buttonUnpressed = false;
    public bool buttonDown = false;

    private float buttonPressedTime;
    private bool buttonImmediatelyPressed;  // used to hold onto the pressed state for one frame.
    private bool buttonImmediatelyUnpressed;  // used to hold onto the pressed state for one frame.

    private AudioSource audioSource;

    void Update() {
        if (this.buttonDown) {
            // Only hold button pressed for one frame
            if (this.buttonPressed && !this.buttonImmediatelyPressed) {
                this.buttonPressed = false;
            }
            this.buttonImmediatelyPressed = false;

            if (Time.time - this.buttonPressedTime > buttonPressDuration) {
                this.transform.position += this.transform.TransformDirection(new Vector3(0.0f, buttonDepressDistance, 0.0f));
                this.buttonDown = false;
                this.buttonImmediatelyUnpressed = true;
            }
        } else {
            if (this.buttonImmediatelyUnpressed) {
                this.buttonUnpressed = true;
                this.buttonImmediatelyUnpressed = false;
            } else {
                this.buttonUnpressed = false;
            }
        }
    }

    private void OnCollisionEnter(Collision collision) {
        this.buttonPressedTime = Time.time;
        if (this.buttonDown) { return; }
        this.transform.position += this.transform.TransformDirection(new Vector3(0.0f, -buttonDepressDistance, 0.0f));

        this.buttonImmediatelyPressed = true;
        this.buttonPressed = true;
        this.buttonDown = true;
    }
}
