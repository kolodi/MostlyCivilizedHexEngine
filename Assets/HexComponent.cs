using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexComponent : MonoBehaviour {

    /// <summary>
    /// Hex data
    /// </summary>
    public Hex Hex;

    /// <summary>
    /// Map reference
    /// </summary>
    public HexMap HexMap;

    /// <summary>
    /// Debug column label
    /// </summary>
    public TextMesh Q_Display;

    /// <summary>
    /// Debug row label
    /// </summary>
    public TextMesh R_Display;

    private void Start()
    {
        UpdateDebugLabels();
    }

    void UpdateDebugLabels()
    {
        Q_Display.text = "Q:" + Hex.Q.ToString();
        R_Display.text = "R:" + Hex.R.ToString();
    }

    public void UpdatePosition()
    {
        this.transform.position = Hex.PositionFromCamera(
            Camera.main.transform.position,
            HexMap.NumRows,
            HexMap.NumColumns
        );
    }

}
