using Unity.Entities;
using UnityEngine;

namespace Tests {
    public partial class TickCounterSystem : SystemBase {
        private int systemTick;

        protected override void OnUpdate() {
            systemTick += 1;
            string s = "";
            Entities
                .ForEach((Entity entity, ref TickCounterData tickData) => {
                    tickData.tickCount += 1;
                    s += $"{entity} = {tickData.tickCount}\n";
                }).WithoutBurst().Run();

            Debug.Log($"-- System Tick = {systemTick} --\n" + s);
        }
    }
}