using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacious.GameModel
{
    public class CollisionManager
    {
        //public static List<BaseGameModel> gameObjects;
        
        //public CollisionManager()
        //{

        //}

        //public static CollisionResult HandleCollisions()
        //{
        //    foreach (var gameObject in gameObjects)
        //    {

        //    }            
        //}
    }

    public class CollisionResult
    {
        public CollisionType CollisionType { get; set; }

        public CollisionResult()
        {
            CollisionType = GameModel.CollisionType.None;
        }
    }

    public enum CollisionType
    {
        None,
        Blocked,
        OffRoad
    }
}
