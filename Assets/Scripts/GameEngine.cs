using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
//This is where all the calculations and game play is organized
public class GameEngine : MonoBehaviour, IPointerDownHandler
{
    private int _gridWidth;
    private int _gridHeight;
    private int _gridCount;
    private int _totalGroupCount;
    private float _spawnYPos;
    private LevelData _currentLevelData;
    private List<Tweener> _movePieceTweeners = new List<Tweener>();
    private List<int> _groupLimits = new List<int>();
    private Dictionary<int, Piece> _gridPieceDictionary = new Dictionary<int, Piece>();
    [SerializeField] private AnimationCurve moveEase;
    [SerializeField] private bool shuffle;
    private void Start()
    {
        //Gets the data
        var data = LevelSetter.Instance.GetPieceData();
        _gridPieceDictionary = data.Item1;
        _currentLevelData = data.Item2;
        _groupLimits = _currentLevelData.GroupLimits;
        _gridWidth = _currentLevelData.GridWidth;
        _gridHeight = _currentLevelData.GridHeight;
        _gridCount = _gridWidth * _gridHeight;
        for (int i = 0; i < _gridCount; i += 2) StartCoroutine(CheckGroups(i, false));
        _spawnYPos = Camera.main.orthographicSize + 1f;
    }

    private void Update()
    {
        //This is here because it is really hard for board to have deadlock situation
        //Did not find it necessary to create an editor button for this
        if (shuffle && MoveCompleted())
        {
            ShuffleBoard();
            shuffle = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!MoveCompleted()) return;
        var tr = eventData.pointerEnter.transform;
        var gridNumber = _gridPieceDictionary.FirstOrDefault(x => x.Value.transform == tr).Key;
        StartCoroutine(CheckGroups(gridNumber, true));
    }

    private IEnumerator CheckGroups(int gridNumber, bool blast)
    {
        //Check groups for destroy, set Icon and deadlock situation 
        while (!MoveCompleted()) yield return null;
        var gridType = _gridPieceDictionary[gridNumber].pieceType;
        var pieceGroup = GetConnectedPieces(new List<int> {gridNumber}, gridNumber, gridType);
        if (!blast)
        {
            SetIcons(pieceGroup);
            if (pieceGroup.Count == 1) _totalGroupCount++;
            if (_totalGroupCount == _gridCount) ShuffleBoard();
        }
        else if (pieceGroup.Count > 1) FindMostDownPiecesInBlastGroup(pieceGroup);
    }

    private List<int> GetConnectedPieces(List<int> blastedGroup, int gridNumber, int gridType)
    {
        //Gets connected pieces' grid numbers to list
        var l = new[]
        {
            -1, _gridWidth, 1, -_gridWidth,
        };
        for (int i = 0; i < l.Length; i++)
        {
            var neighbourGrid = gridNumber + l[i];
            if (neighbourGrid < 0 || neighbourGrid > _gridCount - 1) continue; //is in grid
            if (i % 2 == 0 && (gridNumber / _gridWidth != neighbourGrid / _gridWidth)) continue; // is connected
            if (blastedGroup.Contains(neighbourGrid)) continue; //is in list
            var neighbourType = _gridPieceDictionary[neighbourGrid].pieceType;
            if (neighbourType == gridType) //is same color
            {
                blastedGroup.Add(neighbourGrid);
                blastedGroup = GetConnectedPieces(blastedGroup, neighbourGrid, neighbourType);
            }
        }
        return blastedGroup;
    }

    private void SetIcons(List<int> blastedGroup)
    {
        //Sets group Icon depending on count
        var stage = 0;
        for (int i = 0; i < _groupLimits.Count; i++)
        {
            if (blastedGroup.Count > _groupLimits[i]) stage = i + 1;
            if (blastedGroup.Count <= _groupLimits[0]) stage = 0;
        }
        foreach (var p in blastedGroup)
        {
            _gridPieceDictionary[p].SetIcon(stage);
        }
    }

    private void ShuffleBoard()
    {
        //Shuffles board in deadlock situation
        //I did not find it necessary to write an algorithm to make it certain to fix deadlock
        //Because after the shuffle the possibility of re-shuffle is incredibly low
        //If it would be necessary with more futures added, what I would do is choose one or many piece and save the color and new position
        //Then my next same color piece would be neighbour to it randomly. Then shuffle rest of the grid
        var pieces = _gridPieceDictionary.Values.ToList();
        Tweener t;
        for (int i = 0; i < _gridCount; i++)
        {
            var rand = Random.Range(0, pieces.Count);
            var newPos = pieces[rand].transform.position;
            var newNumber = _gridPieceDictionary.First(x => x.Value == pieces[rand]).Key;
            var currentPiece = _gridPieceDictionary[i];
            t = currentPiece.transform.DOMove(newPos, 0.5f)
                .OnComplete(() => _gridPieceDictionary[newNumber] = currentPiece);
            _movePieceTweeners.Add(t);
            pieces.RemoveAt(rand);
        }
        _totalGroupCount = 0;
        for (int i = 0; i < _gridCount; i++) StartCoroutine(CheckGroups(i, false));
    }

    private void FindMostDownPiecesInBlastGroup(List<int> blastedGroup)
    {
        //Get columns of group and sets the most down pieces
        //Most down piece will help to get pieces will move
        var columns = SplitToColumns(blastedGroup);
        var checkGroup = true; //do once
        for (int i = 0; i < columns.Length; i++)
        {
            var mostDownInColumn = columns[i].Min(x => x);
            BlastAndSetColumn(columns[i], mostDownInColumn, checkGroup);
            checkGroup = false;
        }
    }

    private List<int>[] SplitToColumns(List<int> blastedGroup)
    {
        //Splits group to columns
        var columns = new List<int>[_gridWidth];
        for (int i = 0; i < _gridWidth; i++)
        {
            columns[i] = new List<int>();
        }
        foreach (var p in blastedGroup)
        {
            var mod = p % _gridWidth;
            columns[mod].Add(p);
        }
        return columns.Where(x => x.Count > 0).ToArray();
    }

    private void BlastAndSetColumn(List<int> blastedColumnGroup, int mostDownInColumn, bool checkGridGroups)
    {
        //For each column gets the up grids and moves them
        //Each non blasted piece calculated to move down
        //Each blasted piece calculated to move up and change color randomly
        //Check grid groups to set icons for once 
        var columnMoveNumbers = new List<int>();
        var gridLine = mostDownInColumn / _gridWidth;
        for (int i = 0; i < _gridHeight - gridLine; i++)
        {
            var upNumber = mostDownInColumn + i * _gridWidth;
            columnMoveNumbers.Add(upNumber);
        }
        var moveTime = 0.35f + blastedColumnGroup.Count * 0.05f;
        foreach (var number in columnMoveNumbers)
        {
            Tweener t;
            var piece = _gridPieceDictionary[number];
            if (blastedColumnGroup.Contains(number))
            {
                var moveUpCount = columnMoveNumbers.Count(x => x > number && !blastedColumnGroup.Contains(x));
                var oldY = piece.transform.position.y;
                var newGridNumber = number + moveUpCount * _gridWidth;
                t = piece.transform.DOMoveY(oldY + moveUpCount, moveTime)
                    .OnStart(() => piece.Enabled(Random.Range(0, _currentLevelData.ColorCount)))
                    .From(_spawnYPos)
                    .SetEase(moveEase)
                    .OnComplete(() => { _gridPieceDictionary[newGridNumber] = piece; });
            }
            else
            {
                var moveDownCount = columnMoveNumbers.Count(x => x < number && blastedColumnGroup.Contains(x));
                var newGridNumber = number - moveDownCount * _gridWidth;
                t = piece.transform.DOMoveY(-moveDownCount, moveTime)
                    .SetRelative(true)
                    .SetEase(moveEase)
                    .OnComplete(() => { _gridPieceDictionary[newGridNumber] = piece; });
            }
            _movePieceTweeners.Add(t);
        }
        if (checkGridGroups)
        {
            _totalGroupCount = 0;
            for (int i = 0; i < _gridCount; i++)
                StartCoroutine(CheckGroups(i, false));
        }
    }

    private bool MoveCompleted()
    {
        if (_movePieceTweeners.Any(t => t.IsActive())) return false;
        _movePieceTweeners.Clear();
        return true;
    }
}