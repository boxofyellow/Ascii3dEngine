using System;

namespace Ascii3dEngine
{
    public struct  Label
    {
        public Label(int column, int row, char character) 
            : this(column, row, character, ConsoleColor.Red, ConsoleColor.Black) { }

        public Label(int column, int row, char character, ConsoleColor foreground, ConsoleColor background)
        {
            Column = column;
            Row = row;
            Character = character;
            Foreground = foreground;
            Background = background;
        }

        public readonly int Column;
        public readonly int Row;
        public readonly char Character;
        public readonly ConsoleColor Foreground;
        public readonly ConsoleColor Background;
    }
}