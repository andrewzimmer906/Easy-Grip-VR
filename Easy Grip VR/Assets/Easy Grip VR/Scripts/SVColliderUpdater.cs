using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SVControllerInput))]
public class SVColliderUpdater : MonoBehaviour {
    public bool isLeft;
    private SVControllerInput input;
    private SphereCollider controllerCollider;

    // Real talk, is this really the best way to define a constant in c#?
    const float kKnockableCollisionSize = 0.06f;

    // Use this for initialization
    void Awake () {
        this.input = this.GetComponent<SVControllerInput>();

        this.controllerCollider = gameObject.AddComponent<SphereCollider>();
        this.controllerCollider.radius = kKnockableCollisionSize;

        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;

        gameObject.hideFlags = HideFlags.HideInHierarchy;
    }
	
	// Update is called once per frame
	void Update () {
        if (isLeft) {
            if (this.input.LeftControllerIsConnected) {
                this.transform.position = this.input.LeftControllerPosition;
                this.controllerCollider.enabled = true;
            } else {
                this.controllerCollider.enabled = false;
            }
        } else {
            if (this.input.RightControllerIsConnected) {
                this.transform.position = this.input.RightControllerPosition;
                this.controllerCollider.enabled = true;
            } else {
                this.controllerCollider.enabled = false;
            }
        }
    }

    private void OnCollisionEnter(Collision collision) {
        bool cancelCollision = true;
        if (collision.gameObject.GetComponent<SVGrabbable>()) {
            SVGrabbable grabbable = collision.gameObject.GetComponent<SVGrabbable>();
            if (grabbable.isKnockable) {
                cancelCollision = false;
            }
        }

        if (cancelCollision) {
            Physics.IgnoreCollision(collision.collider, this.controllerCollider);
        }
    }
}
