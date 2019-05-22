using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Matrix<T> : IEnumerable<T>
{
    T[,] matrix;

    public Matrix(int width, int height)
    {
        matrix = new T[width, height];
    }

    public Matrix(T[,] copyFrom)
    {
        matrix = copyFrom;
    }

    public Matrix<T> Clone()
    {
        return new Matrix<T>((T[,])matrix.Clone());
    }

    public void SetRangeTo(int x0, int y0, int x1, int y1, T item)
    {
        if (x0 < Width - 1 || x0 > Width - 1 || y0 < Height - 1 || y0 > Height - 1) throw new UnityException("Some index is out of range");
        if (x0 > x1 || y0 > y1) throw new UnityException("Some starter index is greater than some ending index");

        var start = x0 + y0 * Width; var end = x1 + y1 * Width;
        for (var i = start; i <= end - start; i++) matrix[i % Width, i / Width] = item;
    }

    public List<T> GetRange(int x0, int y0, int x1, int y1)
    {
        List<T> l = new List<T>();

        for (int i = y0; i < y1; i++)
        {
            for (int j = x0; j < x1; j++)
            {
                l.Add(matrix[j, i]); 
            }
        }

        return l;
    }

    public T this[int x, int y]
    {
        get
        {
            return matrix[x, y];
        }
        set
        {
            matrix[x, y] = value;
        }
    }

    public int Width { get { return matrix.GetLength(0); } }

    public int Height { get { return matrix.GetLength(1); } }

    public int Capacity { get { return matrix.Length; } }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in matrix) yield return item;
    }
}