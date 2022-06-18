/**
A linked list that can be concatenated with other lists
*
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfLinkedList<T>
{
    private GfLinkedNode<T> first = null;
    private GfLinkedNode<T> last = null;

    public int count { get; private set; } = 0;

    private GfLinkedNode<T> InternalAddLast(GfLinkedNode<T> valueNode)
    {
        valueNode.list = this;

        if (first == null)
        {
            first = last = valueNode;
        }
        else
        {
            last.next = valueNode;
            valueNode.previous = last;
            last = valueNode;
        }

        ++count;

        return valueNode;
    }

    public GfLinkedNode<T> AddLast(T value)
    {
        return InternalAddLast(new GfLinkedNode<T>(value));
    }

    private GfLinkedNode<T> InternalAddFirst(GfLinkedNode<T> valueNode)
    {
        valueNode.list = this;

        if (first == null)
        {
            first = last = valueNode;
        }
        else
        {
            valueNode.next = first;
            first.previous = valueNode;
            first = valueNode;
        }

        ++count;

        return valueNode;
    }

    public GfLinkedNode<T> AddFirst(T value)
    {
        return InternalAddFirst(new GfLinkedNode<T>(value));
    }

    public GfLinkedNode<T> Insert(T value, int index)
    {
        GfLinkedNode<T> valueNode = new GfLinkedNode<T>(value);
        if (index == 0)
        {
            return InternalAddFirst(valueNode);
        }
        else if (index >= count)
        {
            return InternalAddLast(valueNode);
        }

        GfLinkedNode<T> currentNode;

        if (index >= count / 2)
        {
            index = (count - 1) - index;
            currentNode = last;
            while (index > 0)
            {
                currentNode = currentNode.previous;
                --index;
            }
        }
        else
        {
            currentNode = first;
            while (index > 0)
            {
                currentNode = currentNode.next;
                --index;
            }
        }

        valueNode.previous = currentNode.previous;
        valueNode.next = currentNode;

        return valueNode;
    }

    public void RemoveFirst()
    {
        if (first == last)
        {
            count = 0;
            first = last = null;
        }
        else
        {
            first = first.next;
            first.previous = null;
            count--;
        }
    }

    public void RemoveLast()
    {
        if (first == last)
        {
            count = 0;
            first = last = null;
        }
        else
        {
            last = last.previous;
            last.next = null;
            count--;
        }
    }

    public void Remove(T value)
    {
        GfLinkedNode<T> currentNode = Find(value);

        if (currentNode != null)
        {
            currentNode.next.previous = currentNode.previous;
            currentNode.previous.next = currentNode.next;
        }
    }

    public GfLinkedNode<T> Find(T value)
    {
        GfLinkedNode<T> currentNode = first;
        while (value.Equals(currentNode.value) && currentNode != null)
        {
            currentNode = currentNode.next;
        }

        return currentNode;
    }

    public void Concatenate(GfLinkedList<T> newList)
    {
        if (count == 0)
        {
            last = newList.last;
            first = newList.first;
        }
        else if (newList.count > 0)
        {
            last.next = newList.first;
            last = newList.last;
        }

        count += newList.count;
    }

    public void DecrementCount() {
        --count;
    }
}


