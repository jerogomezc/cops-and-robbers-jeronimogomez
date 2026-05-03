using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        // Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        // 1️⃣ Inicializar a 0
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        // 2️⃣ Rellenar adyacencias (arriba, abajo, izquierda, derecha)
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int row = i / 8;
            int col = i % 8;

            // Arriba
            if (row > 0)
                matriu[i, i - 8] = 1;

            // Abajo
            if (row < 7)
                matriu[i, i + 8] = 1;

            // Izquierda
            if (col > 0)
                matriu[i, i - 1] = 1;

            // Derecha
            if (col < 7)
                matriu[i, i + 1] = 1;
        }

        // 3️ Pasar matriz a listas de adyacencia
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }

    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {

        {
            // Lista de posibles casillas a las que puede moverse
            List<Tile> posibles = new List<Tile>();

            // Calculamos casillas alcanzables desde la posición del ladrón
            FindSelectableTiles(false);

            // Recorremos todas las casillas
            for (int i = 0; i < tiles.Length; i++)
            {
                // Si es alcanzable, la guardamos
                if (tiles[i].selectable)
                {
                    posibles.Add(tiles[i]);
                }
            }

            // Si hay casillas disponibles
            if (posibles.Count > 0)
            {
                // Elegimos una aleatoria
                int randomIndex = Random.Range(0, posibles.Count);
                Tile destino = posibles[randomIndex];

                // Movemos el ladrón a esa casilla
                robber.GetComponent<RobberMove>().MoveToTile(destino);
            }
        }
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
                 
        int indexcurrentTile;        

        if (cop==true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        // Inicializamos
        tiles[indexcurrentTile].visited = true;
        tiles[indexcurrentTile].distance = 0;

        nodes.Enqueue(tiles[indexcurrentTile]);

        while (nodes.Count > 0)
        {
            Tile current = nodes.Dequeue();

            foreach (int adjIndex in current.adjacency)
            {
                Tile neighbor = tiles[adjIndex];

                //  Evitar pasar por el otro policía (solo si es turno de policía)
                bool blocked = false;
                if (cop)
                {
                    for (int i = 0; i < cops.Length; i++)
                    {
                        int copTile = cops[i].GetComponent<CopMove>().currentTile;

                        if (copTile == adjIndex && adjIndex != indexcurrentTile)
                        {
                            blocked = true;
                        }
                    }
                }

                if (!neighbor.visited && !blocked)
                {
                    neighbor.visited = true;
                    neighbor.parent = current;
                    neighbor.distance = current.distance + 1;

                    //  Solo hasta distancia 2
                    if (neighbor.distance <= 2)
                    {
                        neighbor.selectable = true;
                    }

                    //  No seguimos más allá de 2
                    if (neighbor.distance < 2)
                    {
                        nodes.Enqueue(neighbor);
                    }
                }
            }
        }
    }


    }
    
   
    

    

   

       

