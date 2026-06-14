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
        public float ResourceFillAmount => this.m_MaxResourceCapacity > 0
            ? (float)this.m_CurrentResources / this.m_MaxResourceCapacity
            : 0f;

        [CreateProperty]
        public int CurrentResources => this.m_CurrentResources;

        [CreateProperty]
        public int MaxResources => this.m_MaxResourceCapacity;

        public bool IsFull => this.m_CurrentResources >= this.m_MaxResourceCapacity;

        private int m_CurrentResources;
        private int m_Wood;
        private int m_Stone;
        private int m_Metal;
        
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnResourcesChanged;
        public event Action<ResourceType, int, int> OnResourceCollected;
        public event Action OnCapacityReached;

        public int AddResource(ResourceType type, int amount)
        {
            if (amount <= 0 || this.m_MaxResourceCapacity <= 0 || this.IsFull) return 0;

            int actualAdded = Mathf.Min(amount, this.m_MaxResourceCapacity - this.m_CurrentResources);
            this.m_CurrentResources += actualAdded;

            int goldValue = GetGoldValue(type);
            int goldEarned = actualAdded * goldValue;
            this.Gold += goldEarned;
            AddToTypeTotal(type, actualAdded);

            this.OnResourcesChanged?.Invoke(this.m_CurrentResources, this.m_MaxResourceCapacity);
            this.OnGoldChanged?.Invoke(this.Gold);
            this.OnResourceCollected?.Invoke(type, actualAdded, goldEarned);

            if (this.IsFull)
            {
                this.OnCapacityReached?.Invoke();
            }

            return actualAdded;
        }

        public int GetResourceCount(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => this.m_Wood,
                ResourceType.Stone => this.m_Stone,
                ResourceType.Metal => this.m_Metal,
                _ => 0,
            };
        }

        private void AddToTypeTotal(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    this.m_Wood += amount;
                    break;
                case ResourceType.Stone:
                    this.m_Stone += amount;
                    break;
                case ResourceType.Metal:
                    this.m_Metal += amount;
                    break;
            }
        }

        public static int GetGoldValue(ResourceType type)
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
            if (amount > 0 && this.Gold >= amount)
            {
                this.Gold -= amount;
                this.OnGoldChanged?.Invoke(this.Gold);
                return true;
            }
            return false;
        }
    }
}
