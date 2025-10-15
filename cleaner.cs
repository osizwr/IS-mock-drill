using System;
namespace RobotCleaner
{
  public class Map
  {
    private enum CellType { Empty, Dirt, Obstacle, Cleaned };
    private CellType[,] _grid;
    public int Width {get; private set;}
    public int Height {get; private set;}

    public Map(int width, int height)
    {
      this.Width = width;
      this.Height = height;
      _grid = new CellType[width, height];
      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++ )
        {
          _grid[x,y] = CellType.Empty;
        }
      }
    }

    public bool IsInBounds(int x, int y)
    {
      return x >= 0 && x < this.Width && y >= 0 && y < this.Height;
    }

    public bool IsDirt(int x, int y){
      return IsInBounds(x,y) && _grid[x,y] == CellType.Dirt;
    }

    public bool IsObstacle(int x, int y){
      return IsInBounds(x,y) && _grid[x,y] == CellType.Obstacle;
    }
    public bool AllCleaned()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                // if cell is not obstacle and not cleaned or dirt, cleaning is not done yet
                if (!IsObstacle(x, y) && _grid[x, y] != CellType.Cleaned)
                    return false;
            }
        }
        return true;
    }
    public bool IsCleaned(int x, int y)
    {
        return IsInBounds(x, y) && _grid[x, y] == CellType.Cleaned;
    }

    public void AddObstacle(int x, int y)
    {
      _grid[x, y] = CellType.Obstacle;
    }
    public void AddDirt(int x, int y)
    {
      _grid[x, y] = CellType.Dirt;
    }

    public void Clean(int x, int y)
    {
      if( IsInBounds(x,y))
      {
        _grid[x, y] = CellType.Cleaned;
      }
    }
    public void Display(int robotX, int robotY)
    {
      // display the 2d grid, it accepts the location of the robot in x and y
      Console.Clear();
      Console.WriteLine("Vacuum cleaner robot simulation");
      Console.WriteLine("--------------------------------");
      Console.WriteLine("Legends: #=Obstacles, D=Dirt, .=Empty, R=Robot, C=Cleaned");

      //display the grid using loop
      for (int y = 0; y < this.Height; y++)
      {
        for (int x = 0; x < this.Width; x++)
        {
          if( x==robotX && y == robotY)
          {
            Console.Write("R ");
          }
          else
          {
            switch(_grid[x,y])
            {
              case CellType.Empty: Console.Write(". "); break;
              case CellType.Dirt: Console.Write("D "); break;
              case CellType.Obstacle: Console.Write("# "); break;
              case CellType.Cleaned: Console.Write("C "); break;
            }
          }
        }
        Console.WriteLine();
      } //outer for loop
      // add delay
      Thread.Sleep(200);
    } // display method
  }//class map
  public interface IStrategy
  {
    void Clean(Robot robot);
  }

  public class PerimeterHuggerStrategy : IStrategy
  {
    public void Clean(Robot robot)
    {
      Console.WriteLine("Perimeter Hugger Strategy start cleaning...");

      robot.CleanCurrentSpot();

      while (robot.Move(robot.X + 1, robot.Y))
      {
        robot.CleanCurrentSpot();
      }

      while (robot.Move(robot.X, robot.Y + 1))
      {
        robot.CleanCurrentSpot();
      }

      while (robot.Move(robot.X - 1, robot.Y))
      {
        robot.CleanCurrentSpot();
      }

      while (robot.Move(robot.X, robot.Y - 1))
      {
        robot.CleanCurrentSpot();
      }

      Console.WriteLine("Perimeter Hugger Strategy finished cleaning.");
    }
  }

public class SpiralStrategy : IStrategy
{
    public void Clean(Robot robot)
    {
        Console.WriteLine("Spiral strategy start cleaning...");

        int centerX = robot.Map.Width / 2;
        int centerY = robot.Map.Height / 2;

        // Start from center
        robot.Move(centerX, centerY);
        robot.CleanCurrentSpot();

        int[,] directions = new int[,]
        {
            { 1, 0 },   // right
            { 0, 1 },   // down
            { -1, 0 },  // left
            { 0, -1 }   // up
        };

        int directionIndex = 0;
        int segmentLength = 1;
        int stepsTaken = 0;
        int turnsMade = 0;
        int stuckCounter = 0;

        while (true)
        {
            // Check if all tiles cleaned
            if (robot.Map.AllCleaned())
            {
                Console.WriteLine("All spots cleaned.");
                break;
            }

            int dx = directions[directionIndex, 0];
            int dy = directions[directionIndex, 1];

            int newX = robot.X + dx;
            int newY = robot.Y + dy;

            // Check if new position is valid
            if (robot.Map.IsInBounds(newX, newY) && !robot.Map.IsObstacle(newX, newY))
            {
                robot.Move(newX, newY);
                if (!robot.Map.IsCleaned(newX, newY))
                    robot.CleanCurrentSpot();

                stepsTaken++;
                stuckCounter = 0; // reset
            }
            else
            {
                // rotate to next direction
                directionIndex = (directionIndex + 1) % 4;
                stuckCounter++;

                // If all directions failed (4 turns), robot is fully blocked
                if (stuckCounter >= 4)
                {
                    Console.WriteLine("Robot fully blocked. Cannot move further.");
                    break;
                }
                continue;
            }

            // Once segment completed, turn
            if (stepsTaken >= segmentLength)
            {
                directionIndex = (directionIndex + 1) % 4;
                stepsTaken = 0;
                turnsMade++;
                stuckCounter = 0;

                // After every two turns, spiral expands
                if (turnsMade % 2 == 0)
                    segmentLength++;
            }

            // Safety stop: if the spiral exceeds map bounds
            if (segmentLength > robot.Map.Width && segmentLength > robot.Map.Height)
            {
                Console.WriteLine("Reached spiral boundary limit.");
                break;
            }
        }

        Console.WriteLine("Spiral Strategy finished cleaning.");
    }
}

  public class Robot
  {
    private readonly Map _map;
    private readonly IStrategy _strategy;

    public int X { get; set; }
    public int Y { get; set; }

    public Map Map { get { return _map; } }

    public Robot(Map map, IStrategy strategy)
    {
      _map = map;
      _strategy = strategy;
      X = 0;
      Y = 0;
    }

    public bool Move(int newX, int newY)
    {
      if (_map.IsInBounds(newX, newY) && !_map.IsObstacle(newX, newY))
      {
        // set the new location
        X = newX;
        Y = newY;
        // display the map with the robot in its location in the grid
        _map.Display(X, Y);
        return true;
      }
      // it cannot move
      return false;
    }// Move method

    public void CleanCurrentSpot()
    {
      if (_map.IsDirt(X, Y))
      {
        _map.Clean(X, Y);
        _map.Display(X, Y);
      }
    }

    public void StartCleaning()
    {
      _strategy.Clean(this);
    }
  }

 public class SomeStrategy : IStrategy
  {
    public void Clean(Robot robot)
    {
        int direction = 1; // 1 = right, -1 = left
        for (int y = 0; y < robot.Map.Height; y++)
        {
            int startX = (direction == 1) ? 0 : robot.Map.Width - 1;
            int endX = (direction == 1) ? robot.Map.Width : -1;
            
            for (int x = startX; x != endX; x += direction)
            {
                robot.Move(x, y);
                robot.CleanCurrentSpot();
            }
            direction *= -1; // Reverse direction for the next row
        }
    }
  }

  public class Program
  {

    public static void Main(string[] args){
      Console.WriteLine("Initialize robot");


      IStrategy some_strategy = new SomeStrategy();
      IStrategy perimeterstrategy = new PerimeterHuggerStrategy();
      IStrategy spiralstrategy = new SpiralStrategy();

      Map map = new Map(20, 10);
      // map.Display( 10,10);

      map.AddDirt(5,3);
      map.AddDirt(10, 8);
      map.AddObstacle(2,5);
      map.AddObstacle(12,1);
      map.Display(11,8);

      Robot robot = new Robot(map,spiralstrategy);

      robot.StartCleaning();

      Console.WriteLine("Done.");
    }
  }
}

