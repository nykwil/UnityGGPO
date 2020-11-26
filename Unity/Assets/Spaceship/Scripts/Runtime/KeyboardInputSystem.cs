using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Spaceship
{
    public class KeyboardInputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .ForEach((ref Player player, ref ActiveInput activeInput) =>
            {
                if (player.PlayerIndex == 0)
                {
                    activeInput.Reverse = Input.GetKey(KeyCode.S);
                    activeInput.Accelerate = Input.GetKey(KeyCode.W);
                    activeInput.Left = Input.GetKey(KeyCode.A);
                    activeInput.Right = Input.GetKey(KeyCode.D);
                    activeInput.Shoot = Input.GetKey(KeyCode.Z);
                }
                else if (player.PlayerIndex == 1)
                {
                    activeInput.Reverse = Input.GetKey(KeyCode.DownArrow);
                    activeInput.Accelerate = Input.GetKey(KeyCode.UpArrow);
                    activeInput.Left = Input.GetKey(KeyCode.LeftArrow);
                    activeInput.Right = Input.GetKey(KeyCode.RightArrow);
                    activeInput.Shoot = Input.GetKey(KeyCode.Space);
                }
            }).Run();
        }
    }
}