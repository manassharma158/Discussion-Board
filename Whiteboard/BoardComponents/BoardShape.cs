﻿/**
 * Owned By: Parul Sangwan
 * Created By: Parul Sangwan
 * Date Created: 10/11/2021
 * Date Modified: 11/01/2021
**/

using System;

namespace Whiteboard
{
    public class BoardShape
    {
        public BoardShape(MainShape mainShapeDefiner, int userLevel, DateTime creationTime, DateTime lastModifiedTime,
            string uid, string shapeOwnerId, Operation operation)
        {
            MainShapeDefiner = mainShapeDefiner;
            UserLevel = userLevel;
            CreationTime = creationTime;
            LastModifiedTime = lastModifiedTime;
            Uid = uid;
            ShapeOwnerId = shapeOwnerId;
            RecentOperation = operation;
        }

        public BoardShape()
        {
            MainShapeDefiner = null;
            UserLevel = 0;
            CreationTime = DateTime.Now;
            LastModifiedTime = DateTime.Now;
            Uid = null;
            ShapeOwnerId = null;
            RecentOperation = Operation.CREATE;
        }

        public MainShape MainShapeDefiner { get; set; }
        public int UserLevel { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public string Uid { get; set; }
        public string ShapeOwnerId { get; set; }
        public Operation RecentOperation { get; set; }

        public BoardShape Clone()
        {
            return new BoardShape(MainShapeDefiner.Clone(), UserLevel, CreationTime, LastModifiedTime,
                (string) Uid.Clone(), (string) ShapeOwnerId.Clone(), RecentOperation);
        }
    }
}