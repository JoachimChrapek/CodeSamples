using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public enum VoxelOctreeIndex
{
    LeftBottomBack = 0,   //000
    LeftUpperBack = 2,    //010
    RightUpperBack = 6,   //110
    RightBottomBack = 4,  //100
    LeftBottomFront = 1,  //001
    LeftUpperFront = 3,   //011
    RightUpperFront = 7,  //111
    RightBottomFront = 5  //101
}



public class VoxelOctree<TType>
{
    public int3 Cords { get; private set; }

    public int Count { get; private set; }

    private VoxelOctreeNode<TType> node;

    public VoxelOctree(int3 cords, int size, int nodeMinSize)
    {
        Cords = cords;

        Count = 0;
        node = new VoxelOctreeNode<TType>(new int3(), size, nodeMinSize);
    }

    public void Add(TType obj, int3 position)
    {
        var added = node.Add(obj, position);

        if (!added)
        {
            return;
        }

        Count++;
    }

    public bool Get(int3 position, out TType obj)
    {
        return node.Get(position, out obj);
    }

    public bool Exists(int3 position)
    {
        return node.Exists(position);
    }

    public List<KeyValuePair<int3, TType>> GetAllWithCords()
    {
        var allObjects = new List<KeyValuePair<int3, TType>>(Count);
        node.GetAll(allObjects);
        return allObjects;
    }

    public bool RemoveAt(int3 position)
    {
        var removed = node.RemoveAt(position);

        if (removed)
        {
            Count--;
        }

        return removed;
    }

    public void Visualize()
    {
        node.VisualizeNode(Cords);
    }
}

public class VoxelOctreeNode<TType>
{
    private const int MAX_OBJECTS_IN_NODE = 8;

    private class OctreeObject
    {
        public int3 position;
        public TType obj;

        public OctreeObject(int3 position, TType obj)
        {
            this.position = position;
            this.obj = obj;
        }
    }

    public int3 Position { get; private set; }
    public int Size { get; private set; }

    private VoxelOctreeNode<TType>[] subNodes;
    private bool HasSubNodes => subNodes != null;

    private List<OctreeObject> objects = new List<OctreeObject>(8);
    private readonly int minSize;

    public VoxelOctreeNode(int3 position, int size, int minSize)
    {
        Position = position;
        Size = size;
        this.minSize = minSize;
    }

    public bool Add(TType obj, int3 objPosition)
    {
        if (!IsInside(objPosition))
        {
            return false;
        }

        if (!HasSubNodes)
        {
            if (objects.Count < MAX_OBJECTS_IN_NODE || Size / 2 < minSize)
            {
                foreach (var octreeObject in objects)
                {
                    if (octreeObject.position.Equals(objPosition))
                    {
                        Debug.LogError($"Object at position {objPosition.ToString()} already exists!");
                        return false;
                    }
                }

                OctreeObject newObject = new OctreeObject(objPosition, obj);
                objects.Add(newObject);
                return true;
            }

            int bestFit;
            Subdivide();
            if (subNodes == null)
            {
                Debug.LogError("SubNodes creation failed");
                return false;
            }

            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var currentObject = objects[i];
                bestFit = GetSubNodeIndex(currentObject.position);
                subNodes[bestFit].Add(currentObject.obj, currentObject.position);
                objects.Remove(currentObject);
            }
        }

        int bestFitChild = GetSubNodeIndex(objPosition);
        subNodes[bestFitChild].Add(obj, objPosition);

        return true;
    }

    public bool Get(int3 position, out TType obj, bool remove = false)
    {
        if (!IsInside(position))
        {
            obj = default;
            return false;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].position.Equals(position))
            {
                obj = objects[i].obj;

                if (remove)
                    objects.RemoveAt(i);

                return true;
            }
        }

        if (subNodes != null)
        {
            var bestFit = GetSubNodeIndex(position);
            return subNodes[bestFit].Get(position, out obj, remove);

        }

        obj = default;
        return false;
    }

    public bool Exists(int3 position)
    {
        if (!IsInside(position))
        {
            return false;
        }

        foreach (var obj in objects)
        {
            if (obj.position.Equals(position))
            {
                return true;
            }
        }

        if (subNodes != null)
        {
            var bestFit = GetSubNodeIndex(position);
            return subNodes[bestFit].Exists(position);
        }

        return false;
    }

    public void GetAll(List<KeyValuePair<int3, TType>> allObjects)
    {
        foreach (var obj in objects)
        {
            allObjects.Add(new KeyValuePair<int3, TType>(obj.position, obj.obj));
        }

        if (subNodes != null)
        {
            for (int i = 0; i < 8; i++)
            {
                subNodes[i].GetAll(allObjects);
            }
        }
    }

    public bool RemoveAt(int3 position)
    {
        var removed = false;

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].position.Equals(position))
            {
                removed = objects.Remove(objects[i]);
                break;
            }
        }

        if (!removed && subNodes != null)
        {
            var bestFit = GetSubNodeIndex(position);
            removed = subNodes[bestFit].RemoveAt(position);
        }

        if (removed && subNodes != null && ShouldMerge())
        {
            Merge();
        }

        return removed;
    }

    private bool ShouldMerge()
    {
        var total = objects.Count;

        if (subNodes != null)
        {
            foreach (var node in subNodes)
            {
                if (node.subNodes != null)
                    return false;

                total += node.objects.Count;
            }
        }

        return total <= MAX_OBJECTS_IN_NODE;
    }

    private void Merge()
    {
        for (int i = 0; i < 8; i++)
        {
            var currentSubNode = subNodes[i];
            for (int j = 0; j < currentSubNode.objects.Count; j++)
            {
                objects.Add(currentSubNode.objects[j]);
            }
        }

        subNodes = null;
    }
    
    private bool IsInside(int3 int3)
    {
        return int3.x >= Position.x && int3.x < (Position.x + Size) &&
               int3.y >= Position.y && int3.y < (Position.y + Size) &&
               int3.z >= Position.z && int3.z < (Position.z + Size);
    }

    private int GetSubNodeIndex(int3 objPosition)
    {
        var center = Position + new int3(1) * (Size / 2);
        return (objPosition.x >= center.x ? 4 : 0) +
               (objPosition.y >= center.y ? 2 : 0) +
               (objPosition.z >= center.z ? 1 : 0);
    }

    private void Subdivide()
    {
        subNodes = new VoxelOctreeNode<TType>[8];
        for (int i = 0; i < subNodes.Length; i++)
        {
            var point = Position;
            
            if ((i & 4) == 4)
            {
                point.x += Size / 2;
            }
            if ((i & 2) == 2)
            {
                point.y += Size / 2;
            }
            if ((i & 1) == 1)
            {
                point.z += Size / 2;
            }

            subNodes[i] = new VoxelOctreeNode<TType>(point, Size / 2, minSize);
        }
    }


    /// <summary>
    /// Draws gizmos to show all nodes. Usues SpaceSettings to determine size
    /// </summary>
    /// <param name="octreeCords"></param>
    public void VisualizeNode(int3 octreeCords)
    {
        Gizmos.color = Color.green;
        var octreePosition = SpaceSettings.BLOCK_SIZE * SpaceSettings.REGION_BLOCK_SIZE *
                             octreeCords.ToVector3();
        Gizmos.DrawWireCube(octreePosition + (Position + (new int3(1) * Size / 2)).ToVector3() * SpaceSettings.BLOCK_SIZE, Vector3.one * Size);

        Gizmos.color = Color.red;
        foreach (var octreeObject in objects)
        {
            
            Gizmos.DrawCube(octreePosition + octreeObject.position.ToVector3() + Vector3.one * (SpaceSettings.BLOCK_SIZE / 2), SpaceSettings.BLOCK_SIZE * Vector3.one);
        }

        if (subNodes != null)
        {
            foreach (var node in subNodes)
            {
                node.VisualizeNode(octreeCords);
            }
        }
    }
}