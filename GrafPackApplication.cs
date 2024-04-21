﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace GrafPack
{
    public partial class GrafPackApplication : Form
    {
        private List<Shape> shapes = new List<Shape>();
        private Shape selectedShape;
        private Point lastMousePosition; // Store the last mouse position for dragging
        private bool isDragging;
        private bool isRotating;
        private Shape tempShape; // Temporary shape for dynamic creation
        private MainMenu mainMenu;
        private bool isCreateMode = false;
        private Type shapeToCreate;
        private Point startPoint;

        public GrafPackApplication()
        {
            InitializeComponent();
            SetupMainMenu();
            this.DoubleBuffered = true;
            isDragging = false;
            lastMousePosition = new Point();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "GrafPack App";
        }

        private void SetupMainMenu()
        {
            mainMenu = new MainMenu();
            var createItem = new MenuItem("Create");
            createItem.MenuItems.Add("Square", (s, e) => { isCreateMode = true; shapeToCreate = typeof(Square); });
            createItem.MenuItems.Add("Circle", (s, e) => { isCreateMode = true; shapeToCreate = typeof(Circle); });
            createItem.MenuItems.Add("Triangle", (s, e) => { isCreateMode = true; shapeToCreate = typeof(Triangle); });
            createItem.MenuItems.Add("Hexagon", (s, e) => { isCreateMode = true; shapeToCreate = typeof(Hexagon); });
            mainMenu.MenuItems.Add(createItem);

            mainMenu.MenuItems.Add("Select", (s, e) => isCreateMode = false);
            mainMenu.MenuItems.Add("Move", (s, e) => StartMove());

            var rotateItem = new MenuItem("Rotate");
            rotateItem.MenuItems.Add("45 Degrees", (s, e) => RotateSelectedShape(45));
            rotateItem.MenuItems.Add("90 Degrees", (s, e) => RotateSelectedShape(90));
            rotateItem.MenuItems.Add("135 Degrees", (s, e) => RotateSelectedShape(135));
            mainMenu.MenuItems.Add(rotateItem);

            MenuItem deleteItem = new MenuItem("Delete", (s, e) => DeleteSelectedShape());
            mainMenu.MenuItems.Add(deleteItem);
            mainMenu.MenuItems.Add("Exit", (s, e) => Close());
            this.Menu = mainMenu;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (isCreateMode)
            {
                startPoint = e.Location;
                tempShape = ShapeFactory.CreateShape(shapeToCreate, startPoint);
                this.MouseMove += OnMouseMoveCreateShape;
                this.MouseUp += OnMouseUpFinalizeShape;
            }
            else
            {
                bool shapeFound = false;
                foreach (var shape in shapes)
                {
                    if (shape.ContainsPoint(e.Location))
                    {
                        if (selectedShape != null)
                        {
                            selectedShape.IsSelected = false; // Deselect the previously selected shape
                        }
                        selectedShape = shape;
                        selectedShape.IsSelected = true; // Highlight the newly selected shape
                        shapeFound = true;

                        // Prepare to move immediately without needing to select "Move" from the menu
                        lastMousePosition = e.Location;
                        isDragging = true;
                        this.MouseDown += OnMouseDownStartDrag;
                        this.MouseMove += OnMouseMoveDrag;
                        this.MouseUp += OnMouseUpEndDrag;
                        break;
                    }
                }

                if (!shapeFound && selectedShape != null)
                {
                    selectedShape.IsSelected = false;
                    selectedShape = null;
                }
                Invalidate();
            }
        }


        private void OnMouseMoveCreateShape(object sender, MouseEventArgs e)
        {
            if (tempShape != null)
            {
                if (tempShape is Square || tempShape is Circle)
                {
                    ((dynamic)tempShape).UpdateEndPoint(e.Location);
                }
                // Other shapes logic...
                this.Invalidate();
            }
        }

        private void OnMouseUpFinalizeShape(object sender, MouseEventArgs e)
        {
            if (tempShape != null)
            {
                shapes.Add(tempShape);
                tempShape = null;
                isCreateMode = false;
                this.MouseMove -= OnMouseMoveCreateShape;
                this.MouseUp -= OnMouseUpFinalizeShape;
                this.Invalidate();
            }
        }

        /////////////////////////////////////////////////////////////////////

        private void StartMove()
        {
            if (selectedShape != null)
            {
                MessageBox.Show("You can now drag the selected shape to move it.", "Move", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No shape selected to move. Please select a shape first.", "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnMouseDownStartDrag(object sender, MouseEventArgs e)
        {
            // Already set up in the OnMouseDown, ensure we're over the selected shape
            if (selectedShape != null && selectedShape.ContainsPoint(e.Location) && !isDragging)
            {
                lastMousePosition = e.Location;
                isDragging = true;
            }
        }

        private void OnMouseMoveDrag(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedShape != null)
            {
                int dx = e.X - lastMousePosition.X;
                int dy = e.Y - lastMousePosition.Y;
                selectedShape.Move(dx, dy);
                lastMousePosition = e.Location;
                Invalidate();
            }
        }

        private void OnMouseUpEndDrag(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                this.MouseDown -= OnMouseDownStartDrag;
                this.MouseMove -= OnMouseMoveDrag;
                this.MouseUp -= OnMouseUpEndDrag;
                Invalidate();
            }
        }


        /////////////////////////////////////////////////////////////////////



        private void RotateSelectedShape(float degrees)
        {
            if (selectedShape != null)
            {
                selectedShape.Rotate(degrees);
                this.Invalidate(); // Redraw the form to update the display
            }
            else
            {
                MessageBox.Show("No shape selected to rotate.", "Rotation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void DeleteSelectedShape()
        {
            if (selectedShape != null)
            {
                // Remove the selected shape from the list of shapes
                shapes.Remove(selectedShape);
                // Clear the selectedShape as it no longer exists
                selectedShape = null;
                // Force the form to redraw to reflect the removal of the shape
                this.Invalidate();
            }
            else
            {
                // Optionally handle the case where no shape was selected but delete was attempted
                MessageBox.Show("No shape selected to delete.", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            foreach (var shape in shapes)
            {
                shape.Draw(g);
            }
            tempShape?.Draw(g); // Draw the temporary shape if it's not null
        }

        // Other event handlers and methods...
    }

    public abstract class Shape
    {
        public abstract void Draw(Graphics g);
        public abstract bool ContainsPoint(Point p);
        public abstract void Move(int dx, int dy);
        public abstract void UpdateEndPoint(Point newEndPoint);
        public abstract void Rotate(float angle);
        public abstract Point GetCenter();
        public bool IsSelected { get; set; }
        public Point StartPoint { get; protected set; }
        public Point EndPoint { get; protected set; }
    }

    public static class ShapeFactory
    {
        public static Shape CreateShape(Type shapeType, Point start, int size = 200) // Default size parameter
        {
            if (shapeType == typeof(Square))
            {
                return new Square(start);
            }
            else if (shapeType == typeof(Circle))
            {
                return new Circle(start);
            }
            else if (shapeType == typeof(Triangle))
            {
                Point p1 = start;
                Point p2 = new Point(start.X + 100, start.Y);
                Point p3 = new Point(start.X + 50, start.Y - 86);
                return new Triangle(p1, p2, p3);
            }
            else if (shapeType == typeof(Hexagon))
            {
                return new Hexagon(start, size); // Size here is the radius of the hexagon
            }
            throw new ArgumentException("Invalid shape type");
        }


    }

    // Implementation for Square, Circle, and other shapes...
    public class Square : Shape
    {
        private PointF center;
        private int size;
        public Square(Point start) : base()
        {
            this.StartPoint = start;
            this.EndPoint = start; // Initially, the end point is the same as the start point.
            this.center = new PointF(start.X + size / 2f, start.Y + size / 2f);
            this.size = size;
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                // Calculate the size based on start and end points.
                int size = Math.Max(Math.Abs(EndPoint.X - StartPoint.X), Math.Abs(EndPoint.Y - StartPoint.Y));
                Rectangle rect = new Rectangle(StartPoint.X, StartPoint.Y, size, size);
                g.DrawRectangle(pen, rect);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            int size = Math.Max(Math.Abs(EndPoint.X - StartPoint.X), Math.Abs(EndPoint.Y - StartPoint.Y));
            return new Rectangle(StartPoint.X, StartPoint.Y, size, size).Contains(p);
        }

        public override void Move(int dx, int dy)
        {
            StartPoint = new Point(StartPoint.X + dx, StartPoint.Y + dy);
            EndPoint = new Point(EndPoint.X + dx, EndPoint.Y + dy);
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            EndPoint = newEndPoint;
        }

        public override void Rotate(float angle)
        {
            // Convert degrees to radians
            double radians = angle * Math.PI / 180.0;

            // Calculate the new start and end points after rotation
            float cosTheta = (float)Math.Cos(radians);
            float sinTheta = (float)Math.Sin(radians);

            float newStartX = (float)(center.X + ((StartPoint.X - center.X) * cosTheta - (StartPoint.Y - center.Y) * sinTheta));
            float newStartY = (float)(center.Y + ((StartPoint.X - center.X) * sinTheta + (StartPoint.Y - center.Y) * cosTheta));
            StartPoint = new Point((int)newStartX, (int)newStartY);

            float newEndX = (float)(center.X + ((EndPoint.X - center.X) * cosTheta - (EndPoint.Y - center.Y) * sinTheta));
            float newEndY = (float)(center.Y + ((EndPoint.X - center.X) * sinTheta + (EndPoint.Y - center.Y) * cosTheta));
            EndPoint = new Point((int)newEndX, (int)newEndY);

            // Recalculate the center point
            center = new PointF((StartPoint.X + EndPoint.X) / 2f, (StartPoint.Y + EndPoint.Y) / 2f);
        }

        public override Point GetCenter()
        {
            return Point.Round(center);
        }

    }

    public class Circle : Shape
    {
        public Circle(Point center) : base()
        {
            this.StartPoint = center;
            this.EndPoint = center; // Initially, the end point is the same as the center for the radius.
        }

        public override void Draw(Graphics g)
        {
            int radius = (int)Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
            Point topLeft = new Point(StartPoint.X - radius, StartPoint.Y - radius);
            Size size = new Size(radius * 2, radius * 2);
            Rectangle rect = new Rectangle(topLeft, size);
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawEllipse(pen, rect);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            int radius = (int)Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
            return (Math.Pow(p.X - StartPoint.X, 2) + Math.Pow(p.Y - StartPoint.Y, 2)) <= (radius * radius);
        }

        public override void Move(int dx, int dy)
        {
            StartPoint = new Point(StartPoint.X + dx, StartPoint.Y + dy);
            EndPoint = new Point(EndPoint.X + dx, EndPoint.Y + dy);
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            EndPoint = newEndPoint;
        }

        public override Point GetCenter()
        {
            // Assuming StartPoint is the center for the circle
            return StartPoint;
        }

        public override void Rotate(float angle)
        {
            throw new NotImplementedException();
        }
    }

    public class Triangle : Shape
    {
        private Point point1, point2, point3;

        public Triangle(Point p1, Point p2, Point p3)
        {
            point1 = p1;
            point2 = p2;
            point3 = p3;
            this.StartPoint = p1;  // StartPoint can be any of the triangle's points
            this.EndPoint = p3;    // EndPoint can be any of the triangle's points different from StartPoint
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawLine(pen, point1, point2);
                g.DrawLine(pen, point2, point3);
                g.DrawLine(pen, point3, point1);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            // Using the ray-casting algorithm to determine if the point is inside the triangle
            int[] x = { point1.X, point2.X, point3.X };
            int[] y = { point1.Y, point2.Y, point3.Y };
            int i, j = 2;
            bool oddNodes = false;

            for (i = 0; i < 3; i++)
            {
                if ((y[i] < p.Y && y[j] >= p.Y || y[j] < p.Y && y[i] >= p.Y) &&
                    (x[i] <= p.X || x[j] <= p.X))
                {
                    oddNodes ^= (x[i] + (p.Y - y[i]) / (y[j] - y[i]) * (x[j] - x[i]) < p.X);
                }
                j = i;
            }

            return oddNodes;
        }

        public override void Move(int dx, int dy)
        {
            point1.Offset(dx, dy);
            point2.Offset(dx, dy);
            point3.Offset(dx, dy);
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            // This can be used to resize the triangle if necessary
            EndPoint = newEndPoint;
        }

        public override void Rotate(float angle)
        {
            // Center of the triangle for rotation
            Point center = GetCenter();

            // Convert degrees to radians
            double radians = angle * Math.PI / 180.0;
            RotatePoint(ref point1, center, radians);
            RotatePoint(ref point2, center, radians);
            RotatePoint(ref point3, center, radians);
        }

        public override Point GetCenter()
        {
            // The centroid of the triangle
            int centerX = (point1.X + point2.X + point3.X) / 3;
            int centerY = (point1.Y + point2.Y + point3.Y) / 3;
            return new Point(centerX, centerY);
        }

        private void RotatePoint(ref Point point, Point center, double radians)
        {
            int x = point.X - center.X;
            int y = point.Y - center.Y;
            point.X = center.X + (int)(x * Math.Cos(radians) - y * Math.Sin(radians));
            point.Y = center.Y + (int)(x * Math.Sin(radians) + y * Math.Cos(radians));
        }
    }

    public class Hexagon : Shape
    {
        private Point[] vertices = new Point[6];

        public Hexagon(Point center, int radius)
        {
            CalculateVertices(center, radius);
        }

        private void CalculateVertices(Point center, int radius)
        {
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 3 * i;
                vertices[i] = new Point(
                    center.X + (int)(radius * Math.Cos(angle)),
                    center.Y + (int)(radius * Math.Sin(angle))
                );
            }
            StartPoint = vertices[0];
            EndPoint = vertices[3]; // Points across the hexagon's diameter
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawPolygon(pen, vertices);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            int crossingNumber = 0;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                if (((vertices[i].Y > p.Y) != (vertices[j].Y > p.Y)) &&
                    (p.X < (vertices[j].X - vertices[i].X) * (p.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                    crossingNumber++;
            }
            return (crossingNumber % 2 == 1);
        }

        public override void Move(int dx, int dy)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].X += dx;
                vertices[i].Y += dy;
            }
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            // Could potentially re-calculate size based on new end point
        }

        public override void Rotate(float angle)
        {
            Point center = GetCenter();
            double radians = angle * Math.PI / 180.0;

            for (int i = 0; i < vertices.Length; i++)
            {
                int x = vertices[i].X - center.X;
                int y = vertices[i].Y - center.Y;
                vertices[i].X = center.X + (int)(x * Math.Cos(radians) - y * Math.Sin(radians));
                vertices[i].Y = center.Y + (int)(x * Math.Sin(radians) + y * Math.Cos(radians));
            }
        }

        public override Point GetCenter()
        {
            int sumX = 0, sumY = 0;
            foreach (Point vertex in vertices)
            {
                sumX += vertex.X;
                sumY += vertex.Y;
            }
            return new Point(sumX / vertices.Length, sumY / vertices.Length);
        }
    }



}