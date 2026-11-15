using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum CardinalDirection
{
    Unknown,
    North,
    NorthEast,
    SouthEast,
    South,
    SouthWest,
    NorthWest
}

public class HexGrid : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;

    public int rows = 7;
    public int columns = 11;

    public GameObject hexagonPrefab;

    public GameObject[,] hexagons;

    private readonly float hexOffsetX = 1.5f;
    private readonly float hexOffsetZ = -Mathf.Sqrt(3);

    private float gridOffsetX, gridOffsetZ;

    private void Start()
    {
        gridOffsetZ = 0.5f * (rows * hexOffsetZ);
        gridOffsetX = -0.5f * (columns * hexOffsetX);

        hexagons = new GameObject[rows, columns];
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                var hexagon = Instantiate(hexagonPrefab, transform);
                hexagon.name = $"Hexagon ({row},{column})";
                hexagon.layer = gameObject.layer;
                hexagon.transform.position = WorldPosition(row, column);

                hexagons[row, column] = hexagon;
            }
        }

        cinemachineCamera.Follow = hexagons[rows / 2, columns / 2].transform;
    }

    //public Vector3 WorldPosition(int row, int column) => new Vector3((row * verticalDistance) - ((row % 2 == 0) ? 0.5f * verticalDistance : 0f) - gridOffsetZ, 0f, (column * horizontalDistance) - gridOffsetX);
    public Vector3 WorldPosition(int row, int column) => new Vector3(gridOffsetX + (column * hexOffsetX), 0f, gridOffsetZ - (row * hexOffsetZ) - ((column % 2 == 0) ? 0.5f * hexOffsetZ : 0f));
    public Vector3 WorldPosition(Vector2Int cellCoordinates) => WorldPosition(cellCoordinates.x, cellCoordinates.y);

    public List<Vector2Int> NeighbourCoordinates(int row, int column)
    {
        var neighbourCoordinates = new List<Vector2Int>();

        if (IsCoordinateInbounds(row, column + 1)) neighbourCoordinates.Add(new Vector2Int(row, column + 1));
        if (IsCoordinateInbounds(row + 1, column)) neighbourCoordinates.Add(new Vector2Int(row + 1, column));
        if (IsCoordinateInbounds(row, column - 1)) neighbourCoordinates.Add(new Vector2Int(row, column - 1));
        if (IsCoordinateInbounds(row - 1, column - 1)) neighbourCoordinates.Add(new Vector2Int(row - 1, column - 1));
        if (IsCoordinateInbounds(row - 1, column)) neighbourCoordinates.Add(new Vector2Int(row - 1, column));
        if (IsCoordinateInbounds(row - 1, column + 1)) neighbourCoordinates.Add(new Vector2Int(row - 1, column + 1));

        return neighbourCoordinates;
    }

    public List<Vector2Int> NeighbourCoordinates(Vector2Int from) => NeighbourCoordinates(from.x, from.y);

    public List<Vector2Int> NeighbourRelativeCoordinates(int row, int column)
    {
        var neighbourCoordinates = new List<Vector2Int>();

        if (IsCoordinateInbounds(row, column + 1)) neighbourCoordinates.Add(new Vector2Int(0, 1));
        if (IsCoordinateInbounds(row + 1, column)) neighbourCoordinates.Add(new Vector2Int(1, 0));
        if (IsCoordinateInbounds(row, column - 1)) neighbourCoordinates.Add(new Vector2Int(0, -1));
        if (IsCoordinateInbounds(row - 1, column - 1)) neighbourCoordinates.Add(new Vector2Int(-1, -1));
        if (IsCoordinateInbounds(row - 1, column)) neighbourCoordinates.Add(new Vector2Int(-1, 0));
        if (IsCoordinateInbounds(row - 1, column + 1)) neighbourCoordinates.Add(new Vector2Int(-1, 1));

        return neighbourCoordinates;
    }

    public List<Vector2Int> NeighbourRelativeCoordinates(Vector2Int from) => NeighbourRelativeCoordinates(from.x, from.y);

    public List<CardinalDirection> NeighbourCardinalDirections(int row, int column)
    {
        var neighbourCardinalDirections = new List<CardinalDirection>();

        var neighbourCoordinates = NeighbourCoordinates(row, column);
        foreach (var coordinate in neighbourCoordinates)
        {
            neighbourCardinalDirections.Add(CardinalDirectionFromRelativeCoordinate(coordinate.x, coordinate.y));
        }

        return neighbourCardinalDirections;
    }

    public List<CardinalDirection> NeighbourCardinalDirections(Vector2Int from) => NeighbourCardinalDirections(from.x, from.y);

    public CardinalDirection CardinalDirectionFromRelativeCoordinate(int row, int column)
    {
        if (row == 0 && column == 1) return CardinalDirection.North;
        if (row == 1 && column == 0) return CardinalDirection.NorthEast;
        if (row == 0 && column == -1) return CardinalDirection.SouthEast;
        if (row == -1 && column == -1) return CardinalDirection.South;
        if (row == -1 && column == 0) return CardinalDirection.SouthWest;
        if (row == -1 && column == 1) return CardinalDirection.NorthWest;

        return CardinalDirection.Unknown;
    }

    public CardinalDirection CardinalDirectionFromRelativeCoordinate(Vector2Int from) => CardinalDirectionFromRelativeCoordinate(from.x, from.y);

    public Vector2Int RelativeCoordinatesFromCardinalDirection(CardinalDirection cardinalDirection)
    {
        switch (cardinalDirection)
        {
            case CardinalDirection.North: return new Vector2Int(0, 1);
            case CardinalDirection.NorthEast: return new Vector2Int(1, 0);
            case CardinalDirection.SouthEast: return new Vector2Int(0, -1);
            case CardinalDirection.South: return new Vector2Int(-1, -1);
            case CardinalDirection.SouthWest: return new Vector2Int(-1, -1);
            case CardinalDirection.NorthWest: return new Vector2Int(-1, -1);

            default: return Vector2Int.zero;
        }
    }

    public List<GameObject> Neighbours(int row, int column)
    {
        var neighbours = new List<GameObject>();

        var neighbourCoordinates = NeighbourCoordinates(row, column);
        foreach (var coordinate in neighbourCoordinates)
        {
            neighbours.Add(hexagons[row, column]);
        }

        return neighbours;
    }

    public List<GameObject> Neighbours(Vector2Int from) => Neighbours(from.x, from.y);
    public GameObject NeighbourAtRelativeCoordinate(int row, int column) => IsCoordinateInbounds(row, column) ? hexagons[row, column] : null;
    public GameObject NeighbourAtRelativeCoordinate(Vector2Int relativeCoordinate) => NeighbourAtRelativeCoordinate(relativeCoordinate.x, relativeCoordinate.y);
    public GameObject NeighbourAtCardinalDirection(CardinalDirection cardinalDirection) => NeighbourAtRelativeCoordinate(RelativeCoordinatesFromCardinalDirection(cardinalDirection));

    public bool IsCoordinateInbounds(int row, int column)
    {
        if (row < 0 || column < 0) return false;
        if (row > columns - 1 || column > rows - 1) return false;

        return true;
    }

    public bool IsCoordinateInbounds(Vector2Int coordinate) => IsCoordinateInbounds(coordinate.x, coordinate.y);
}
