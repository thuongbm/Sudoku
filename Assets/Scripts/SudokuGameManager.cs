using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SudokuGameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject difficultyPanel;
    public GameObject gamePanel;
    public GameObject congratsPanel;
    
    [Header("UI Setup")]
    public GameObject cellPrefab; // Sudoku cell prefab
    public Transform gridParent; // The object with the GridLayoutGroup
    
    private SudokuCell[,] _allCells = new SudokuCell[9, 9];
    private int[,] _solution = new int[9, 9]; 
    private int[,] _puzzle = new int[9, 9]; 
    private SudokuCell _selectedCell;
    private int _currentDifficulty = 40; // Default to Medium
    
    private Stack<SudokuMove> _undoStack = new Stack<SudokuMove>();
    private struct SudokuMove
    {
        public SudokuCell cell;
        public int preValue;
    }
    void Start()
    {
        // Start by only showing the Main Menu
        ShowPanel(mainMenuPanel); 
    }

    void Update()
    {
        // Navigation should work on any selected cell
        if (_selectedCell != null)
        {
            HandleNavigation();
        }
        
        // Input should only work on non-fixed cells
        if (_selectedCell != null && !_selectedCell.isFixed)
        {
            HandleInput();
        } 
    }

    #region Menu Navigation & UI Logic

    public void OpenDifficultySelection()
    {
        ShowPanel(difficultyPanel);
    }

    public void SetDifficulty(int numberOfHoles)
    {
        _currentDifficulty = numberOfHoles;
        Debug.Log("Difficulty set to: " + numberOfHoles + " holes.");
    }

    public void StartGame()
    {
        ShowPanel(gamePanel);
        //Clear history
        _undoStack.Clear();
        GenerateNewGame(_currentDifficulty);
    }

    public void BackToMenu()
    {
        ShowPanel(mainMenuPanel);
    }
    
    public void ShowPanel(GameObject panelToShow)
    {
        mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
        difficultyPanel.SetActive(panelToShow == difficultyPanel);
        gamePanel.SetActive(panelToShow == gamePanel);
        // Congrats panel is handled separately but closed when switching menus
        congratsPanel.SetActive(false);
    }
    
    #endregion
    
    #region Board Generation

    void GenerateNewGame(int difficulty)
    {
        // Clear existing board UI
        foreach(Transform child in gridParent) Destroy(child.gameObject);
        
        // 1. Generate full solution
        _solution = new int[9, 9];
        FillBoardRecursive(_solution, 0, 0);
        
        // 2. Clone solution to puzzle and remove numbers
        _puzzle = (int[,])_solution.Clone(); 
        RemoveNumbers(_puzzle, difficulty); 
        
        // 3. Spawn the UI cells
        CreateBoard();
    }

    void CreateBoard()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                GameObject go = Instantiate(cellPrefab, gridParent);
                SudokuCell cell = go.GetComponent<SudokuCell>();
                
                cell.Setup(_puzzle[r, c], this);
                cell.row = r;
                cell.col = c;
                _allCells[r, c] = cell;
            }
        }

        // Default selection to the top-left cell so navigation works immediately
        OnCellSelected(_allCells[0, 0]);
    }

    #endregion

    #region Interaction & Highlighting Logic

    public void OnCellSelected(SudokuCell selected)
    {
        _selectedCell = selected;

        // 1. Reset all cells to idle color
        foreach (var cell in _allCells)
        {
            cell.SetHighlight(false);
        }

        // 2. Highlight the main clicked cell
        _selectedCell.SetHighlight(true, true);

        // 3. Contextual Highlights
        if (_selectedCell.Value != 0)
        {
            // If cell has a number, highlight all cells with the SAME VALUE
            HighlightSameValues(_selectedCell.Value);
        }
        else
        {
            // If cell is empty, highlight its Row, Column, and 3x3 Block
            HighlightRelatedArea(_selectedCell.row, _selectedCell.col);
        }
    }

    void HighlightSameValues(int value)
    {
        foreach (SudokuCell cell in _allCells)
        {
            if (cell.Value == value)
            {
                cell.SetHighlight(true, false);
            }
        }
    }

    void HighlightRelatedArea(int row, int col)
    {
        int startRow = row - row % 3, startCol = col - col % 3;

        foreach (SudokuCell cell in _allCells)
        {
            bool sameRowOrCol = (cell.row == row) || (cell.col == col);
            bool samePart = (cell.row >= startRow && cell.row < startRow + 3 &&
                             cell.col >= startCol && cell.col < startCol + 3);

            if (sameRowOrCol || samePart)
            {
                cell.SetHighlight(true, false);
            }
        }
    }

    #region Side Menu Funcions

    public void UndoMove()
    {
        if (_undoStack.Count > 0)
        {
            SudokuMove lastMove = _undoStack.Pop();
            lastMove.cell.Value = lastMove.preValue;
            OnCellSelected(lastMove.cell);
        }
    }

    public void EraseCell()
    {
        if (_selectedCell != null && !_selectedCell.isFixed && _selectedCell.Value != 0)
        {
            RecordMove(_selectedCell, _selectedCell.Value);
            _selectedCell.Value = 0;
            OnCellSelected(_selectedCell);
        }
    }

    public void SaveGame()
    {
        SudokuSaveData data = new SudokuSaveData();
        data.difficulty = _currentDifficulty;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int i = r * 9 + c;
                data.currentValues[i] = _allCells[r, c].Value;
                data.fixedStatus[i] = _allCells[r, c].isFixed;
                data.solutionValues[i] = _solution[r, c];
            }
        }
        
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Application.persistentDataPath + "/save.json", json);
        Debug.Log("Saved to: " + Application.persistentDataPath);
    }

    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/save.json";
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        SudokuSaveData data = JsonUtility.FromJson<SudokuSaveData>(json);

        ShowPanel(gamePanel);
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int i = r * 9 + c;
                _solution[r, c] = data.solutionValues[i];
                GameObject go = Instantiate(cellPrefab, gridParent);
                SudokuCell cell = go.GetComponent<SudokuCell>();
                cell.Setup(data.currentValues[i], this);
                cell.isFixed = data.fixedStatus[i];
                cell.row = r;
                cell.col = c;
                _allCells[r, c] = cell;
            }
        }
        OnCellSelected(_allCells[0, 0]);
    }
    
    public void QuitToMenu()
    {
        _undoStack.Clear();
        ShowPanel(mainMenuPanel);
    }
    
    public void ChangeTheme()
    {
        // Simple toggle example: you could swap background colors here
        Debug.Log("Theme Changed!");
    }
    
    private void RecordMove(SudokuCell cell, int oldValue)
    {
        _undoStack.Push(new SudokuMove { cell = cell, preValue = oldValue });
    }

    #endregion
    
    #endregion

    #region Input & Navigation Handling

    void HandleInput()
    {
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()) || Input.GetKeyDown("[" + i + "]"))
            {
                RecordMove(_selectedCell, _selectedCell.Value);
                _selectedCell.Value = i;
                // Refresh highlights to show all cells with this new number
                OnCellSelected(_selectedCell); 
                CheckWin();
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            RecordMove(_selectedCell, _selectedCell.Value);
            _selectedCell.Value = 0;
            // Return to area highlights since cell is now empty
            OnCellSelected(_selectedCell); 
        }
    }

    void HandleNavigation()
    {
        int newRow = _selectedCell.row;
        int newCol = _selectedCell.col;

        // Direction Fix: Down increases row, Up decreases row
        if (Input.GetKeyDown(KeyCode.DownArrow)) newRow++;
        if (Input.GetKeyDown(KeyCode.UpArrow)) newRow--;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) newCol--;
        if (Input.GetKeyDown(KeyCode.RightArrow)) newCol++;

        // Wrap-around math (keeps index 0-8)
        newRow = (newRow + 9) % 9;
        newCol = (newCol + 9) % 9;

        // Logic Fix: Use OR (||) so it triggers if either row or col changes
        if (newRow != _selectedCell.row || newCol != _selectedCell.col)
        {
            OnCellSelected(_allCells[newRow, newCol]);
        }
    }
    
    void CheckWin()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (_allCells[r, c].Value != _solution[r, c]) return;
            }
        }
        
        congratsPanel.SetActive(true);
    }

    #endregion

    #region Backtracking Algorithm

    bool FillBoardRecursive(int[,] grid, int row, int col)
    {
        if (col >= 9) { row++; col = 0; }
        if (row >= 9) return true;

        List<int> nums = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        // Shuffle numbers for unique board generation
        for (int i = 0; i < nums.Count; i++) {
            int temp = nums[i];
            int rand = Random.Range(i, nums.Count);
            nums[i] = nums[rand];
            nums[rand] = temp;
        }

        foreach (int n in nums) 
        {
            if (IsSafe(grid, row, col, n))
            {
                grid[row, col] = n;
                if (FillBoardRecursive(grid, row, col + 1)) return true;
                grid[row, col] = 0;
            }
        }
        return false;
    }

    bool IsSafe(int[,] grid, int row, int col, int num)
    {
        for (int i = 0; i < 9; i++)
        {
            if (grid[row, i] == num || grid[i, col] == num) return false;
        }

        int sRow = row - row % 3, sCol = col - col % 3;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (grid[i + sRow, j + sCol] == num) return false;

        return true;
    }
    
    void RemoveNumbers(int[,] grid, int count)
    {
        while (count > 0)
        {
            int r = Random.Range(0, 9);
            int c = Random.Range(0, 9);
            if (grid[r, c] != 0)
            {
                grid[r, c] = 0;
                count--;
            }
        }
    }

    #endregion

    [System.Serializable]
    public class SudokuSaveData
    {
        public int[] currentValues = new int[81];
        public bool[] fixedStatus = new bool[81];
        public int[] solutionValues = new int[81];
        public int difficulty;
    }
}