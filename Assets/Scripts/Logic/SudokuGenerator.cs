using System.Collections.Generic;
using UnityEngine;

public static class SudokuGenerator
{
    public static void GeneratePuzzle(int difficulty, out int[,] solution, out int[,] puzzle)
    {
        solution = new int[9, 9];
        FillBoardRecursive(solution, 0, 0);

        puzzle = (int[,])solution.Clone(); 
        RemoveNumbers(puzzle, difficulty); 
    }

    private static bool FillBoardRecursive(int[,] grid, int row, int col)
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

    private static bool IsSafe(int[,] grid, int row, int col, int num)
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
    
    private static void RemoveNumbers(int[,] grid, int count)
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
}