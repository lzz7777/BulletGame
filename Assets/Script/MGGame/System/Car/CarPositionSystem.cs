using System;
using UnityEngine;

namespace XN
{
    public static class CarPositionSystem
    {
        [UpdateSystem]
        public static void Update(this CarPositionComponent self, float deltaTime)
        {
            if (!GameStateCtrl.IsGaming)
            {
                return;
            }
            
            self.UpdateChangeGroup(deltaTime);
            self.UpdateChangeLine(deltaTime);

            if (GameConst.CarAniType == 1)
            {
                self.UpdatePos(deltaTime);   
            }
            else
            {
                self.UpdateX(deltaTime);
                self.UpdateY(deltaTime);
            }
        }

        public static void UpdatePos(this CarPositionComponent self, float deltaTime)
        {
            CarViewComponent carViewComp = self.Entity.GetComponent<CarViewComponent>();
            Transform tran = carViewComp.Car.transform;
            float speed = 3;
            float xPos = tran.position.x + (self.X - tran.position.x) * deltaTime * speed;
            float yPos = tran.position.y + (self.Y - tran.position.y) * deltaTime * speed;
            
            tran.position = new Vector3(xPos, yPos, tran.position.z);
            
        }

        public static void SetPosX(this CarPositionComponent self, float x, float moveTime = 0)
        {
            self.X = x;
            self.InitPos = true;

            if (moveTime != 0)
            {
                self.MoveXTime = moveTime;
            }
        }

        public static void SetPosY(this CarPositionComponent self, float y, float moveTime = 0)
        {
            self.Y = y;
            self.InitPos = true;
            
            if (moveTime != 0)
            {
                self.MoveYTime = moveTime;
            }
        }
        
        /// <summary>
        /// 更新组
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deltaTime"></param>
        private static void UpdateChangeGroup(this CarPositionComponent self, float deltaTime)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            bool canMove = carInfoComp.CanMoveY();
            if (!canMove)
            {
                return;
            }
            
            var rank = RoomHelper.GetCarRank(self.Entity.Id);
            if (carInfoComp.Group == rank)
            {
                return;
            }

            carInfoComp.Group = rank;
            
            var groupLinePos = RoomManager.Instance.GroupLinePos;
            var moveY = groupLinePos[rank][carInfoComp.Line].y;
            var carViewComp = self.Entity.GetComponent<CarViewComponent>();
            self.SetPosY(moveY, 0.5f);
            carViewComp.RefreshAllOrder();
            carViewComp.UpdateDeviceScale();
                
            EventsManager.BroadCast(GameEnum.CarChangeGroup, carInfoComp.Entity.Id);
        }

        /// <summary>
        /// 更新轨道
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deltaTime"></param>
        private static void UpdateChangeLine(this CarPositionComponent self, float deltaTime)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            var changeRoadCD = TotalConfigManager.ConfigManager.ConstConfigCategory.ChangeRoadCD;
            //changeRoadTime ms->s
            float changeRoadTime = TotalConfigManager.ConfigManager.ConstConfigCategory.ChangeRoadTime / 1000.0f;
            
            carInfoComp.ChangeLineTime += deltaTime;

            bool canMoveY = false;
            if (carInfoComp.ChangeLineDelay != 0 && carInfoComp.ChangeLineTime >= carInfoComp.ChangeLineDelay)
            {
                canMoveY = true;
            }
            else if (carInfoComp.ChangeLineTime >= changeRoadCD)
            {
                canMoveY = true;
            }

            if (carInfoComp.CanMoveY() && canMoveY)
            {
                if (carInfoComp.ChangeLineDelay != 0)
                {
                    carInfoComp.ChangeLineDelay = 0;
                }
                
                carInfoComp.ChangeLineTime = 0;
                carInfoComp.Line = carInfoComp.Line == 0 ? 1 : 0;
                float targetY = RoomManager.Instance.GroupLinePos[carInfoComp.Group][carInfoComp.Line].y;
                self.SetPosY(targetY, changeRoadTime);
            }
        }

        private static void UpdateX(this CarPositionComponent self, float deltaTime)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            if (!carInfoComp.CanMoveX() || !self.InitPos)
            {
                return;
            }

            var carViewComp = self.Entity.GetComponent<CarViewComponent>();
            float targetX = self.X;
            float dis = Math.Abs(targetX - carViewComp.Car.transform.position.x);

            if (dis < 0.001)
            {
                return;
            }

            if (self.MoveXTime == 0)
            {
                self.MoveXTime = 0.1f;
            }
            
            carViewComp.DoMoveX(targetX, self.MoveXTime);
            self.MoveXTime = 0;
        }

        private static void UpdateY(this CarPositionComponent self, float deltaTime)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            if (!carInfoComp.CanMoveY() || !self.InitPos)
            {
                return;
            }
            
            var carViewComp = self.Entity.GetComponent<CarViewComponent>();
            float targetY = self.Y;
            float dis = Math.Abs(targetY - carViewComp.Car.transform.position.y);

            if (dis < 0.001)
            {
                return;
            }

            if (self.MoveYTime == 0)
            {
                self.MoveYTime = 0.5f;
            }
            
            carViewComp.DoMoveY(targetY, self.MoveYTime);
            self.MoveYTime = 0;
        }
    }
}