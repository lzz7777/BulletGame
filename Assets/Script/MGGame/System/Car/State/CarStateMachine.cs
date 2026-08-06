using System.Collections.Generic;
using cfg;

namespace XN
{
    public class CarStateMachine
    {
        private CarStateBase _currentState;
        private Dictionary<State, CarStateBase> _statesDic = new();

        public void ChangeState(State carState, long carId)
        {
            _currentState?.OnExit();

            if (!_statesDic.TryGetValue(carState, out var newState))
            {
                newState = CreateState(carState, carId);
                _statesDic[carState] = newState;
            }

            _currentState = newState;
            _currentState?.OnEnter();
        }

        private CarStateBase CreateState(State carState, long carId)
        {
            CarStateBase carStateBase = null;
            switch (carState)
            {
                case State.None:
                    break;
                case State.Start:
                    carStateBase = new CarStartState();
                    break;
                case State.Normal:
                    carStateBase = new CarNormalState();
                    break;
                case State.Fast:
                    carStateBase = new CarFastState();
                    break;
                case State.Hit:
                    carStateBase = new CarHitState();
                    break;
                case State.Invincible:
                    carStateBase = new CarInvincibleState();
                    break;
                case State.Damaged:
                    carStateBase = new CarDamagedState();
                    break;
            }

            if (carStateBase != null)
            {
                carStateBase.CarState = carState;
                carStateBase.CarId = carId;
            }
            
            return carStateBase;
        }

        public void Update() => _currentState?.OnUpdate();
    }
}