using UnityEngine;
using System;
using Unity.Properties;

namespace ArenaCraft
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int m_MaxResourceCapacity = 100;
        
        [CreateProperty]
        public int Gold { get; private set; }
        
        [CreateProperty]
        public float ResourceFillAmount => (float)this.m_CurrentResources / this.m_MaxResourceCapacity;

        [CreateProperty]
        public int CurrentResources => this.m_CurrentResources;

        [CreateProperty]
        public int MaxResources => this.m_MaxResourceCapacity;

        private int m_CurrentResources;
        
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnResourcesChanged;
        public event Action OnCapacityReached;

        public void AddResource(ResourceType type, int amount)
        {
            if (this.m_CurrentResources >= this.m_MaxResourceCapacity) return;

            int actualAdded = Mathf.Min(amount, this.m_MaxResourceCapacity - this.m_CurrentResources);
            this.m_CurrentResources += actualAdded;

            int goldValue = GetGoldValue(type);
            this.Gold += actualAdded * goldValue;

            this.OnResourcesChanged?.Invoke(this.m_CurrentResources, this.m_MaxResourceCapacity);
            this.OnGoldChanged?.Invoke(this.Gold);

            if (this.m_CurrentResources >= this.m_MaxResourceCapacity)
            {
                this.OnCapacityReached?.Invoke();
            }
        }

        private int GetGoldValue(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => 1,
                ResourceType.Stone => 2,
                ResourceType.Metal => 5,
                _ => 1,
            };
        }

        public bool SpendGold(int amount)
        {
            if (this.Gold >= amount)
            {
                this.Gold -= amount;
                this.OnGoldChanged?.Invoke(this.Gold);
                return true;
            }
            return false;
        }
    }
}
