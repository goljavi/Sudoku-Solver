using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Sudoku : MonoBehaviour {
    public int quadrantSize = 3;
	public Cell prefabCell;
	public Canvas canvas;
	public Text feedback;
	public float stepDuration = 0.05f;
	[Range(1, 82)]public int difficulty = 40;

	Matrix<Cell> _board;
    Matrix<int> _createdMatrix;
    List<int> posibles = new List<int>();
	int _smallSide;
	int _bigSide;
    string memory = "";
    string canSolve = "";
    bool canPlayMusic = false;
    List<int> nums = new List<int>();



    float r = 1.0594f;
    float frequency = 440;
    float gain = 0.5f;
    float increment;
    float phase;
    float samplingF = 48000;


    void Start()
    {
        long mem = System.GC.GetTotalMemory(true);
        feedback.text = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        memory = feedback.text;
        _smallSide = quadrantSize;
        _bigSide = _smallSide * quadrantSize;
        frequency = frequency * Mathf.Pow(r, 2);
        CreateEmptyBoard();
        ClearBoard();
    }

    void ClearBoard() {
		_createdMatrix = new Matrix<int>(_bigSide, _bigSide);
		foreach(var cell in _board) {
			cell.number = 0;
			cell.locked = cell.invalid = false;
		}
	}

	void CreateEmptyBoard() {
		float spacing = 68f;
		float startX = -spacing * 4f;
		float startY = spacing * 4f;

		_board = new Matrix<Cell>(_bigSide, _bigSide);
		for(int x = 0; x<_board.Width; x++) {
			for(int y = 0; y<_board.Height; y++) {
                var cell = _board[x, y] = Instantiate(prefabCell);
				cell.transform.SetParent(canvas.transform, false);
				cell.transform.localPosition = new Vector3(startX + x * spacing, startY - y * spacing, 0);
            }
		}
	}
	


	int watchdog = 0;

    int ResolveBrute(Matrix<int> matrixParent, out Matrix<int> solution)
    {
        var solved = false;
        var intentos = 0;
        solution = new Matrix<int>(0, 0);

        while (!solved)
        {
            intentos++;
            solution = matrixParent.Clone();
            var breakFree = false;

            for (int i = 0; i < _board.Height; i++)
            {
                var rand = GenerateRandomNums();
                for (int j = 0; j < _board.Width; j++)
                {
                    if(matrixParent[i, j] == 0)
                    {
                        if(CanPlaceValue(solution, rand[0], i, j))
                        {
                            solution[i, j] = rand[0];
                            rand.RemoveAt(0);
                        }
                        else
                        {
                            breakFree = true;
                            break;
                        }
                    }
                }

                if (breakFree) break;
                if (i == _board.Height - 1) solved = true;
            }
        }

        return intentos;
    }

    bool RecuSolve(Matrix<int> matrixParent, out List<Matrix<int>> solution)
    {
        var solutionList = new List<Matrix<int>>();
        solutionList.Add(_createdMatrix.Clone());
        var solve = RecuSolve(0, 0, 0, solutionList);
        solution = solutionList;
        return solve;
    }

    bool RecuSolve(int x, int y, int protectMaxDepth, List<Matrix<int>> solution)
    {
        if (y > _board.Height-1) return true;

        // Si el casillero esta vacío lo resuelvo, si ya tenia un valor cargado de antes lo salteo (porque está bloqueado)
        if (solution[solution.Count - 1][x, y] == 0)
        {
            var rand = GenerateRandomNums();
            for (int i = 0; i < rand.Count; i++)
            {
                if (CanPlaceValue(solution[solution.Count - 1], rand[i], x, y))
                {
                    var newSolution = solution[solution.Count - 1].Clone();
                    newSolution[x, y] = rand[i];
                    solution.Add(newSolution);

                    var newCoordinates = GenerateNewCoordinates(x, y);

                    // Si RecuSolve llega a dar false tengo que probar con el proximo numero en mi lista random
                    if (RecuSolve(newCoordinates[0], newCoordinates[1], protectMaxDepth, solution))
                    {
                        return true;
                    }
                    else
                    {
                        newSolution[x, y] = 0;
                        solution.Add(newSolution);
                    }
                }
            } 
        }
        else
        {
            var newCoordinates = GenerateNewCoordinates(x, y);
            if (RecuSolve(newCoordinates[0], newCoordinates[1], protectMaxDepth, solution)) return true;
        }

        //Se me terminaron los random numbers! Hay que hacer backtracking
        return false;
    }

    int[] GenerateNewCoordinates(int x, int y)
    {
        var newRow = x;
        var newCol = y;
        if (newRow == _board.Height - 1) { newCol++; newRow = 0; }
        else newRow++;

        return new int[2] { newRow, newCol };
    }


    void OnAudioFilterRead(float[] array, int channels)
    {
        if(canPlayMusic)
        {
            increment = frequency * Mathf.PI / samplingF;
            for (int i = 0; i < array.Length; i++)
            {
                phase = phase + increment;
                array[i] = (float)(gain * Mathf.Sin((float)phase));
            }
        }
        
    }
    void changeFreq(int num)
    {
        frequency = 440 + num * 80;
    }

	//IMPLEMENTAR - punto 3
	IEnumerator ShowSequence(List<Matrix<int>> seq)
    {
        var total = seq.Count;
        var paso = 0;
        foreach (var item in seq)
        {
            paso++;
            TranslateAllValues(item);
            LockValuesToSolve();
            feedback.text = "Pasos: " + paso + "/" + total + " - " + memory + " - " + canSolve;
            changeFreq(Random.Range(0, 50));
            yield return new WaitForSeconds(0);
        }
        canPlayMusic = false;
    }

	void Update () {
		if(Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1))
            SolvedSudoku();
        else if (Input.GetKeyDown(KeyCode.C) || Input.GetMouseButtonDown(0))
            CreateSudoku();
        else if (Input.GetKeyDown(KeyCode.B))
            SolvedBruteSudoku();
    }

    void SolvedBruteSudoku()
    {
        StopAllCoroutines();
        Matrix<int> solution;
        watchdog = 100000;
        var result = ResolveBrute(_createdMatrix, out solution);
        long mem = System.GC.GetTotalMemory(true);
        memory = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        feedback.text = "Pasos: " + result + " - " + memory;
        TranslateAllValues(solution);
        LockValuesToSolve();
    }

    void SolvedSudoku()
    {
        StopAllCoroutines();
        List<Matrix<int>> solution;
        watchdog = 100000;
        var result = RecuSolve(_createdMatrix, out solution);

        long mem = System.GC.GetTotalMemory(true);
        memory = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        canSolve = result ? " VALID" : " INVALID";
        canPlayMusic = true;
        StartCoroutine(ShowSequence(solution));
    }

    void CreateSudoku()
    {
        StopAllCoroutines();
        ClearBoard();
        List<Matrix<int>> solution;
        watchdog = 100000;
        var result = RecuSolve(_createdMatrix, out solution);

        long mem = System.GC.GetTotalMemory(true);
        memory = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        canSolve = result ? " VALID" : " INVALID";
        canPlayMusic = true;
        StartCoroutine(ShowSequence(solution));
        _createdMatrix = solution[solution.Count - 1];
        LockRandomCells();
        ClearUnlocked(_createdMatrix);
        TranslateAllValues(_createdMatrix);
    }


    List<int> GenerateRandomNums()
    {
        List<int> list = new List<int>();
        for (int i = 0; i < _board.Width; i++)
        {
             list.Add(i + 1);
        }

        int listAux = 0;
        for (int j = 0; j < list.Count; j++)
        {
            int r = 1 + Random.Range(j, list.Count);
            listAux = list[r - 1];
            list[r - 1] = list[j];
            list[j] = listAux;
        }

        return list;
    }


    void ClearUnlocked(Matrix<int> mtx)
	{
		for (int i = 0; i < _board.Height; i++) {
			for (int j = 0; j < _board.Width; j++) {
				if (!_board [j, i].locked)
					mtx[j,i] = Cell.EMPTY;
			}
		}
	}

	void LockRandomCells()
	{
		List<Vector2> posibles = new List<Vector2> ();
		for (int i = 0; i < _board.Height; i++) {
			for (int j = 0; j < _board.Width; j++) {
				if (!_board [j, i].locked)
					posibles.Add (new Vector2(j,i));
			}
		}
		for (int k = 0; k < (_board.Capacity+1)-difficulty; k++) {
			int r = Random.Range (0, posibles.Count);
			_board [(int)posibles [r].x, (int)posibles [r].y].locked = true;
			posibles.RemoveAt (r);
		}
	}

    void TranslateAllValues(Matrix<int> matrix)
    {
        for (int y = 0; y < _board.Height; y++)
        {
            for (int x = 0; x < _board.Width; x++)
            {
                _board[x, y].number = matrix[x, y];
            }
        }
    }

    void LockValuesToSolve()
    {
        for (int y = 0; y < _board.Height; y++)
        {
            for (int x = 0; x < _board.Width; x++)
            {
                if (_createdMatrix[x, y] > 0) _board[x, y].locked = true;
            }
        }
    }

    void TranslateSpecific(int value, int x, int y)
    {
        _board[x, y].number = value;
    }

    void TranslateRange(int x0, int y0, int xf, int yf)
    {
        for (int x = x0; x < xf; x++)
        {
            for (int y = y0; y < yf; y++)
            {
                _board[x, y].number = _createdMatrix[x, y];
            }
        }
    }

    void CreateNew()
    {
        _createdMatrix = new Matrix<int>(Tests.validBoards[10]);
        TranslateAllValues(_createdMatrix);
    }

    bool CanPlaceValue(Matrix<int> mtx, int value, int x, int y)
    {
        if (quadrantSize > 3) return CanPlaceValue2(mtx, value, x, y);
        List<int> fila = new List<int>();
        List<int> columna = new List<int>();
        List<int> area = new List<int>();
        List<int> total = new List<int>();

        Vector2 cuadrante = Vector2.zero;

        for (int i = 0; i < mtx.Height; i++)
        {
            for (int j = 0; j < mtx.Width; j++)
            {
                if (i != y && j == x) columna.Add(mtx[j, i]);
                else if (i == y && j != x) fila.Add(mtx[j, i]);
            }
        }



        cuadrante.x = (int)(x / quadrantSize);

        if (x < quadrantSize)
            cuadrante.x = 0;
        else if (x < quadrantSize * 2)
            cuadrante.x = quadrantSize;
        else
            cuadrante.x = quadrantSize * 2;

        if (y < quadrantSize)
            cuadrante.y = 0;
        else if (y < quadrantSize * 2)
            cuadrante.y = quadrantSize;
        else
            cuadrante.y = quadrantSize * 2;

        area = mtx.GetRange((int)cuadrante.x, (int)cuadrante.y, (int)cuadrante.x + quadrantSize, (int)cuadrante.y + quadrantSize);
        total.AddRange(fila);
        total.AddRange(columna);
        total.AddRange(area);
        total = FilterZeros(total);

        if (total.Contains(value))
            return false;
        else
            return true;
    }


    List<int> FilterZeros(List<int> list)
    {
        List<int> aux = new List<int>();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != 0) aux.Add(list[i]);
        }
        return aux;
    }


    bool CanPlaceValue2(Matrix<int> mtx, int value, int x, int y)
    {
        // row & column
        for (int i = 0; i < _board.Width; i++) if (i != x && mtx[i, y] == value || i != y && mtx[x, i] == value) return false;
        
        // quadrant
        int srow = x / quadrantSize * quadrantSize;
        int scol = y / quadrantSize * quadrantSize;
        for (int contador_row = srow; contador_row < srow + quadrantSize; contador_row++)
            for (int contador_col = scol; contador_col < scol + quadrantSize; contador_col++)
                if (!(contador_row == x && contador_col == y))
                    if (mtx[contador_row, contador_col] == value)
                        return false;

        return true;
    }
}
