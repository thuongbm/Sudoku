using UnityEngine;

// --- Undo System Structure ---
public struct SudokuMove
{
    public SudokuCell cell;
    public int preValue;
}

// --- Hard Drive File Layout ---
[System.Serializable]
public class SudokuSaveData
{
    public int[] currentValues = new int[81];
    public bool[] fixedStatus = new bool[81];
    public int[] solutionValues = new int[81];
    public int difficulty;
}

// --- Main Menu Card Configuration ---
[System.Serializable]
public class SaveSlotUI
{
    public GameObject emptyStateGroup;
    public GameObject filledStateGroup;
    public TMPro.TMP_Text difficultyText;
    public TMPro.TMP_Text dateText;
    public TMPro.TMP_Text progressText;
}