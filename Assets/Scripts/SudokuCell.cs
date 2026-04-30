using UnityEngine;
using UnityEngine.UI;

public class SudokuCell : MonoBehaviour
{
    [Header("UI Components")]
    public Image cellBackground;    // The Button's Image component
    public Image numberDisplay;     // The Image for 1-9 sprites
    public GameObject ticksParent;  // A container for the 9 small tick images
    public Image[] tickImages;      // Array of 9 small images for Ticks

    [Header("Sprites from your Project")]
    public Sprite[] mainNumbers;    // Drag sprites 1-9 here
    public Sprite[] tickNumbers;    // Drag sprites Tick1-Tick9 here

    [Header("Colors for State")]
    public Color selectedColor = new Color(1, 1, 1, 0.5f); // Semi-transparent white
    public Color hintColor = new Color(1, 1, 1, 0.2f);    // Semi-transparent red
    public Color idleColor = new Color(0, 0, 0, 0);       // Completely transparent
    
    [HideInInspector] public int row, col;

    private int _value = 0;
    public bool isFixed = false;
    private SudokuGameManager _manager;

    public int Value {
        get => _value;
        set { _value = value; RefreshUI(); }
    }

    public void Setup(int val, SudokuGameManager mgr) {
        _manager = mgr;
        isFixed = (val != 0);
        Value = val;
        SetSelected(false); // make it transparent at the start
    }

    private void RefreshUI() {
        if (Value == 0) {
            numberDisplay.enabled = false;
            ticksParent.SetActive(true); // Show pencil marks if empty
        } else {
            numberDisplay.enabled = true;
            numberDisplay.sprite = mainNumbers[Value - 1];
            ticksParent.SetActive(false); // Hide pencil marks if filled
        }
    }

    public void SetSelected(bool isSelected) {
        cellBackground.color = isSelected ? selectedColor : idleColor;
    }
    
    //handle difference highlight types
    public void SetHighlight(bool isHighlighted, bool isMainSelection = false)
    {
        if (!isHighlighted) {
            cellBackground.color = idleColor;
        } else {
            cellBackground.color = isMainSelection ? selectedColor : hintColor;
        }
    }
    
    //button can trigger a selection
    public void OnCellClicked() 
    {
        _manager.OnCellSelected(this);
    }
}