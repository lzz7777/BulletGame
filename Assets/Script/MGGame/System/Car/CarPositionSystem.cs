using System;
using UnityEngine;

namespace XN
{
    public static class CarPositionSystem
    {
        [UpdateSystem]
        public static void Update(this CarPositionComponent self, float deltaTime)
        {
            self.UpdatePos(deltaTime);
            
            if (!GameStateCtrl.IsGaming)
            {
                return;
            }

            self.UpdateChangeGroup(deltaTime);
            self.UpdateChangeLine(deltaTime);
        }

        public static void UpdatePos(this CarPositionComponent self, float deltaTime)
        {
            var carViewComp = self.Entity.GetComponent<CarViewComponent>();
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            var targPos = new Vector3(self.X, self.Y, 0);

            if (carViewComp != null)
            {
                targPos = carViewComp.Car.transform.position;
            }
            
            float speed = 3;
            float xPos = targPos.x + (self.X - targPos.x) * deltaTime * speed;
            float yPos = targPos.y + (self.Y - targPos.y) * deltaTime * speed;
            var newPos = new Vector3(xPos, yPos, targPos.z);

            if (Math.Abs(targPos.x - newPos.x) < 0.1f)
            {
                if (self.MoveXEndCb != null)
                {
                    self.MoveXEndCb();
                    self.MoveXEndCb = null;
                }

                if (!carInfoComp.CanMoveX())
                    carInfoComp.RemoveMoveType(CarMoveType.MoveX);
            }

            if (Math.Abs(targPos.y - newPos.y) < 0.1f)
            {
                if (self.MoveYEndCb != null)
                {
                    self.MoveYEndCb();
                    self.MoveYEndCb = null;
                }

                if (!carInfoComp.CanMoveY())
                {
                    carInfoComp.RemoveMoveType(CarMoveType.MoveY);
                }
            }

            if (carViewComp?.Car != null)
                carViewComp.Car.transform.position = newPos;
        }

        public static void SetPosX(this CarPositionComponent self, float x, Action onFinish = null)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            if (!carInfoComp.CanMoveX())
                return;

            self.X = x;

            if (onFinish != null)
            {
                self.MoveXEndCb = onFinish;
            }

            carInfoComp.AddMoveType(CarMoveType.MoveX);
        }

        public static void SetPosY(this CarPositionComponent self, float y, Action onFinish = null)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            if (!carInfoComp.CanMoveY())
                return;

            self.Y = y;

            if (onFinish != null)
            {
                self.MoveYEndCb = onFinish;
            }

            carInfoComp.AddMoveType(CarMoveType.MoveY);
        }

        /// <summary>
        /// 更新组
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deltaTime"></param>
        private static void UpdateChangeGroup(this CarPositionComponent self, float deltaTime)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            bool canMove = carInfoComp?.CanMoveY() ?? false;
            if (!canMove || carInfoComp.IsDiscard)
                return;

            var rank = RoomHelper.GetCarRank(self.Entity.Id);
            if (carInfoComp.Group == rank)
            {
                return;
            }

            carInfoComp.Group = rank;

            var groupLinePos = RoomManager.Instance.GroupLinePos;
            var moveY = groupLinePos[rank][carInfoComp.Line].y;
            var carViewComp = self.Entity.GetComponent<CarViewComponent>();

            if (carInfoComp.Group <= 6 && carViewComp == null)
            {
                //从屏幕外面冲到屏幕里面
                carViewComp = self.Entity.AddComponent<CarViewComponent>();
                carViewComp.InitSystem();
                //刷新特效
                carViewComp.RefreshEffect();
            }

            self.SetPosY(moveY, self.UpdateChangeGroupFinish);

            carViewComp?.RefreshAllOrder();
            carViewComp?.UpdateDeviceScale();

            EventsManager.BroadCast(GameEnum.CarChangeGroup, carInfoComp.Entity.Id);
        }

        /// <summary>
        /// 更换组完成事件
        /// </summary>
        /// <param name="self"></param>
        private static void UpdateChangeGroupFinish(this CarPositionComponent self)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();

            if (carInfoComp.Group >= 7 && self.Entity.GetComponent(out CarViewComponent viewComp))
            {
                //从屏幕里面冲到外面
                //删除view组件
                self.Entity.RemoveComponent(viewComp);
            }
        }

        /// <summary>
        /// 更新轨道
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deltaTime"></param>
        private static void UpdateChangeLine(this CarPositionComponent self, float deltaTime)
        {
            var carInfoComp = self.Entity.GetComponent<CarInfoComponent>();
            bool canMove = carInfoComp?.CanMoveY() ?? false;
            if (!canMove || carInfoComp.IsDiscard)
                return;
            
            var changeRoadCD = TotalConfigManager.ConfigManager.ConstConfigCategory.ChangeRoadCD;
            //changeRoadTime ms->s
            //float changeRoadTime = TotalConfigManager.ConfigManager.ConstConfigCategory.ChangeRoadTime / 1000.0f;

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
                self.SetPosY(targetY);
            }
        }
    }
}