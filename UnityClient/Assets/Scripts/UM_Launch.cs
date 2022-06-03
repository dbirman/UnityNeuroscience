using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class UM_Launch : MonoBehaviour
{
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private BrainCameraController cameraController;
    [SerializeField] private float maxExplosion = 10f;

    [SerializeField] private GameObject consolePanel;
    [SerializeField] private TextMeshProUGUI consoleText;

    [SerializeField] private bool loadDefaults;

    // Exploding
    [Range(0,1), SerializeField] private float percentageExploded = 0f;
    private float prevPerc = 0f;
    private bool explodeLeftOnly;
    private bool colorLeftOnly;

    private Vector3 center = new Vector3(5.7f, 4f, -6.6f);

    // Colormaps
    private List<Converter<float, Color>> colormaps;
    [SerializeField] private List<string> colormapOptions;
    private Converter<float, Color> activeColormap;
    private Vector3 teal = new Vector3(0f, 1f, 1f);
    private Vector3 magenta = new Vector3(1f, 0f, 1f);

    private int[] cosmos = { 315, 698, 1089, 703, 623, 549, 1097, 313, 1065, 512 };
    private Dictionary<int, Vector3> cosmosMeshCenters;
    private Dictionary<int, Vector3> originalTransformPositionsLeft;
    private Dictionary<int, Vector3> originalTransformPositionsRight;
    private Dictionary<int, Vector3> nodeMeshCenters;
    private Dictionary<int, Vector3> cosmosVectors;
    
    private Dictionary<int, CCFTreeNode> visibleNodes;

    // COSMOS
    [SerializeField] private List<GameObject> cosmosParentObjects;
    private int cosmosParentIdx = 0;

    private bool ccfLoaded;

    // Start is called before the first frame update
    void Start()
    {
        colormaps = new List<Converter<float, Color>>();
        colormaps.Add(Cool);
        colormaps.Add(Gray);
        activeColormap = Cool;

        originalTransformPositionsLeft = new Dictionary<int, Vector3>();
        originalTransformPositionsRight = new Dictionary<int, Vector3>();
        nodeMeshCenters = new Dictionary<int, Vector3>();

        visibleNodes = new Dictionary<int, CCFTreeNode>();

        modelControl.SetBeryl(true);
        modelControl.LateStart(loadDefaults);

        if (loadDefaults)
            DelayedStart();

        RecomputeCosmosCenters();
    }

    private async void DelayedStart()
    {
        await modelControl.GetDefaultLoaded();
        ccfLoaded = true;

        foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
        {
            FixNodeTransformPosition(node);

            RegisterNode(node);
            node.SetNodeModelVisibility(true, true);
        }
    }

    public void FixNodeTransformPosition(CCFTreeNode node)
    {
        // I don't know why we have to do this? For some reason when we load the node models their positions are all offset in space in a weird way... 
        node.GetNodeTransform().localPosition = Vector3.zero;
        node.GetNodeTransform().localRotation = Quaternion.identity;
        node.RightGameObject().transform.localPosition = Vector3.forward * 11.4f;
    }

    public void ChangeCosmosIdx(int newIdx)
    {
        cosmosParentIdx = newIdx;
        RecomputeCosmosCenters();
        UpdateExploded(percentageExploded);
    }

    private void RecomputeCosmosCenters()
    {
        GameObject parentGO = cosmosParentObjects[cosmosParentIdx];
        parentGO.SetActive(true);

        cosmosMeshCenters = new Dictionary<int, Vector3>();
        cosmosVectors = new Dictionary<int, Vector3>();

        // save the cosmos transform positions
        foreach (int cosmosID in cosmos)
        {
            GameObject cosmosGO = parentGO.transform.Find(cosmosID + "L").gameObject;
            cosmosMeshCenters.Add(cosmosID, cosmosGO.GetComponentInChildren<Renderer>().bounds.center);
            cosmosGO.SetActive(false);

            cosmosVectors.Add(cosmosID, cosmosGO.transform.localPosition);
        }

        parentGO.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        //Check if we need to make an update
        if (prevPerc != percentageExploded)
        {
            prevPerc = percentageExploded;

            // for each tree node, move it's model away from the 0,0,0 point
            _UpdateExploded();
        }

        // Check for key down events
        if (Input.GetKeyDown(KeyCode.C))
        {
            consolePanel.SetActive(!consolePanel.activeSelf);
        }
    }

    public void RegisterNode(CCFTreeNode node)
    {
        originalTransformPositionsLeft.Add(node.ID, node.LeftGameObject().transform.localPosition);
        originalTransformPositionsRight.Add(node.ID, node.RightGameObject().transform.localPosition);
        nodeMeshCenters.Add(node.ID, node.GetMeshCenter());
        visibleNodes.Add(node.ID,node);
    }

    public Color GetColormapColor(float perc)
    {
        return activeColormap(perc);
    }

    public void ChangeColormap(string newColormap)
    {
        if (colormapOptions.Contains(newColormap))
            activeColormap = colormaps[colormapOptions.IndexOf(newColormap)];
        else
            Log("Colormap " + newColormap + " not an available option");
    }

    // [TODO] Refactor colormaps into their own class
    public Color Cool(float perc)
    {
        Vector3 colorVector = Vector3.Lerp(teal, magenta, perc);
        return new Color(colorVector.x, colorVector.y, colorVector.z, 1f);
    }

    public Color Gray(float perc)
    {
        Vector3 colorVector = Vector3.Lerp(Vector3.zero, Vector3.one, perc);
        return new Color(colorVector.x, colorVector.y, colorVector.z, 1f);
    }

    public void Log(string text)
    {
        // Todo: deal with log running off the screen
        Debug.Log(text);
        consoleText.text += "\n" + text;
    }

    public void SetLeftExplodeOnly(bool state)
    {
        explodeLeftOnly = state;
        _UpdateExploded();
    }

    public void SetLeftColorOnly(bool state)
    {
        colorLeftOnly = state;
        if (colorLeftOnly)
        {
            Debug.Log("Doing the thing");
            foreach (CCFTreeNode node in visibleNodes.Values)
            {
                Debug.Log(node.GetColor());
                Debug.Log(node.GetDefaultColor());
                node.SetColorOneSided(node.GetColor(), true);
            }
        }
        else
        {
            Debug.Log("Reversing the thing");
            foreach (CCFTreeNode node in visibleNodes.Values)
            {
                node.SetColor(node.GetColor());
            }
        }
    }

    public bool GetLeftColorOnly()
    {
        return colorLeftOnly;
    }

    public void UpdateExploded(float newPercExploded)
    {
        percentageExploded = newPercExploded;
        _UpdateExploded();
    }

    private void _UpdateExploded()
    {
        cameraController.SetControlBlock(true);

        Vector3 flipVector = new Vector3(1f, 1f, -1f);

        foreach (CCFTreeNode node in visibleNodes.Values)
        {
            int cosmos = modelControl.GetCosmosID(node.ID);
            Transform nodeTLeft = node.LeftGameObject().transform;
            Transform nodeTright = node.RightGameObject().transform;

            nodeTLeft.localPosition = originalTransformPositionsLeft[node.ID] +
                cosmosVectors[cosmos] * percentageExploded;

            if (!explodeLeftOnly)
            {
                nodeTright.localPosition = originalTransformPositionsRight[node.ID] +
                    Vector3.Scale(cosmosVectors[cosmos], flipVector) * percentageExploded;
            }
            else
            {
                nodeTright.localPosition = originalTransformPositionsRight[node.ID];
            }
        }
    }

    public void UpdateDataIndex(float newIdx)
    {
        Debug.Log(newIdx);
    }
}
