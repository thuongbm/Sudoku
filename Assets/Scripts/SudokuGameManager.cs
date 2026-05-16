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
    public GameObject saveGamePanel;

    [Header("3 Save Slots Configuration")]
    public SaveSlotUI[] saveSlots = new SaveSlotUI[3]; // Clean dropdown array for slots 1, 2, and 3
    
    [Header("UI Setup")]
    public GameObject cellPrefab; // Sudoku cell prefab
    public Transform gridParent; // The object with the GridLayoutGroup
    
    [Header("Main menu References")]
    public UnityEngine.UI.Button loadGameButton;
    
    private SudokuCell[,] _allCells = new SudokuCell[9, 9];
    private int[,] _solution = new int[9, 9]; 
    private int[,] _puzzle = new int[9, 9]; 
    private SudokuCell _selectedCell;
    private int _currentDifficulty = 40; // Default to Medium
    private int _currentSlotIndex = 0; // Tracks which slot is currently playing
    
    private Stack<SudokuMove> _undoStack = new Stack<SudokuMove>();

    void Start()
    {
        // Start by only showing the Main Menu
        ShowPanel(mainMenuPanel); 
        CheckForSaveData();
        
        if (saveGamePanel != null)
        {
            saveGamePanel.SetActive(false);
        }
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

    // Call this helper method from your large "+" Plus Buttons first
    public void SelectSlotForNewGame(int slotIndex)
    {
        _currentSlotIndex = slotIndex; // Remember where we are starting the game
        OpenDifficultySelection();
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
        
        // MODULAR CALL: Call your isolated mathematical static calculation handler!
        SudokuGenerator.GeneratePuzzle(difficulty, out _solution, out _puzzle);
        
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

    #region Side Menu Functions

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

    public void CheckForSaveData()
    {
        // Automatically scan through all 3 save slots inside a loop
        for (int i = 0; i < 3; i++)
        {
            string path = Application.persistentDataPath + $"/save_{i}.json";
            SaveSlotUI slotUI = saveSlots[i];

            if (File.Exists(path))
            {
                //show object
                slotUI.emptyStateGroup.SetActive(false);
                slotUI.filledStateGroup.SetActive(true);
                
                //read file data to display
                string json = File.ReadAllText(path);
                SudokuSaveData data = JsonUtility.FromJson<SudokuSaveData>(json);

                int filledCells = 0;
                for (int j = 0; j < 81; j++)
                    if (data.currentValues[j] != 0)
                        filledCells++;
                int progressPercent = Mathf.RoundToInt((filledCells / (float)81) * 100);
                
                //Update the text
                slotUI.difficultyText.text = "Difficulty: " + GetDifficultyname(data.difficulty);
                slotUI.dateText.text = "Date: " + System.DateTime.Now.ToString("dd/MM/yyyy");
                slotUI.progressText.text = $"Progress: {progressPercent}%";
            }
            else
            {
                slotUI.emptyStateGroup.SetActive(true);
                slotUI.filledStateGroup.SetActive(false);
            }
        }
    }

    private string GetDifficultyname(int holes)
    {
        if (holes <= 30) return "Easy";
        if (holes <= 45) return "Medium";
        return "Hard";
    }

    // Call this from your trash icon buttons, passing the index (0, 1, or 2)
    public void DeleteSaveFileFromSlot(int slotIndex)
    {
        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Slot {slotIndex} save file deleted.");
        }
    
        // Refresh the layout right away to bring back the plus button!
        CheckForSaveData();
    }

    public void OpenSaveGameMenu()
    {
        if (saveGamePanel != null)
        {
            saveGamePanel.SetActive(true);
        }
        
        CheckForSaveData();
    }
    
    public void SaveGameToSlot(int slotIndex)
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
        // Dynamic path based on which slot button was clicked
        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        File.WriteAllText(path, json);
        Debug.Log($"Successfully saved game data into Slot {slotIndex} at: {path}");
        
        // Instantly refresh the cards 
        CheckForSaveData();
        
        // Auto close the selection screen box after writing data
        CloseSavePanel();
    }
    public void CloseSavePanel()
    {
        if (saveGamePanel != null)
        {
            saveGamePanel.SetActive(false);
        }
    }

    // Call this from your play/continue icon buttons, passing the index (0, 1, or 2)
    public void LoadGameFromSlot(int slotIndex)
    {
        _currentSlotIndex = slotIndex; // Keep track of the active slot context
        string path = Application.persistentDataPath + $"/save_{slotIndex}.json";
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        SudokuSaveData data = JsonUtility.FromJson<SudokuSaveData>(json);

        _currentDifficulty = data.difficulty;
        
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
        
        CloseSavePanel();
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
}