using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SVGlowIndicator : SVAbstractGripIndicator {
    public Color glowColor;

    private Material[] modelMaterials;
    private Color updatedColor;

    // Use this for initialization
    void Start () {

        MeshRenderer[] renderers = (MeshRenderer[])SVUtilities.AllComponentsOfType<MeshRenderer>(gameObject);
        this.modelMaterials = new Material[renderers.Length];
        for (int i = 0; i < modelMaterials.Length; i++) {
            this.modelMaterials[i] = renderers[i].material;
        }

        foreach (Material material in this.modelMaterials) {
            material.EnableKeyword("_EMISSION");
        }
        updatedColor = new Color(0, 0, 0);
    }

    // Update is called once per frame
    void Update () {
        if (this.lastIsActive != this.indicatorActive) {
            updatedColor.r = glowColor.r * this.indicatorActive * glowColor.a;
            updatedColor.g = glowColor.g * this.indicatorActive * glowColor.a;
            updatedColor.b = glowColor.b * this.indicatorActive * glowColor.a;

            foreach (Material material in this.modelMaterials) {
                material.SetColor("_EmissionColor", updatedColor);
            }
        }
    }
}
