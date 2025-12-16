using MagniSnap;
using System.Collections.Generic;

public class ShortestPath
{
    private int width, height;
    private double[,] dist;
    private (int x, int y)[,] parent;

    private readonly int[] dx = { 1, -1, 0, 0 };
    private readonly int[] dy = { 0, 0, 1, -1 };

    public ShortestPath(int w, int h)
    {
        width = w;
        height = h;
        dist = new double[w, h];
        parent = new (int x, int y)[w, h];
    }

    public (List<(int x, int y)> path, (int x, int y)[,] parent, double[,] dist) Dijkstra(
      (int x, int y) anchor,
      (int x, int y) destination,
      PixelGraph graph)
    {
        // Initialize distance and parent arrays
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                dist[i, j] = double.MaxValue;
                parent[i, j] = (-1, -1);
            }

        dist[anchor.x, anchor.y] = 0;
        parent[anchor.x, anchor.y] = (-1, -1);

        var pq = new SortedSet<(double dist, int x, int y)>(Comparer<(double dist, int x, int y)>.Create((a, b) =>
        {
            int cmp = a.dist.CompareTo(b.dist);
            if (cmp != 0) return cmp;
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        }));

        pq.Add((dist: 0, x: anchor.x, y: anchor.y));

        while (pq.Count > 0)
        {
            var current = pq.Min;
            pq.Remove(current);

            int ux = current.x;
            int uy = current.y;
            if (ux == destination.x && uy == destination.y)
                break;

            if (current.dist > dist[ux, uy])
                continue;

            foreach (var n in graph.GetNeighbors(ux, uy))
            {
                int nx = n.nx;
                int ny = n.ny;
                double w = n.weight;

                double newDist = dist[ux, uy] + w;

                if (newDist < dist[nx, ny])
                {
                    dist[nx, ny] = newDist;
                    parent[nx, ny] = (ux, uy);
                    pq.Add((newDist, nx, ny));
                }
            }
        }

        // Backtrack to find the path
        List<(int x, int y)> path = new List<(int, int)>();
        (int x, int y) currentPoint = destination;

        // Backtrack until the parent is (-1, -1), which is the anchor
        while (parent[currentPoint.x, currentPoint.y] != (-1, -1))
        {
            path.Insert(0, currentPoint);  // Insert at the beginning to get the path in correct order
            currentPoint = parent[currentPoint.x, currentPoint.y];
        }

        // Add the anchor point at the start of the path
        path.Insert(0, anchor);

        return (path, parent, dist);
    }

}
