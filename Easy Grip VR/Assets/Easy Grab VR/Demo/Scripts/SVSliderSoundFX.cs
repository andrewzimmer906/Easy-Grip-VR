using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVSliderSoundFX : MonoBehaviour {
    public AudioClip sliderClip;

    public float minPitch = 0.5f;
    public float maxPitch = 2.0f;
    public float volume = 1f;

    private SVSlider slider;
    private SVGrabbable grabbable;

    private AudioSource audioSource;

    void Start () {
        slider = GetComponent<SVSlider>();
        grabbable = GetComponent<SVGrabbable>();
	}
	
	// Update is called once per frame
	void Update () {
        if (grabbable.inHand) {
            if (audioSource == null) {
                audioSource = SVUtilities.SetOrAddAudioSource(gameObject);
                audioSource.clip = sliderClip;
                audioSource.volume = volume;
                audioSource.loop = true;
            }

            audioSource.pitch = slider.value * (maxPitch - minPitch) + minPitch;
            if (!audioSource.isPlaying) {
                audioSource.Play();
            }
        } else {
            if (audioSource != null && audioSource.isPlaying) {
                audioSource.Stop();
            }
        }
    }
}
