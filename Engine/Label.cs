namespace Ascii3dEngine
{
    public struct  Label
    {
        public Label(int column, int row, char character)
        {
            Column = column;
            Row = row;
            Character = character;
        }

        public readonly int Column;
        public readonly int Row;
        public readonly char Character;
    }
}