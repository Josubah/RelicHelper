using System.Collections.Generic;

namespace RelicHelper;
internal class LimitedCapacityQueue<T> : Queue<T>
{
    public int Capacity { get; init; }

    public LimitedCapacityQueue(int capacity)
    {
        Capacity = capacity;
    }

    public new void Enqueue(T item)
    {
        int count = Count;
        if (count >= Capacity)
        {
            for (int i = count; i >= Capacity; i--)
            {
                Dequeue();
            }
        }

            base.Enqueue(item);
    }
}
