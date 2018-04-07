using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SVOutline : MonoBehaviour {

	[HideInInspector]
    public float outlineActive = 0; // value between 0 and 1

    [Tooltip("The thickness of the outline effect")]
    public float outlineThickness = 1f;
    public Color outlineColor;

    // NAVI: HEY, OVER HERE!
    // If you rearrange the folders you might need to update this path!
    private string materialPath = "Easy Grip VR/SVOutlineMaterial";

    private GameObject outlineModel;
    private Material outlineModelMaterial;

    private float lastIsActive = 100;

	// Use this for initialization
	void Start () {
        this.outlineModelMaterial = (Material)Resources.Load(materialPath, typeof(Material));
        Assert.IsNotNull(this.outlineModelMaterial, "SVOutline was unable to load the SVOutlineMaterial. No biggie, this probably means you need to reset your folder structure. You'll need to have a structure like this Resources/Easy Grip VR/SVOutlineMaterial.");

        this.RefreshHighlightMesh();
	}
	
	// Update is called once per frame
	void Update () {
        if (this.lastIsActive != this.outlineActive) {
            outlineModelMaterial.SetFloat("_Alpha", this.outlineActive);
            outlineModelMaterial.SetFloat("_Thickness", this.outlineThickness);
            outlineModelMaterial.SetColor("_OutlineColor", (Color)this.outlineColor);
            this.lastIsActive = this.outlineActive;

            if (this.outlineActive > 0) {
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
