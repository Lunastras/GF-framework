using System.Collections;
using System.Collections.Generic;

public class Dequeue<T>
{

    LinkedList<T> list;
    //0 is front
    //n is back

    public Dequeue()
    {
        list = new LinkedList<T>();
        isEmpty = true;
    }

    public bool isEmpty { get; private set; }

    public T PeekBack()
    {
        if (isEmpty)
            return default;

        return list.Last.Value;
    }

    public T PeekFront()
    {
        if (isEmpty)
            return default;

        return list.First.Value;
    }

    public void EnqueueFront(T item)
    {
        list.AddFirst(item);
        isEmpty = false;
    }

    public void EnqueueBack(T item)
    {
        list.AddLast(item);
        isEmpty = false;
    }

    public void PopBack()
    {
        list.RemoveLast();
        isEmpty = list.Count == 0;
    }

    public void PopFront()
    {
        list.RemoveFirst();
        isEmpty = list.Count == 0;
    }

    public void Clear()
    {
        list.Clear();
        isEmpty = true;
    }


}

