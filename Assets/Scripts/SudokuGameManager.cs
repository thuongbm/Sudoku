using System.Collections.Generic;
using UnityEngine;

public class SudokuGameManager : MonoBehaviour
{
    [Header("UI Setup")]
    public GameObject cellPrefab; // Sudoku cell
    public Transform gridParent; // The object with the GridLayoutGroup
    public GameObject congratsPanel;
    
    private SudokuCell[,] _allCells = new SudokuCell[9, 9];
    private int[,] _solution = new int[9, 9]; // the full solved board
    private int[,] _puzzle = new int[9, 9]; // the board with holes
    private SudokuCell _selectedCell;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateNewGame(1); // 40 empty cells
    }

    // Update is called once per frame
    void Update()
    {
        if (_selectedCell != null && !_selectedCell.isFixed)
        {
            HandleInput();
        } 
    }
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
    public void OnCellSelected(SudokuCell cell)
    {
        if (_selectedCell != null) _selectedCell.SetSelected(false);
        _selectedCell = cell;
        _selectedCell.SetSelected(true);
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

    void CheckWin()
    {
        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
            if (_allCells[r, c].Value != _solution[r, c]) return;

        congratsPanel.SetActive(true);
    }
    #endregion
}
