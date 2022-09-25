using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ClothSimulation : MonoBehaviour
{
    public GameObject pointPrefab;

    [System.Serializable]
    public class Force
    {
        [Range(-10f, 10f)]
        public float gravity = 9.81f;
        [Range(0f, 100f)]
        public float windMultiplier = 1.0f;
        public WindField windField;
        [Range(0.1f, 10f)]
        public float mass = 1.0f;
    }
    [System.Serializable]
    public class Cloth
    {
        public ClothCollider[] colliders;
        public bool lockCornerA = true;
        public bool lockCornerB = true;
        public bool lockCornerC = false;
        public bool lockCornerD = false;
        public bool canSelfRepair = false;
        [Range(1, 50)]
        public int rows = 10;
        [Range(1, 50)]
        public int columns = 10;
        [Range(2f, 25.0f)]
        public float spacing = 1.0f;
        [Range(2f, 100f)]
        public float extensibility = 5f;
        [Range(0f, 1f)]
        public float consistency = 0.5f;
    }

    public Force force;
    public Cloth cloth;
    [Range(1, 100)]
    public int solveIterations = 5;

    private float lastSpacing, lastExtensibility;
    private bool lastCanSelfRepair;
    List<List<Point>> points = new List<List<Point>>();


    private void Start()
    {
        Vector3 position = transform.position;
        GameObject shape = Instantiate(pointPrefab, position, Quaternion.identity);
        Point p = new Point(position, shape, cloth.lockCornerA, true);
        List<Point> initRow = new List<Point>();
        initRow.Add(p);
        points.Add(initRow);
    }
    void AddRow()
    {
        int existingRows = points.Count;
        int existingColumns = points[0].Count;
        points.Add(new List<Point>());

        float spacing = cloth.spacing;
        float extensibility = cloth.extensibility;
        for (int i = 0; i < existingColumns; i++)
        {
            Vector3 position = transform.position;
            if (existingRows == 1)
            {
                position = points[existingRows - 1][i].position + Vector3.forward * spacing;
            }
            if (existingRows > 1)
            {
                Vector3 closestPoint = points[existingRows - 1][i].position;
                Vector3 nextClosestPoint = points[existingRows - 2][i].position;
                Vector3 direction = closestPoint - nextClosestPoint;
                position = closestPoint + direction.normalized * spacing;
            }

            GameObject shape = Instantiate(pointPrefab, position, Quaternion.identity);
            Point p = new Point(position, shape, false, true);
            points[existingRows].Add(p);

            //Link to previous row
            if (existingRows > 0)
            {
                Point pointInLastRow = points[existingRows-1][i];
                p.LinkTo(pointInLastRow, spacing, extensibility, "row");

            }
            //Link to previous column
            if (i > 0)
            {
                Point pointInLastColumn = points[existingRows][i-1];
                p.LinkTo(pointInLastColumn, spacing, extensibility, "column");
            }
        }
        // Unlock previous last row
        points[existingRows - 1][0].locked = false;
        points[existingRows - 1][existingColumns - 1].locked = false;

    }
    void RemoveRow()
    {
        int existingRows = points.Count;
        int existingColumns = points[0].Count;

        List<Point> pointsInRowToRemove = points[existingRows - 1];
        points.RemoveAt(existingRows - 1);
        foreach (Point p in pointsInRowToRemove)
        {
            Destroy(p.GetShape());
        }
    }

    void AddColumn()
    {
        int existingRows = points.Count;
        int existingColumns = points[0].Count;
        float spacing = cloth.spacing;
        float extensibility = cloth.extensibility;
        for (int i = 0; i < existingRows; i++)
        {
            Vector3 position = transform.position;
            if (existingColumns == 1)
            {
                position = points[i][existingColumns - 1].position + Vector3.right * spacing;
            }
            if (existingColumns > 1)
            {
                Vector3 closestPoint = points[i][existingColumns - 1].position;
                Vector3 nextClosestPoint = points[i][existingColumns - 2].position;
                Vector3 direction = closestPoint - nextClosestPoint;
                position = closestPoint + direction.normalized * spacing;
            }

            GameObject shape = Instantiate(pointPrefab, position, Quaternion.identity);
            Point p = new Point(position, shape, false, true);
            points[i].Add(p);

            //Link to previous row
            if (i > 0)
            {
                Point pointInLastRow = points[i - 1][existingColumns];
                p.LinkTo(pointInLastRow, spacing, extensibility, "row");
            }
            // Link to previous column
            if (existingColumns > 0)
            {
                Point pointInLastColumn = points[i][existingColumns - 1];
                p.LinkTo(pointInLastColumn, spacing, extensibility, "column");
            }
        }
        // Unlock previous last column
        points[0][existingColumns - 1].locked = false;
        points[existingRows - 1][existingColumns - 1].locked = false;
    }
    void RemoveColumn()
    {
        int existingRows = points.Count;
        int existingColumns = points[0].Count;
        for (int i = 0; i < existingRows; i++)
        {
            Point p = points[i][existingColumns - 1];
            points[i].RemoveAt(existingColumns - 1);
            Destroy(p.GetShape());
        }
    }
    bool RefreshPointAmount(int rows, int columns)
    {
        int currentRows = points.Count;
        int currentColumns = points[0].Count;

        if (rows < currentRows)
        {
            for (int i = 0; i < currentRows - rows; i++)
            {
                RemoveRow();
            }
        }
        if (columns < currentColumns)
        {
            for (int i = 0; i < currentColumns - columns; i++)
            {
                RemoveColumn();
            }
        }
        if (rows > currentRows)
        {
            for (int i = 0; i < rows - currentRows; i++)
            {
                AddRow();
            }
        }

        if (columns > currentColumns)
        {
            for (int i = 0; i < columns - currentColumns; i++)
            {
                AddColumn();
            }
        }

        // Add diagonal links to new points
        for (int i = currentRows; i < rows; i++)
        {
            for (int j = currentColumns; j < columns; j++)
            {

                if (i > 0 && j > 0)
                {
                    Point p = points[i][j];
                    Point pointInLastRow = points[i - 1][j - 1];
                    p.LinkTo(pointInLastRow, cloth.spacing * 1.414213562373095f, cloth.extensibility, "diagonal");
                    points[i - 1][j].LinkTo(points[i][j - 1], cloth.spacing * 1.414213562373095f, cloth.extensibility, "diagonal");
                }
            }
        }


        // Returns true if amount of points changed
        return rows != currentRows || columns != currentColumns;
    }

    void RefreshExtensibilities(int rows, int columns, float extensibility)
    {
        float sturdyExtensibility = extensibility * 100;
        for (int i = 0; i < rows; i++)
        {
            int sturdyColumn = 0;
            if (i % 2 == 0)
            {
                sturdyColumn = columns - 1;
            }
            for (int j = 0; j < columns; j++)
            {
                Point p = points[i][j];
                if (i > 0)
                {
                    Link rowLink = p.GetLink(0);
                    if (j == sturdyColumn)
                    {
                        rowLink.SetExtensibility(sturdyExtensibility);
                    }
                    else
                    {
                        rowLink.SetExtensibility(extensibility);
                    }

                }

                if (j > 0)
                {
                    Link columnLink = p.GetLink(1);
                    if (i == 0)
                    {
                        columnLink = p.GetLink(0);
                    }
                    columnLink.SetExtensibility(sturdyExtensibility);
                }
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        int rows = cloth.rows;
        int columns = cloth.columns;
        bool amountChanged = RefreshPointAmount(rows, columns);

        // Link spacing
        float spacing = cloth.spacing;
        if (spacing != lastSpacing)
        {
            lastSpacing = spacing;
            float diagonalSpacing = spacing * 1.414213562373095f;
            foreach (List<Point> row in points)
            {
                foreach (Point p in row)
                {
                    p.UpdateSpacing(spacing, "diagonal", null);
                    p.UpdateSpacing(diagonalSpacing, null, "diagonal");
                }
            }
        }
        // update extensibility with a higher value added to links in a squirming corchet like order.
        // This will looks like threads are pulled appart from the cloth.
        float extensibility = cloth.extensibility;
        if (extensibility != lastExtensibility || amountChanged)
        {
            lastExtensibility = extensibility;
            RefreshExtensibilities(rows, columns, extensibility);
        }

        // Decides of the cloth can repair broken links so the cloth is pulled back into its original formation.
        // sort of looks like magnets.
        bool canSelfRepair = cloth.canSelfRepair;
        if (canSelfRepair != lastCanSelfRepair)
        {
            lastCanSelfRepair = canSelfRepair;
            foreach (List<Point> row in points)
            {
                foreach (Point p in row)
                {
                    p.SetSelfRepair(canSelfRepair);
                }
            }
        }
        // Make sure the desired points are locked/unlocked (static/dynamic).
        points[0][0].locked = cloth.lockCornerA;
        points[rows-1][0].locked = cloth.lockCornerB;
        points[0][columns-1].locked = cloth.lockCornerC;
        points[rows-1][columns-1].locked = cloth.lockCornerD;

        // Verlet simulation of the points, then bounce off collisions. 
        float dt = Time.fixedDeltaTime;
        float mass = force.mass;
        // Convert to meters/second
        float windMultiplier = force.windMultiplier * mass;
        WindField windField = force.windField;
        Vector3 gravityVector = Vector3.down * mass * force.gravity;
        foreach (List<Point> row in points)
        {
            foreach (Point p in row)
            {
                Vector3 force = gravityVector;
                if (windField != null)
                {
                    Vector3 windVector = windField.getWind(p.position) * windMultiplier;
                    force += windVector;
                }
                p.UpdateForces(force, dt);
            }
        }


        if (cloth.consistency != 0f)
        {
            foreach (List<Point> row in points)
            {
                foreach (Point p in row)
                {
                    p.SolveLinks(cloth.consistency, null, "diagonal");
                }
            }
        }
        // Link solving
        for (int i = 0; i < solveIterations; i++)
        {
            foreach (List<Point> row in points)
            {
                foreach (Point p in row)
                {
                    p.SolveLinks(1f, "diagonal", null);
                }
            }
        }


        ClothCollider[] colliders = cloth.colliders;
        foreach (List<Point> row in points)
        {
            foreach (Point p in row)
            {
                p.UpdateCollisions(colliders);
            }
        }

        // Set shape positions
        foreach (List<Point> row in points)
        {
            foreach (Point p in row)
            {
                p.UpdateShapePosition();
                p.UpdateShapeRotation();
            }
        }
    }
}
