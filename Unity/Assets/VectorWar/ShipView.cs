using UnityEngine;

namespace VectorWar {

    public class ShipView : MonoBehaviour {
        public int progress;
        public string status;

        public void Populate(Ship shipGs) {
            transform.position = shipGs.position;
            transform.rotation = Quaternion.Euler(0, shipGs.heading, 0);
        }
    }
}
