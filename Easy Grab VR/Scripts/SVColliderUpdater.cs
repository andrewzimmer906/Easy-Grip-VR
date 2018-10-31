using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SVControllerInput))]
public class SVColliderUpdater : MonoBehaviour {
    public bool isLeft;
    private SVControllerInput input;
    private SphereCollider controllerCollider;
    private Rigidbody rb;


    // Real talk, is this really the best way to define a constant in c#?
#if USES_STEAM_VR
    const float kKnockableColliderSize = 0.06f;
    const float kHandMass = 1.0f;
    Vector3 kKnockableColliderPosition = new Vector3(.00f, -.03f, -.085f);
#elif USES_OPEN_VR
    const float kKnockableColliderSize = 0.06f;
    const float kHandMass = 1.0f;
    Vector3 kKnockableColliderPosition = new Vector3(.00f, -.03f, 0f);
#else
    const float kKnockableColliderSize = 0.06f;
    const float kHandMass = 1.0f;
    Vector3 kKnockableColliderPosition = new Vector3(.00f, -.03f, -.085f);
#endif
    // Use this for initialization
    void Awake () {
        this.input = this.GetComponent<SVControllerInput>();

        this.controllerCollider = gameObject.AddComponent<SphereCollider>();
        this.controllerCollider.radius = kKnockableColliderSize;
        this.controllerCollider.center = kKnockableColliderPosition;

        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = kHandMass;
        rb.isKinematic = false;

        gameObject.hideFlags = HideFlags.HideInHierarchy;
    }
	
	// Update is called once per frame
	void Update () {
        if (isLeft) {
            if (this.input.LeftControllerIsConnected && !SVControllerManager.leftControllerActive) {
                this.transform.rotation = this.input.LeftControllerRotation;
                this.transform.position = this.input.LeftControllerPosition;
                rb.velocity = this.input.LeftControllerVelocity;
                this.controllerCollider.enabled = true;
            } else {
                this.controllerCollider.enabled = false;
            }
        } else {
            if (this.input.RightControllerIsConnected && !SVControllerManager.rightControllerActive) {
                this.transform.rotation = this.input.RightControllerRotation;
                this.transform.position = this.input.RightControllerPosition;
                rb.velocity = this.input.RightControllerVelocity;
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
