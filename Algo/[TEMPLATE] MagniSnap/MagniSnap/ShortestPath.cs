using System;
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
    public ((int x, int y)[,] parent, double[,] dist) Dijkstra(
        (int x, int y) anchor,
        Func<(int x, int y), (int x, int y), double> weight)
    {
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                dist[i, j] = double.MaxValue;
                parent[i, j] = (-1, -1);
            }

        dist[anchor.x, anchor.y] = 0;
        parent[anchor.x, anchor.y] = (-1, -1);

        var pq = new SortedSet<(double dist, int x, int y)>(
            Comparer<(double dist, int x, int y)>.Create((a, b) =>
            {
                int cmp = a.dist.CompareTo(b.dist);
                if (cmp != 0) return cmp;
                if (a.x != b.x) return a.x.CompareTo(b.x);
                return a.y.CompareTo(b.y);
            })
        );

        pq.Add((dist: 0, x: anchor.x, y: anchor.y));

        while (pq.Count > 0)
        {
            var current = pq.Min;
            pq.Remove(current);

            int ux = current.x;
            int uy = current.y;
            if (current.dist > dist[ux, uy])
                continue;

            for (int k = 0; k < 4; k++)
            {
                int nx = ux + dx[k];
                int ny = uy + dy[k];

                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    continue;

                double w = weight((ux, uy), (nx, ny));
                double newDist = dist[ux, uy] + w;

                if (newDist < dist[nx, ny])
                {
                    dist[nx, ny] = newDist;
                    parent[nx, ny] = (ux, uy);
                    pq.Add((dist: newDist, x: nx, y: ny));
                }
            }
        }

        return (parent, dist);
    }
}
