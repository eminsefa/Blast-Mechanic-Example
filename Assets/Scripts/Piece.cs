using System.Collections.Generic;
using UnityEngine;

public class Piece:MonoBehaviour
{
    private bool _created;
    public int pieceType;
    private GameObject _currentIcon;
    private List<GameObject> _icons = new List<GameObject>();
    [SerializeField] private List<GameObject> colors;
    
    
    public void SetIcon(int stage)
    {
        //Change icon depending on stage
        if(_created) _currentIcon.SetActive(false);
        _currentIcon = _icons[stage];
        _currentIcon.SetActive(true);
    }

    public void Enabled(int color)
    {
        //Is called when level start and piece is destroyed(moved back to up randomly)
        if(_created) _currentIcon.SetActive(false);
        _icons.Clear();
        colors[pieceType].SetActive(false);
        pieceType = color;
        colors[pieceType].SetActive(true);
        foreach (Transform tr in colors[pieceType].transform)
        {
            _icons.Add(tr.gameObject);
        }
        SetIcon(0);
        _created = true;
    }
}
