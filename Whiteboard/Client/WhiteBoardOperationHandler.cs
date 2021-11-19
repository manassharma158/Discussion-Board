﻿/**
 * Owned By: Parul Sangwan
 * Created By: Parul Sangwan
 * Date Created: 10/13/2021
 * Date Modified: 11/12/2021
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Whiteboard
{
    /// <summary>
    /// Main Handler for Board Operations.
    /// </summary>
    public class WhiteBoardOperationHandler : IWhiteBoardOperationHandler
    {
        /// <summary>
        /// Size of the Canvas.
        /// </summary>
        private readonly Coordinate _canvasSize;

        /// <summary>
        /// State the board is in.
        /// </summary>
        private BoardOperationsState _boardState;

        /// <summary>
        /// Storing Handler for Active State.
        /// </summary>
        private readonly ActiveBoardOperationsHandler _activeBoardOperationsHandler;

        /// <summary>
        /// Storing Handler for inactive State.
        /// </summary>
        private readonly InactiveBoardOperationsHandler _inactiveBoardOperationsHandler;
        private BoardState _boardStateIdentifier;

        /// <summary>
        /// Construction for WhiteBoardOperationHandler.
        /// </summary>
        /// <param name="canvasSize"></param>
        public WhiteBoardOperationHandler(Coordinate canvasSize)
        {
            _canvasSize = canvasSize;
            _activeBoardOperationsHandler = new ActiveBoardOperationsHandler();
            _inactiveBoardOperationsHandler = new InactiveBoardOperationsHandler();
            _boardState = _activeBoardOperationsHandler;
            _boardStateIdentifier = BoardState.ACTIVE;
        }


        /// <summary>
        /// Changes the Fill Color of the shape.
        /// </summary>
        /// <param name="shapeFill"> Shape Fill Color. </param>
        /// <param name="shapeId">Id of the shape. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> ChangeShapeFill(BoardColor shapeFill, string shapeId)
        {
            return _boardState.ChangeShapeFill(shapeFill, shapeId);
        }

        /// <summary>
        /// Changes the Stroke Color of the shape outline.
        /// </summary>
        /// <param name="strokeColor"> Stroke Color. </param>
        /// <param name="shapeId">Id of the shape. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> ChangeStrokeColor(BoardColor strokeColor, string shapeId)
        {
            return _boardState.ChangeStrokeColor(strokeColor, shapeId);
        }

        /// <summary>
        /// Changes the thickness of the shape outline stroke.
        /// </summary>
        /// <param name="strokeWidth"> Stroke Thickness. </param>
        /// <param name="shapeId">Id of the shape. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> ChangeStrokeWidth(float strokeWidth, string shapeId)
        {
            return _boardState.ChangeStrokeWidth(strokeWidth, shapeId);
        }

        /// <summary>
        /// Creates Ellipse/Circle.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="strokeWidth"> Shape boundary stroke width. </param>
        /// <param name="strokeColor"> Color of shape boundary stroke. </param>
        /// <param name="shapeId"> Id of the shape. Null if shape creation just started. </param>
        /// <param name="shapeComp"> indicative of a mouse up event. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> CreateEllipse(Coordinate start, Coordinate end, float strokeWidth, BoardColor strokeColor, string shapeId = null, bool shapeComp = false)
        {
            return _boardState.CreateShape(ShapeType.ELLIPSE, start, end, strokeWidth, strokeColor, shapeId, shapeComp);
        }

        /// <summary>
        /// Creates straight line.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="strokeWidth"> Shape boundary stroke width. </param>
        /// <param name="strokeColor"> Color of shape boundary stroke. </param>
        /// <param name="shapeId"> Id of the shape. Null if shape creation just started. </param>
        /// <param name="shapeComp"> indicative of a mouse up event. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> CreateLine(Coordinate start, Coordinate end, float strokeWidth, BoardColor strokeColor, string shapeId = null, bool shapeComp = false)
        {
            return _boardState.CreateShape(ShapeType.LINE, start, end, strokeWidth, strokeColor, shapeId, shapeComp);
        }

        /// <summary>
        /// Creates Polyline.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="strokeWidth"> Shape boundary stroke width. </param>
        /// <param name="strokeColor"> Color of shape boundary stroke. </param>
        /// <param name="shapeId"> Id of the shape. Null if shape creation just started. </param>
        /// <param name="shapeComp"> indicative of a mouse up event. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> CreatePolyline(Coordinate start, Coordinate end, float strokeWidth, BoardColor strokeColor, string shapeId = null, bool shapeComp = false)
        {
            return _boardState.CreateShape(ShapeType.POLYLINE, start, end, strokeWidth, strokeColor, shapeId, shapeComp);
        }

        /// <summary>
        /// Creates Rectangle/Square.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="strokeWidth"> Shape boundary stroke width. </param>
        /// <param name="strokeColor"> Color of shape boundary stroke. </param>
        /// <param name="shapeId"> Id of the shape. Null if shape creation just started. </param>
        /// <param name="shapeComp"> indicative of a mouse up event. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> CreateRectangle(Coordinate start, Coordinate end, float strokeWidth, BoardColor strokeColor, string shapeId = null, bool shapeComp = false)
        {
            return _boardState.CreateShape(ShapeType.RECTANGLE, start, end, strokeWidth, strokeColor, shapeId, shapeComp);
        }

        /// <summary>
        /// Delete the shape with given shape ID.
        /// </summary>
        /// <param name="shapeId">Id of the shape.</param>
        /// <returns></returns>
        public List<UXShape> DeleteShape(string shapeId)
        {
            return _boardState.Delete(shapeId);
        }

        /// <summary>
        /// Gets owner of the shape with a shape Id.
        /// </summary>
        /// <param name="shapeId"> Id of the shape. </param>
        /// <returns> User Name. </returns>
        public string GetUserName(string shapeId)
        {
            return _boardState.GetUserName(shapeId);
        }

        /// <summary>
        /// Performs Redo.
        /// </summary>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> Redo()
        {
            return _boardState.Redo() ?? (new());
        }

        /// <summary>
        /// Resizes the shape with given shape ID.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="shapeId"> Id of the shape. </param>
        /// <param name="shapeComp"> Indicative of a mouse up event. </param>
        /// <param name="dragPos">The latch used for performing resizing.</param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> ResizeShape(Coordinate start, Coordinate end, string shapeId, DragPos dragPos, bool shapeComp = false)
        {
            Debug.Assert(shapeComp == true);
            return _boardState.Resize(start, end, shapeId, dragPos);
        }

        /// <summary>
        /// Rotates the shape with given shape ID.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="shapeId"> Id of the shape. </param>
        /// <param name="shapeComp"> Indicative of a mouse up event. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> RotateShape(Coordinate start, Coordinate end, string shapeId, bool shapeComp = false)
        {
            return _boardState.ModifyShapeRealTime(RealTimeOperation.ROTATE, start, end, shapeId, shapeComp);
        }

        /// <summary>
        /// Switches WhiteBoard state from active to inactive and vice-versa.
        /// </summary>
        /// <returns> Denotes succesfull state switch. </returns>
        public bool SwitchState()
        {
            _boardState = (_boardStateIdentifier == BoardState.ACTIVE) ? _inactiveBoardOperationsHandler : _activeBoardOperationsHandler;
            _boardStateIdentifier = (_boardStateIdentifier == BoardState.ACTIVE) ? BoardState.INACTIVE : BoardState.ACTIVE;
            return true;
        }

        /// <summary>
        /// Translates the shape with given shape ID.
        /// </summary>
        /// <param name="start"> Coordinate of mouse down event. </param>
        /// <param name="end"> Current cordinate to display real-time shape creation before/at mouse up event. </param>
        /// <param name="shapeId"> Id of the shape. </param>
        /// <param name="shapeComp"> Indicative of a mouse up event. </param>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> TranslateShape(Coordinate start, Coordinate end, string shapeId, bool shapeComp = false)
        {
            return _boardState.ModifyShapeRealTime(RealTimeOperation.TRANSLATE, start, end, shapeId, shapeComp);
        }

        /// <summary>
        /// Performs Undo.
        /// </summary>
        /// <returns> List of UXShapes for UX to render. </returns>
        public List<UXShape> Undo()
        {
            return _boardState.Undo() ?? (new());
        }

        public void SetUserLevel(int userlevel)
        {
            _boardState.UserLevel = userlevel;
        }
    }
}
