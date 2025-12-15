using System;
using System.Collections.Generic;
using System.IO;

namespace MagniSnap
{
    public class PixelGraph
    {
        private RGBPixel[,] ImageMatrix;
        private int Width;
        private int Height;

        public PixelGraph(RGBPixel[,] image)
        {
            ImageMatrix = image;
            Width = ImageToolkit.GetWidth(image);
            Height = ImageToolkit.GetHeight(image);
        }

        /// <summary>
        /// Returns the valid neighbors of pixel (x,y) with the calculated weight
        /// </summary>
        public List<(int nx, int ny, double weight)> GetNeighbors(int x, int y)
        {
            List<(int nx, int ny, double weight)> neighbors =
                new List<(int, int, double)>();

            Vector2D e = ImageToolkit.CalculatePixelEnergies(x, y, ImageMatrix);

            // RIGHT neighbor
            if (x + 1 < Width)
            {
                double w = 1.0 / Math.Max(e.X, 1e-9);
                neighbors.Add((x + 1, y, w));
            }

            // DOWN neighbor
            if (y + 1 < Height)
            {
                double w = 1.0 / Math.Max(e.Y, 1e-9);
                neighbors.Add((x, y + 1, w));
            }

            // LEFT neighbor (use left pixel's X energy)
            if (x - 1 >= 0)
            {
                Vector2D left = ImageToolkit.CalculatePixelEnergies(x - 1, y, ImageMatrix);
                double w = 1.0 / Math.Max(left.X, 1e-9);
                neighbors.Add((x - 1, y, w));
            }

            // UP neighbor (use upper pixel's Y energy)
            if (y - 1 >= 0)
            {
                Vector2D up = ImageToolkit.CalculatePixelEnergies(x, y - 1, ImageMatrix);
                double w = 1.0 / Math.Max(up.Y, 1e-9);
                neighbors.Add((x, y - 1, w));
            }

            return neighbors;
        }
        public void ExportGraph(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int index = 0;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        sw.WriteLine($"The index node {index}");
                        sw.WriteLine("Edges");

                        var neighbors = GetNeighbors(x, y);

                        foreach (var n in neighbors)
                        {
                            int neighborIndex = n.ny * Width + n.nx;
                            sw.WriteLine(
                                $"edge from {index} To {neighborIndex} With Weights {n.weight}"
                            );
                        }

                        index++;
                    }
                }
            }
        }

    }
}
