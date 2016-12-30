using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using StarfieldUtils.MathUtils;

namespace Tiling
{
    public partial class Form1 : Form
    {
        List<Vec2D> bounds = new List<Vec2D>();

        bool boundsSet = false;

        enum Tiling
        {
            Grid,
            Noise
        }

        private Tiling tiling;
        Random rand = new Random();

        public Form1()
        {
            InitializeComponent();

            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, panelDrawing, new object[] { true });

            enableTiling(Tiling.Grid);
        }

        private List<Point> GenerateGrid(PaintEventArgs e)
        {
            Brush brush = new SolidBrush(Color.Blue);
            int numX = (panelDrawing.Width / trackBar1.Value) + 1;
            int numY = (panelDrawing.Height / trackBar2.Value) + 1;

            List<Point> points = new List<Point>();

            for (int x = 0; x < numX; x++)
            {
                for(int y = 0; y < numY; y++)
                {
                    points.Add(new Point(x * trackBar1.Value, y * trackBar2.Value));
                }
            }

            if (checkBoxShowTiles.Checked)
            {
                foreach(Point point in points)
                {
                    e.Graphics.FillRectangle(brush, point.X, point.Y, 1, 1);
                }
            }

            return points;
        }

        private List<Point> GenerateNoise(PaintEventArgs e)
        {
            List<Point> points = new List<Point>();
            Bitmap bmp = new Bitmap(panelDrawing.Width, panelDrawing.Height);

            float persistance = 1.0f / (float)trackBar2.Value;
            int numOctaves = trackBar1.Value;
            float lacunarity = (float)trackBar3.Value / 10.0f;
            float tVal = (float)trackBar4.Value / 100.0f;
            float maxPct = (float)trackBar5.Value / 1000.0f; //.01f;// 
            float minPct = .0f;

            float minN = 1.0f;
            float maxN = 0.0f;

            for (int x = 0; x < panelDrawing.Width; x++)
            {
                for (int y = 0; y < panelDrawing.Height; y++)
                {
                    float xVal = (float)x / (float)panelDrawing.Width;
                    float yVal = (float)y / (float)panelDrawing.Height;

                    float n = .5f + SimplexNoise.fbm_noise4(xVal, yVal, 0, tVal, numOctaves, persistance, lacunarity);
                    
                    minN = Math.Min(n, minN);
                    maxN = Math.Max(n, maxN);

                    n = Math.Min(1.0f, Math.Max(0.0f, n));

                    if (checkBoxShowTiles.Checked)
                    {
                        int brightness = Math.Min(255, Math.Max(0, (int)(n * 255)));

                        Color toDraw = Color.FromArgb(brightness, brightness, brightness);

                        e.Graphics.FillRectangle(new SolidBrush(toDraw), x, y, 1, 1);
                    }

                    if ((n * (maxPct - minPct) + minPct) > rand.NextDouble())
                    {
                        points.Add(new Point(x, y));
                        if (checkBoxShowTiles.Checked)
                        {
                            e.Graphics.FillRectangle(new SolidBrush(Color.Blue), x, y, 1, 1);
                        }
                    }
                }
            }
            Console.WriteLine(minN);
            Console.WriteLine(maxN);

            return points;
        }

        private void enableTiling(Tiling tiling)
        {
            this.tiling = tiling;

            switch(tiling)
            {
                case Tiling.Grid:    
                    label1.Visible = true;
                    trackBar1.Visible = true;
                    label1.Text = "X Width";
                    trackBar1.Minimum = 5;
                    trackBar1.Maximum = 505;
                    trackBar1.SmallChange = 10;
                    trackBar1.Value = 40;

                    label2.Visible = true;
                    trackBar2.Visible = true;
                    label2.Text = "Y Width";
                    trackBar2.Minimum = 5;
                    trackBar2.Maximum = 505;
                    trackBar2.SmallChange = 10;
                    trackBar2.Value = 40;

                    label3.Visible = false;
                    trackBar3.Visible = false;
                    label4.Visible = false;
                    trackBar4.Visible = false;
                    label5.Visible = false;
                    trackBar5.Visible = false;

                    break;
                case Tiling.Noise:
                    panelDrawing.Hide();
                    label1.Visible = true;
                    trackBar1.Visible = true;
                    label1.Text = "Octaves";
                    trackBar1.Minimum = 1;
                    trackBar1.Maximum = 20;
                    trackBar1.SmallChange = 1;
                    trackBar1.Value = 4;

                    label2.Visible = true;
                    trackBar2.Visible = true;
                    label2.Text = "Persistence";
                    trackBar2.Minimum = 1;
                    trackBar2.Maximum = 10;
                    trackBar2.SmallChange = 1;
                    trackBar2.Value = 4;

                    label3.Visible = true;
                    trackBar3.Visible = true;
                    label3.Text = "Lacunarity";
                    trackBar3.Minimum = 1;
                    trackBar3.Maximum = 100;
                    trackBar3.SmallChange = 10;
                    trackBar3.Value = 20;

                    label4.Visible = true;
                    trackBar4.Visible = true;
                    label4.Text = "time";
                    trackBar4.Minimum = 0;
                    trackBar4.Maximum = 100;
                    trackBar4.SmallChange = 1;
                    trackBar4.Value = 0;

                    label5.Visible = true;
                    trackBar5.Visible = true;
                    label5.Text = "density";
                    trackBar5.Minimum = 1;
                    trackBar5.Maximum = 10;
                    trackBar5.SmallChange = 1;
                    trackBar5.Value = 3;

                    panelDrawing.Show();

                    break;
            }
        }

        private void panelDrawing_Paint(object sender, PaintEventArgs e)
        {
            if (boundsSet)
            {
                List<Point> points;
                List<Vec2D> pointVecs = new List<Vec2D>();
                Delaunay.Geo.Polygon polyBounds = new Delaunay.Geo.Polygon(bounds);
                Delaunay.Geo.Polygon smoothedBoundary = polyBounds.Scale(1.15f).Smooth(2);

                if (smoothedBoundary.Vertices.Count > 0)
                {
                    Point[] boundsVertices = new Point[smoothedBoundary.Vertices.Count];
                    for (int i = 0; i < smoothedBoundary.Vertices.Count; i++)
                    {
                        boundsVertices[i] = new Point((int)smoothedBoundary.Vertices[i].X, (int)smoothedBoundary.Vertices[i].Y);
                    }
                    e.Graphics.FillPolygon(new SolidBrush(Color.Black), boundsVertices);
                }

                //e.Graphics.FillRectangle(new SolidBrush(Color.Black), e.ClipRectangle);
                switch (this.tiling)
                {
                    case Tiling.Grid:
                        points = GenerateGrid(e);
                        break;
                    case Tiling.Noise:
                        points = GenerateNoise(e);
                        break;
                    default:
                        points = new List<Point>();
                        break;
                }

                List<uint> colors = new List<uint>();
                foreach (Point myPoint in points)
                {
                    pointVecs.Add(new Vec2D(myPoint.X, myPoint.Y));
                    colors.Add(0);
                }

                Voronoi v;

                for (int i = 0; i < numericUpDown1.Value; i++)
                {
                    List<Vec2D> newVecs = new List<Vec2D>();
                    v = new Voronoi(pointVecs, colors, new Delaunay.Geo.Rect(0, 0, panelDrawing.Width, panelDrawing.Height));
                    foreach (Vec2D vec in pointVecs)
                    {
                        List<Vec2D> region = v.Region(vec);
                        Vec2D centroid = (new Delaunay.Geo.Polygon(region)).Centroid();

                        newVecs.Add(centroid);
                    }

                    pointVecs = newVecs;
                }

                List<Vec2D> clipped = new List<Vec2D>();
                foreach(Vec2D site in pointVecs)
                {
                    if(polyBounds.ContainsPoint(site))
                    {
                        clipped.Add(site);
                    }
                }
                pointVecs = clipped;

                v = new Voronoi(pointVecs, colors, new Delaunay.Geo.Rect(0, 0, panelDrawing.Width, panelDrawing.Height));
                List<Delaunay.Geo.LineSegment> edges = v.VoronoiDiagram();

                foreach (Vec2D site in pointVecs)
                {
                    List<Vec2D> region = v.Region(site);
                    List<Vec2D> clippedRegion = new List<Vec2D>();
                    bool hasClipped = false;
                    Delaunay.Geo.LineSegment lastBoundIntersected = null;
                    Delaunay.Geo.Polygon polyRegion = new Delaunay.Geo.Polygon(region);
                    int indexOfFirstInsideBounds = -1;
                    int i = 0;
                    int index = 0;
                    bool secondPass = false;

                    while (i != ((indexOfFirstInsideBounds + 1) % region.Count) || !secondPass)
                    {
                        index = i;

                        Vec2D prev = index == 0 ? region[region.Count - 1] : region[index - 1];
                        Vec2D current = region[index];
                        Delaunay.Geo.LineSegment currentSegment = new Delaunay.Geo.LineSegment(prev, current);
                        if (!polyBounds.ContainsPoint(current))
                        {
                            if (!hasClipped && polyBounds.ContainsPoint(prev))
                            {
                                Delaunay.Geo.LineSegment intersectingSegment = polyBounds.Intersection(currentSegment);
                                lastBoundIntersected = intersectingSegment;
                                Vec2D intersection = Delaunay.Geo.LineSegment.Intersection(currentSegment, intersectingSegment);
                                clippedRegion.Add(intersection);
                            }
                            hasClipped = true;
                        }
                        else
                        {
                            if (indexOfFirstInsideBounds < 0)
                            {
                                indexOfFirstInsideBounds = i;
                            }
                            if (hasClipped)
                            {
                                Delaunay.Geo.LineSegment intersectingSegment = polyBounds.Intersection(new Delaunay.Geo.LineSegment(prev, current));
                                Vec2D intersection = Delaunay.Geo.LineSegment.Intersection(currentSegment, intersectingSegment);
                                if (lastBoundIntersected != null)
                                {
                                    foreach (Vec2D vertex in polyBounds.GetVerticesBetween(lastBoundIntersected.p0, intersectingSegment.p1, polyRegion.Winding()))
                                    {
                                        clippedRegion.Add(vertex);
                                    }
                                }

                                clippedRegion.Add(intersection);
                                hasClipped = false;
                            }
                            if (i < region.Count)
                            {
                                clippedRegion.Add(region[i]);
                            }
                        }
                        i = (i + 1) % region.Count;
                        if(i == 0)
                        {
                            secondPass = true;
                        }
                    }


                    Point[] clippedVertices = new Point[clippedRegion.Count];

                    for (i = 0; i < clippedRegion.Count; i++)
                    {
                        clippedVertices[i] = new Point((int)clippedRegion[i].X, (int)clippedRegion[i].Y);
                    }

                    if (checkBoxShowTiles.Checked)
                    {
                        e.Graphics.DrawPolygon(new Pen(new SolidBrush(Color.Green), 1.0f), clippedVertices);
                    }

                    region = clippedRegion;

                    Vec2D centroid = (new Delaunay.Geo.Polygon(region)).Centroid();
                    List<Vec2D> shrunk = (new Delaunay.Geo.Polygon(region)).Scale(.75f).Smooth(2).Vertices;

                    Point[] shrunkVertices = new Point[shrunk.Count];

                    for (i = 0; i < shrunk.Count; i++)
                    {
                        shrunkVertices[i] = new Point((int)shrunk[i].X, (int)shrunk[i].Y);
                    }

                    if (shrunkVertices.Length > 0 && !checkBoxShowTiles.Checked)
                    {
                        e.Graphics.FillPolygon(new SolidBrush(Color.White), shrunkVertices);
                    }

                    if (checkBoxShowTiles.Checked)
                    {
                        e.Graphics.DrawPolygon(new Pen(new SolidBrush(Color.Red), 1.0f), shrunkVertices);
                    }
                }

                if (checkBoxShowTiles.Checked)
                {
                    foreach (Delaunay.Geo.LineSegment segment in edges)
                    {
                        e.Graphics.DrawLine(new Pen(new SolidBrush(Color.FromArgb(0x33FFFFFF)), 1.0f), new Point((int)segment.p0.X, (int)segment.p0.Y), new Point((int)segment.p1.X, (int)segment.p1.Y));
                    }

                    List<Delaunay.Geo.Circle> circles = v.Circles();
                    foreach (Delaunay.Geo.Circle circle in circles)
                    {
                        //e.Graphics.DrawEllipse(new Pen(new SolidBrush(Color.FromArgb(0x33FFFFFF))), new Rectangle((int)(circle.center.X - circle.radius), (int)(circle.center.Y - circle.radius), (int)(circle.radius * 2), (int)(circle.radius * 2)));
                    }
                }
            }

            if (!boundsSet || checkBoxShowTiles.Checked)
            {
                //draw bounds
                if (bounds.Count > 2)
                {
                    Point[] boundsVertices = new Point[bounds.Count];
                    for (int i = 0; i < bounds.Count; i++)
                    {
                        boundsVertices[i] = new Point((int)bounds[i].X, (int)bounds[i].Y);
                    }
                    e.Graphics.DrawPolygon(new Pen(new SolidBrush(Color.Purple), 1.0f), boundsVertices);
                }
                else if (bounds.Count > 1)
                {
                    e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Purple), 1.0f), Vec2DToPoint(bounds[0]), Vec2DToPoint(bounds[1]));
                }
                else if (bounds.Count > 0)
                {
                    e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Purple)), (int)bounds[0].X - 2, (int)bounds[0].Y - 2, 4, 4);
                }
            }
        }

        private Point Vec2DToPoint(Vec2D pt)
        {
            return new Point((int)pt.X, (int)pt.Y);
        }

        private void radioButtonGrid_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButtonGrid.Checked)
            {
                enableTiling(Tiling.Grid);

                panelDrawing.Refresh();
            }
        }

        private void radioButtonNoise_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoise.Checked)
            {
                enableTiling(Tiling.Noise);

                panelDrawing.Refresh();
            }
        }

        private void checkBoxShowTiles_CheckedChanged(object sender, EventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void radioButtonNone_CheckedChanged(object sender, EventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void radioButtonVoronoi_CheckedChanged(object sender, EventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void trackBar3_MouseUp(object sender, MouseEventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void trackBar2_MouseUp(object sender, MouseEventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void trackBar4_MouseUp(object sender, MouseEventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void trackBar5_MouseUp(object sender, MouseEventArgs e)
        {
            panelDrawing.Refresh();
        }

        private void panelDrawing_MouseUp(object sender, MouseEventArgs e)
        {
            if(boundsSet == false)
            {
                bounds.Add(new Vec2D(e.X, e.Y));
                panelDrawing.Refresh();
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Enter)
            {
                boundsSet = true;
                panelDrawing.Refresh();
            }
        }
    }
}
