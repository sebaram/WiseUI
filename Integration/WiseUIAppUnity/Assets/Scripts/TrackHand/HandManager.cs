using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandManager : MonoBehaviour
{


    #region Singleton
    /// <summary>
    /// Creates instance of SkeletonManager
    /// </summary>
    public static HandManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            this.gameObject.SetActive(false);
            Debug.LogWarning("More than 1 HandManager in scene");
        }
    }
    #endregion

    [HideInInspector]
    ///The list of joints used for visualization
    public List<GameObject> _listOfJoints = new List<GameObject>();

    ///The prefab that will be used for visualization of the joints 
    [SerializeField]
    private GameObject[] jointPrefab;

    ///The linerenderes used on the joints in the jointPrefabs
    private LineRenderer[] lineRenderers = new LineRenderer[6];

    ///used to clamp the depth value
    private float clampMinDepth = 0.4f;

    ///The materials used on the joints / Line renderers
    [SerializeField]
    private Material[] jointsMaterial;

    /// The number of Joints the skeleton is made of.
    private int jointsLength = 21;

    // Start is called before the first frame update
    void Start()
    {
        Inititialize();
    }

    void Inititialize()
    {

        // for (int i = 0; i < jointPrefab.Length; i++)
        // {
        //     jointPrefab[i] = Instantiate(jointPrefab[i]);
        // }

        // SkeletonModel(0, 1);

        // ManomotionManager.OnSkeleton3dActive += SkeletonModel;     

        // for (int i = 0; i < jointsMaterial.Length; i++)
        // {
        //     Color tempColor = jointsMaterial[i].color;
        //     tempColor.a = 0f;
        //     jointsMaterial[i].color = tempColor;
        // }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
