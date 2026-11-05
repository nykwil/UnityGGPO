using UnityEngine;
namespace VectorWar {
    public sealed class VwShipView : MonoBehaviour {
        public Transform model;
        public void Populate(Ship ship) {
            transform.position = new Vector3(FixedMath.ToFloat(ship.position.x), FixedMath.ToFloat(ship.position.y));
            model.rotation = Quaternion.Euler(0, 0, ship.heading);
        }
    }
}
