using System.Collections.Generic;
using UnityEngine;

public class SudokuGameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject difficultyPanel;
    public GameObject gamePanel;
    public GameObject congratsPanel;
    
    [Header("UI Setup")]
    public GameObject cellPrefab; // Sudoku cell
    public Transform gridParent; // The object with the GridLayoutGroup
    
    private SudokuCell[,] _allCells = new SudokuCell[9, 9];
    private int[,] _solution = new int[9, 9]; // the full solved board
    private int[,] _puzzle = new int[9, 9]; // the board with holes
    private SudokuCell _selectedCell;
    private int _currentDifficulty = 40;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowPanel(mainMenuPanel); 
    }

    // Update is called once per frame
    void Update()
    {
        if (_selectedCell != null)
        {
            HandleNavigation();
        }
        
        if (_selectedCell != null && !_selectedCell.isFixed)
        {
            HandleInput();
        } 
    }

    #region Menu Navigation

    public void OpenDifficultySelection()
    {
        ShowPanel(difficultyPanel);
    }

    public void StartGame()
    {
        ShowPanel(gamePanel);
        GenerateNewGame(_currentDifficulty); // depend on _currerntDifficulty
    }

    public void SetDifficulty(int numberOfHoles)
    {
        _currentDifficulty = numberOfHoles;
        Debug.Log("Difficulty changed to: " + numberOfHoles + " empty cells.");
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
        congratsPanel.SetActive(false);
    }
    
    #endregion
    
    void GenerateNewGame(int difficulty)
    {
        // clear existing board if any
        foreach(Transform child in gridParent) Destroy(child.gameObject);
        
        // genarate a full valid Sudoku solution
        _solution = new int[9, 9];
        FillBoardRecursive(_solution, 0, 0);
        
        // copy solution and remove numbers
        _puzzle = (int[,])_solution.Clone(); // Copy the full board first
        RemoveNumbers(_puzzle, difficulty);  // Now poke holes in the copy
        
        //spawn UI
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
    }
    #region Backtracking Logic
    bool FillBoardRecursive(int[,] grid, int row, int col)
    {
        if (col >= 9) { row++; col = 0; }
        if (row >= 9) return true;

        // Try numbers in random order for a unique board every time
        List<int> nums = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
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
    
    #region Interaction & Win Logic
    public void OnCellSelected(SudokuCell selected)
    {
        _selectedCell = selected;

        // 1. Clear ALL highlights first
        foreach (var cell in _allCells)
        {
            cell.SetHighlight(false);
        }

        // 2. Set the main selection
        _selectedCell.SetHighlight(true, true);

        // 3. Highlight related cells
        if (_selectedCell.Value != 0)
        {
            // Highlight all cells with the SAME VALUE
            HighlightSameValues(_selectedCell.Value);
        }
        else
        {
            // Highlight Row, Column, and 3x3 Part
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
            //Same row or same column
            bool sameRow = (cell.row == row) || (cell.col == col);
            
            //Same 3*3 part
            bool samePart = (cell.row >= startRow && cell.row < startRow + 3 &&
                             cell.col >= startCol && cell.col < startCol + 3);

            if (sameRow || samePart)
            {
                cell.SetHighlight(true, false);
            }
        }
    }
    
    void HandleInput()
    {
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()) || Input.GetKeyDown("[" + i + "]"))
            {
                _selectedCell.Value = i;
                CheckWin();
            }
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) _selectedCell.Value = 0;
    }

    void HandleNavigation()
    {
        int newRow = _selectedCell.row;
        int newCol = _selectedCell.col;

        if (Input.GetKeyDown(KeyCode.DownArrow)) newRow++;
        if (Input.GetKeyDown(KeyCode.UpArrow)) newRow--;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) newCol--;
        if (Input.GetKeyDown(KeyCode.RightArrow)) newCol++;

        //Wrap-around math 
        newRow = (newRow + 9) % 9;
        newCol = (newCol + 9) % 9;

        if (newRow != _selectedCell.row || newCol != _selectedCell.col)
        {
            OnCellSelected(_allCells[newRow, newCol]);
        }
    }
    
    void CheckWin()
    {
        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
            if (_allCells[r, c].Value != _solution[r, c]) return;
        
        congratsPanel.SetActive(true);
    }
    #endregion
}
