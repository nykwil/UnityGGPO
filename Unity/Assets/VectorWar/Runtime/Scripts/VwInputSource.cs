using SharedGame;
using UnityEngine.InputSystem;
namespace VectorWar {
    public sealed class VwInputSource : ILocalInputSource {
        public long ReadInput(int controllerId, int frame) {
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0;
            long input = 0;
            if (controllerId == 0) {
                if (keyboard.upArrowKey.isPressed) input |= VWConstants.INPUT_THRUST;
                if (keyboard.downArrowKey.isPressed) input |= VWConstants.INPUT_BREAK;
                if (keyboard.leftArrowKey.isPressed) input |= VWConstants.INPUT_ROTATE_LEFT;
                if (keyboard.rightArrowKey.isPressed) input |= VWConstants.INPUT_ROTATE_RIGHT;
                if (keyboard.rightCtrlKey.isPressed) input |= VWConstants.INPUT_FIRE;
                if (keyboard.rightShiftKey.isPressed) input |= VWConstants.INPUT_BOMB;
            } else if (controllerId == 1) {
                if (keyboard.wKey.isPressed) input |= VWConstants.INPUT_THRUST;
                if (keyboard.sKey.isPressed) input |= VWConstants.INPUT_BREAK;
                if (keyboard.aKey.isPressed) input |= VWConstants.INPUT_ROTATE_LEFT;
                if (keyboard.dKey.isPressed) input |= VWConstants.INPUT_ROTATE_RIGHT;
                if (keyboard.fKey.isPressed) input |= VWConstants.INPUT_FIRE;
                if (keyboard.gKey.isPressed) input |= VWConstants.INPUT_BOMB;
            }
            return input;
        }
    }
}
