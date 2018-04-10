using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/**
 * This is used to draw an outline around the object you are picking up.  Alternativly use SVGlow to just adjust the emission variable instead. :)
 */
public class SVOutlineIndicator : SVAbstractGripIndicator {
    [Tooltip("The thickness of the outline effect")]
    public float outlineThickness = 1f;
    public Color outlineColor;

    // NAVI: HEY, OVER HERE!
    // If you rearrange the folders you might need to update this path!
    private string materialPath = "SVOutlineMaterial";

    private GameObject outlineModel;
    private Material outlineModelMaterial;

	// Use this for initialization
	void Start () {
        this.outlineModelMaterial = new Material((Material)Resources.Load(materialPath, typeof(Material)));
        Assert.IsNotNull(this.outlineModelMaterial, "SVOutlineIndicator was unable to load the SVOutlineMaterial. No biggie, this probably means you need to reset your folder structure. You'll need to have a structure like this Easy Grab VR/Resources/SVOutlineMaterial.");

        this.RefreshHighlightMesh();
	}

    // Update is called once per frame

    private void Update() {
        if (this.lastIsActive != this.indicatorActive) {
            outlineModelMaterial.SetFloat("_Alpha", this.indicatorActive);
            outlineModelMaterial.SetFloat("_Thickness", this.outlineThickness);
            outlineModelMaterial.SetColor("_OutlineColor", (Color)this.outlineColor);
            this.lastIsActive = this.indicatorActive;

            if (this.indicatorActive > 0) {
                this.outlineModel.SetActive(true);
            } else {
                this.outlineModel.SetActive(false);
            }
        }
    }

    public void RefreshHighlightMesh() {
        if (this.outlineModel != null) {
            Destroy(outlineModel);
        }

		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
		CombineInstance[] combine = new CombineInstance[meshFilters.Length];
		int i = 0;
		while (i < meshFilters.Length) {
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = gameObject.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;

			i++;
		}


		this.outlineModel = new GameObject(name + "OutlineModel");
		outlineModel.transform.SetParent(this.gameObject.transform, false);

		MeshFilter filter = outlineModel.AddComponent<MeshFilter>();
		filter.mesh = new Mesh ();
		filter.mesh.CombineMeshes (combine);

		MeshRenderer renderer = outlineModel.AddComponent<MeshRenderer>();
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.material = this.outlineModelMaterial;

		this.outlineModel.SetActive(false);
    }
}
