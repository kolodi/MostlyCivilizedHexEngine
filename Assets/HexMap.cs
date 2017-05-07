using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMap : MonoBehaviour, 
    IDragHandler, 
    IBeginDragHandler, 
    IEndDragHandler
{

    #region Visible in inspector

    [Tooltip("Unique pprefab that is currently used for hexes")]
    public GameObject HexPrefab;
    [Tooltip("Set of materials randomly used for different hexes during map generation")]
    public Material[] HexMaterials;
    
    [Tooltip("How meny hexes to draw around current map center")]
    [Range(1, 100)]
    public int Radius = 5;
    [Tooltip("Number of map rows")]
    public int NumRows = 20;
    [Tooltip("Number of map columns")]
    public int NumColumns = 20;

    #endregion

    #region Private fields and properties

    /// <summary>
    /// Hexex 2-dimansional array.
    /// Map data.
    /// </summary>
    Hex[,] hexes;

    /// <summary>
    /// Hex materials dictionary
    /// </summary>
    Dictionary<string, Material> testMaterials;

    /// <summary>
    /// The hex meant to be at the center of a 
    /// map portion that is currently displayed
    /// </summary>
    Hex currentCenterHex;
    
    /// <summary>
    /// Map Width in meters
    /// </summary>
    float mapWidth;
    /// <summary>
    /// Map height in meters
    /// </summary>
    float mapHeight;

    /// <summary>
    /// Distance along x axis between 2 hexes (2 map columns)
    /// </summary>
    float hexHorizontalSpacing;
    /// <summary>
    /// Distance along z axis between 2 hexes (2 map rows)
    /// </summary>
    float hexVerticalSpacing;

    #endregion

    #region Initialisations

    void Start()
    {
        CreateMaterialDictionary();
        GenerateMap();
        DisplayMapPortion(hexes[0, 0], Radius);
    }

    /// <summary>
    /// Prepare material dictionary
    /// </summary>
    void CreateMaterialDictionary()
    {
        testMaterials = new Dictionary<string, Material>();
        foreach (Material mat in HexMaterials)
        {
            testMaterials.Add(mat.name, mat);
        }
    }

    #endregion

    #region Map Generation

    /// <summary>
    /// Fill hexes array with hexes.
    /// </summary>
    public void GenerateMap()
    {
        /// TODO: use some height map (perlin noise or similar) 
        /// to have a natural material distribution and not just random

        /// Create example hex to obtain some useful data
        Hex infoHex = new Hex(0, 0);
        hexHorizontalSpacing = infoHex.HexHorizontalSpacing();
        hexVerticalSpacing = infoHex.HexVerticalSpacing();
        mapWidth = hexHorizontalSpacing * NumColumns;
        mapHeight = hexVerticalSpacing * NumRows;

        /// initiate hexes array
        hexes = new Hex[NumColumns, NumRows];
        /// fill array with hexes
        for (int column = 0; column < NumColumns; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                Hex h = new Hex( column, row );
                h.TestMaterialName = HexMaterials[Random.Range(0, HexMaterials.Length)].name;
                hexes[column, row] = h;
            }
        }
    }

    #endregion

    #region Map Drawing

    /// <summary>
    /// Draw all map hexes.
    /// </summary>
    public void DrawAllMap()
    {
        for (int column = 0; column < hexes.GetLength(0); column++)
        {
            for (int row = 0; row < hexes.GetLength(1); row++)
            {
                Hex h = hexes[column, row];
                GameObject hexGO = Instantiate<GameObject>(HexPrefab, this.transform);
                hexGO.transform.localPosition = h.Position();
                hexGO.GetComponentInChildren<MeshRenderer>().material = testMaterials[h.TestMaterialName];
                HexComponent hc = hexGO.GetComponent<HexComponent>();
                hc.HexMap = this;
                hc.Hex = h;
                
            }
        }

        // shift map to look at map center

    }

    /// <summary>
    /// Remove all map hexes
    /// </summary>
    void ClearMap()
    {
        while (transform.childCount > 0)
        {
            Transform c = transform.GetChild(0);
            c.parent = null;
            Destroy(c.gameObject);
        }
    }

    /// <summary>
    /// Redraw portion of the map around center hex (the hex in focus)
    /// </summary>
    /// <param name="centerHex">new center hex</param>
    /// <param name="radius">radius from center hex to draw other hexes around</param>
    void RedrawMapPortion(Hex centerHex, int radius)
    {
        /// TODO: implement hexes gameobjects pooling
        /// TODO: implement differential redrawing (remove only hexes outside new radius and draw new hexes only)
        ClearMap();
        DisplayMapPortion(centerHex, radius);
    }

    /// <summary>
    /// Draw portion of the map around center hex (the hex in focus)
    /// </summary>
    /// <param name="centerHex"></param>
    /// <param name="radius"></param>
    void DisplayMapPortion(Hex centerHex, int radius)
    {
        currentCenterHex = centerHex;
        // TODO: implement east-west wrapping
        // traverse hexes subset
        int westLimit = Mathf.Clamp(centerHex.Q - radius, 0, centerHex.Q);
        int eastLimit = Mathf.Clamp(centerHex.Q + radius, centerHex.Q, NumColumns);
        int southLimit = Mathf.Clamp(centerHex.R - radius, 0, centerHex.R);
        int northLimit = Mathf.Clamp(centerHex.R + radius, centerHex.R, NumRows);


        for (int column = westLimit; column < eastLimit; column++)
        {
            for (int row = southLimit; row < northLimit; row++)
            {
                DrawHex(hexes[column, row]);
            }
        }
    }

    /// <summary>
    /// Draw a hex
    /// </summary>
    /// <param name="hex">Hex to draw</param>
    /// <param name="goName">Hex game object name</param>
    void DrawHex(Hex hex, string goName = "hex")
    {
        /// TODO: implement hex gameobjects pooling
        GameObject hexGO = Instantiate<GameObject>(HexPrefab, this.transform);
        hexGO.name = goName;
        hexGO.transform.localPosition = hex.Position();
        hexGO.GetComponentInChildren<MeshRenderer>().material = testMaterials[hex.TestMaterialName];
        HexComponent hc = hexGO.GetComponent<HexComponent>();
        hc.Hex = hex;
        hc.HexMap = this;
    }

    #endregion

    #region Drag Handlers

    /// <summary>
    /// Last raycast intersection point on map
    /// </summary>
    Vector3 lastOnDragIntersectionPoint;
    /// <summary>
    /// Raycast hit world position when drag begins
    /// </summary>
    Vector3 onBeginDragIntersectionPoint;
    /// <summary>
    /// The hex that was touched when drag begins.
    /// Null if user grab the map outside any hex.
    /// </summary>
    HexComponent onBeginDragHex;

    /// <summary>
    /// OnDrag interface implementation.
    /// Used for map displacement.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {

        Vector3 delta = eventData.pointerCurrentRaycast.worldPosition - lastOnDragIntersectionPoint;
        // just to secure y is always 0
        delta.y = 0;
        transform.position += delta;
        lastOnDragIntersectionPoint = eventData.pointerCurrentRaycast.worldPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        onBeginDragIntersectionPoint = eventData.pointerCurrentRaycast.worldPosition;
        lastOnDragIntersectionPoint = onBeginDragIntersectionPoint;

        onBeginDragHex = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<HexComponent>();
        
    }

    /// <summary>
    /// OnEndDrag interface implementation.
    /// This event handler is used to trigger map portion redrawing 
    /// due to its displacement
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        /// 1st variant. Raycast from camera center
        /// downsides: 
        /// 1. does not work when raycast miss the tile (outside the map)
        /// 2. not convenient when camera angle changes
        /// TODO: combine two variants to use advantages of both variants

        //RaycastHit hit;
        //Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        //if(Physics.Raycast(ray, out hit, 100f))
        //{
        //    HexComponent hc = hit.collider.gameObject.GetComponentInParent<HexComponent>();
        //    if (hc != null)
        //    {
        //        if (hc.Hex != currentCenterHex)
        //        {
        //            ClearMap();
        //            DisplayMapPortion(hc.Hex, Radius);
        //        }
        //    }
        //}

        /// 2nd variant. calculate drug delta
        /// downsides: 
        /// 1. slightly inpresize,
        /// 2. require additional collider for the map outside zone

        Vector3 mapDisplacementDelta = eventData.pointerCurrentRaycast.worldPosition - onBeginDragIntersectionPoint;
        mapDisplacementDelta.y = 0;

        //Debug.Log(overallDragDelta);

        if (mapDisplacementDelta.magnitude > 1)
        {
            Hex previousCenterHex = currentCenterHex;
            /// if there was hex touched at the drag begin, 
            /// use it as previous center to be more presize
            if (onBeginDragHex != null)
                previousCenterHex = onBeginDragHex.Hex;
            
            /// get columns delta:
            /// inverse x axis map displacement / distance between columns
            /// round to int to use as column index
            int columnsDelta = Mathf.RoundToInt(-mapDisplacementDelta.x / hexHorizontalSpacing);

            /// get new center column index after map displacement
            /// clamp column index between 0 and last possible index (assuming no east-west wrapping)
            /// TODO: implement east-west wrapping
            int newCenterColumn = Mathf.Clamp(previousCenterHex.Q + columnsDelta, 0, NumColumns - 1);

            /// in the same way get rows delta
            int rowsDelta = Mathf.RoundToInt(-mapDisplacementDelta.z / hexVerticalSpacing);
            /// get new center row index
            int newCenterRow = Mathf.Clamp(previousCenterHex.R + rowsDelta, 0, NumRows - 1);

            
            Hex newCenterHex = hexes[newCenterColumn, newCenterRow];

            /// redraw map only if new center hex has changed
            if (currentCenterHex != newCenterHex)
            {
                RedrawMapPortion(newCenterHex, Radius);
            }
        }
    }

    #endregion
}
