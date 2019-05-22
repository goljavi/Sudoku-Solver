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
        for (int i = y0; i < y1; i++)
        {
            for (int j = x0; j < x1; j++)
            {
                matrix[j, i] = item;
            }
        }
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