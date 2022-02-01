using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelData
{
    public int GridWidth;
    public int GridHeight;
    public int ColorCount;

    public List<int> GroupLimits;
    //Would add level number, grid pieces or any level detail here
}

//This is where data would be saved and read
public static class LevelDataHandler
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPRSQTVYZ";
    private static List<LevelData> _levelDataList;

    public static List<LevelData> ReadLevelData()
    {
        _levelDataList = new List<LevelData>();
        var levelsDefault = Resources.LoadAll<TextAsset>("Levels");
        var levelTextAssets = levelsDefault
            .Where(x => x.name.Contains("Level") && !x.name.Contains("meta")).ToArray();

        foreach (var t in levelTextAssets)
        {
            var result = t.text.Split('\n');

            var gridWidth = result[0].Replace("M:", "");
            var gridHeight = result[1].Replace("N:", "");
            var colorCount = result[2].Replace("K:", "");

            var limitList = new List<int>();
            for (int i = 3; i < result.Length; i++)
            {
                var old=Alphabet[i - 3] + ":";
                var limit = result[i].Replace(old, "");
                limitList.Add(int.Parse(limit));
            }
            var levelData = new LevelData()
            {
                GridWidth = int.Parse(gridWidth),
                GridHeight = int.Parse(gridHeight),
                ColorCount = int.Parse(colorCount),
                GroupLimits = limitList
            };
            _levelDataList.Add(levelData);
        }

        return _levelDataList;
    }
}