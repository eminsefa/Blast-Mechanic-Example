using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//This is where grid, camera, background and border is set
public class LevelSetter : MonoBehaviour
{
    public static LevelSetter Instance;
    private const float ScreenDefaultAspect = 0.46f; // 1125x2436
    private float _screenAspect;
    private LevelData _currentLevelData;
    private Dictionary<int, Piece> _gridPieceDictionary = new Dictionary<int, Piece>();
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private Transform border;
    [SerializeField] private Transform background;
    private void Awake()
    {
        Instance = this;
        _screenAspect = (float) Screen.width / Screen.height;
        
        //Gets the data and sets the level
        var data = LevelDataHandler.ReadLevelData();
        SetGrid(data[0]); //0 would be level number
    }

    private void SetCameraSizeForGrid(int gridWidth, int gridHeight)
    {
        //Sets camera and background size depending on grid and screen aspects
        var cam = Camera.main;
        var gridAspect = (float) gridWidth / gridHeight;

        if (gridAspect > _screenAspect) cam.orthographicSize = (gridWidth / (2 * _screenAspect)) + 1;
        else cam.orthographicSize = (gridHeight + 2) / 2f;
        background.localScale *= Mathf.Max((cam.orthographicSize / 5), (_screenAspect / ScreenDefaultAspect));
    }

    private void SetGrid(LevelData data)
    {
        _currentLevelData = data;
        var gridWidth = data.GridWidth;
        var gridHeight = data.GridHeight;
        var colorCount = data.ColorCount;

        border.localScale =
            new Vector2(gridWidth + 0.45f * gridWidth / 10f, gridHeight + 0.5f * gridHeight / 12f + 0.1f);

        SetCameraSizeForGrid(gridWidth, gridHeight);

        var iCount = 0;
        for (int i = -gridHeight + 1; i < gridHeight; i += 2)
        {
            var jCount = 0;
            for (int j = -gridWidth + 1; j < gridWidth; j += 2)
            {
                var spawnPos = new Vector3(j / 2f, i / 2f, 0);
                var gridNumber = iCount * gridWidth + jCount;
                var pieceInt = Random.Range(0, colorCount);
                var p = Instantiate(piecePrefab, spawnPos, Quaternion.identity, transform)
                    .GetComponent<Piece>();
                p.Enabled(pieceInt);
                _gridPieceDictionary.Add(gridNumber, p);
                jCount++;
            }

            iCount++;
        }
    }

    public (Dictionary<int, Piece>, LevelData) GetPieceData()
    {
        return (_gridPieceDictionary, _currentLevelData);
    }
}