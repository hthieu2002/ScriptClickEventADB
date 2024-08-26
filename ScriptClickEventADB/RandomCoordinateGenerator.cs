using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptClickEventADB
{
    public class RandomCoordinateGenerator
    {
        private static readonly Random Random = new Random();

        public int GetRandomTapX(int min = 300, int max = 700)
        {
            return Random.Next(min, max + 1);
        }

        public int GetRandomTapY(int min = 800, int max = 1300)
        {
            return Random.Next(min, max + 1);
        }
        // Parameters to control the swipe length
        private const int SwipeLengthX = 100; // Maximum swipe length in X direction
        private const int SwipeLengthY = 100; // Maximum swipe length in Y direction

        public int GetRandomSwipeTapX(int min = 250, int max = 800)
        {
            return Random.Next(min, max + 1);
        }

        public int GetRandomSwipeTapY(int min = 650, int max = 800)
        {
            return Random.Next(min, max + 1);
        }

        public (int StartX, int StartY, int EndX, int EndY) GetRandomSwipeCoordinates(int minX = 250, int maxX = 800, int minY = 650, int maxY = 800)
        {
            // Generate start coordinates
            int startX = Random.Next(minX, maxX + 1);
            int startY = Random.Next(minY, maxY + 1);

            // Ensure the end coordinates are within a short range from the start coordinates
            int endX = Random.Next(Math.Max(minX, startX - SwipeLengthX), Math.Min(maxX, startX + SwipeLengthX) + 1);
            int endY = Random.Next(Math.Max(minY, startY - SwipeLengthY), Math.Min(maxY, startY + SwipeLengthY) + 1);

            return (startX, startY, endX, endY);
        }
    }
}
