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
                ResourceType.Metal => 3,
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

        /// <summary>
        /// Remove up to <paramref name="amount"/> resources from the player's largest stack.
        /// Returns the actual removed count. Outputs the type that was reduced, or Wood if nothing
        /// could be dropped.
        /// </summary>
        public int DropLargestStack(int amount, out ResourceType type)
        {
            type = ResourceType.Wood;
            int max = this.m_Wood;
            if (this.m_Stone > max) { max = this.m_Stone; type = ResourceType.Stone; }
            if (this.m_Metal > max) { max = this.m_Metal; type = ResourceType.Metal; }
            if (max <= 0 || amount <= 0) return 0;

            int removed = Mathf.Min(amount, max);
            switch (type)
            {
                case ResourceType.Wood: this.m_Wood -= removed; break;
                case ResourceType.Stone: this.m_Stone -= removed; break;
                case ResourceType.Metal: this.m_Metal -= removed; break;
            }
            this.m_CurrentResources = Mathf.Max(0, this.m_CurrentResources - removed);
            this.Gold = Mathf.Max(0, this.Gold - removed * GetGoldValue(type));

            this.OnResourcesChanged?.Invoke(this.m_CurrentResources, this.m_MaxResourceCapacity);
            this.OnGoldChanged?.Invoke(this.Gold);
            return removed;
        }
    }
}
