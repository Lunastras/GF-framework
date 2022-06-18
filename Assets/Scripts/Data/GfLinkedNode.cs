public class GfLinkedNode<T>
{
    public T value { get; set; }
    public GfLinkedNode<T> next { get; set; } = null;
    public GfLinkedNode<T> previous { get; set; } = null;

    public GfLinkedList<T> list;

    public GfLinkedNode(T value)
    {
        this.value = value;
    }

    public void RemoveFromList()
    {

        if (next != null) //check if node is in front
        {
            next.previous = previous;
        }
        else
        {
            list.RemoveLast();
        }

        if (previous != null) //check if node is in back
        {
            previous.next = next;
        }
        else
        {
            list.RemoveFirst();
        }

        list.DecrementCount();
    }
}