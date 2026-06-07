using UnityEngine;

namespace ArenaCraft
{
    public class ShopZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.CurrentPhase != GamePhase.Shopping) return;

            PlayerInventory inv = other.GetComponentInParent<PlayerInventory>();
            Health hp = other.GetComponentInParent<Health>();
            MeleeAttack melee = other.GetComponentInParent<MeleeAttack>();

            if (inv != null && ShopController.Instance != null)
            {
                ShopController.Instance.OpenShop(inv, hp, melee);
            }
        }
    }
}
