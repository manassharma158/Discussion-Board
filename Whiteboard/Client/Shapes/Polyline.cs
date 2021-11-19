﻿/**
 * Owned By: Parul Sangwan
 * Created By: Parul Sangwan
 * Date Created: 11/01/2021
 * Date Modified: 11/12/2021
**/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whiteboard
{
    /// <summary>
    /// Polyline Class.
    /// </summary>
    public class Polyline : MainShape
    {
        /// <summary>
        /// Constructor setting just the basic attributes of Polyline.
        /// </summary>
        /// <param name="start">The Coordinate of start of mouse drag while creation.</param>
        public Polyline(Coordinate start) : base(ShapeType.POLYLINE)
        {
            this.Start = start;
            Points.Add(Start.Clone());
        }

        /// <summary>
        /// Constructor to create Polyline.
        /// </summary>
        /// <param name="height">Height of Polyline.</param>
        /// <param name="width">Width of Polyline.</param>
        /// <param name="strokeWidth">Stroke Width/</param>
        /// <param name="strokeColor">Stroke Color.</param>
        /// <param name="shapeFill">Fill color of the shape.</param>
        /// <param name="start">The left bottom coordinate of the smallest rectangle enclosing the shape.</param>
        /// <param name="points">List of points, if any.</param>
        /// <param name="angle">Angle of Rotation.</param>
        public Polyline(float height,
                         float width,
                         float strokeWidth,
                         BoardColor strokeColor,
                         BoardColor shapeFill,
                         Coordinate start,
                         Coordinate center,
                         List<Coordinate> points,
                         float angle) :
                         base(ShapeType.POLYLINE, height, width, strokeWidth, strokeColor, shapeFill, start, center, points, angle)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Polyline() : base(ShapeType.POLYLINE)
        {
            this.Points = new();
        }

        /// <summary>
        /// Creates/ modifies the previous shape.
        /// </summary>
        /// <param name="start">Start of mouse drag.</param>
        /// <param name="end">End of mouse drag.</param>
        /// <param name="prevPolyline">Previous Polyline object to modify.</param>
        /// <returns>Create/modified Polyline object.</returns>
        public override MainShape ShapeMaker([NotNull] Coordinate start, [NotNull] Coordinate end, MainShape prevPolyline = null)
        {
            // Create new shape if prevPolyLine is null.
            if (prevPolyline == null)
            {
                prevPolyline = new Polyline(start.Clone());
                AddToList(end.Clone());
            }
            AddToList(end.Clone());
            return prevPolyline;
        }

        /// <summary>
        /// Creating clone object of this class.
        /// </summary>
        /// <returns>Clone of shape.</returns>
        public override MainShape Clone()
        {
            List<Coordinate> pointClone = Points.Select(cord => new Coordinate(cord.R, cord.C)).ToList();
            return new Polyline(Height, Width, StrokeWidth, StrokeColor.Clone(), ShapeFill.Clone(), Start.Clone(), Center.Clone(), pointClone, AngleOfRotation);
        }

        /// <summary>
        /// Resize override for polyline.
        /// </summary>
        /// <param name="start">start of mouse drag for resize.</param>
        /// <param name="end">end of mousedrag for resize.</param>
        /// <param name="dragPos">The latch selected while resizing.</param>
        /// <returns></returns>
        public override bool Resize(Coordinate start, Coordinate end, DragPos dragPos)
        {
            return false;
        }

    }
}
