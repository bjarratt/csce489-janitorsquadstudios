using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using XNAnimation;
using XNAnimation.Controllers;
using XNAnimation.Effects;

namespace WorldTest
{
    struct NavMeshVertex
    {
        public Vector3 position;
        //public List<int> adjacentFaces;
    }

    public class NavMeshNode
    {
        private int index;
        private bool obstacle;
        public const int NUM_VERTICES = 4;

        private Vector3[] vertices = {Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero};
        private Vector3 centroid;
        public List<int> adjacent_polygons;

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public bool Obstacle
        {
            get { return obstacle; }
            set { obstacle = value; }
        }

        public Vector3 V0
        {
            get { return vertices[0]; }
            set { vertices[0] = value; }
        }

        public Vector3 V1
        {
            get { return vertices[1]; }
            set { vertices[1] = value; }
        }

        public Vector3 V2
        {
            get { return vertices[2]; }
            set { vertices[2] = value; }
        }

        public Vector3 V3
        {
            get { return vertices[3]; }
            set { vertices[3] = value; }
        }

        public Vector3 Centroid
        {
            get { return centroid; }
            set { centroid = value; }
        }

        public Vector3 GetVertex(int index)
        {
            return vertices[index];
        }

        public void SetVertex(int index, Vector3 value)
        {
            if (index >= 0 && index < NUM_VERTICES)
            {
                vertices[index] = value;
            }
        }
    }

    public class Path<NavMeshNode> : IEnumerable<NavMeshNode>
    {
        public NavMeshNode LastStep { get; private set; }
        public Path<NavMeshNode> PreviousSteps { get; private set; }
        public double TotalCost { get; private set; }

        private Path(NavMeshNode lastStep, Path<NavMeshNode> previousSteps, double totalCost)
        {
            LastStep = lastStep;
            PreviousSteps = previousSteps;
            TotalCost = totalCost;
        }

        public Path(NavMeshNode start) : this(start, null, 0)
        {
        }

        public Path<NavMeshNode> AddStep(NavMeshNode step, double stepCost)
        {
            return new Path<NavMeshNode>(step, this, TotalCost + stepCost);
        }

        public IEnumerator<NavMeshNode> GetEnumerator()
        {
            for (Path<NavMeshNode> p = this; p != null; p = p.PreviousSteps)
                yield return p.LastStep;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class PriorityQueue<P, V>
    {
        private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();

        public void Enqueue(P priority, V value)
        {
            Queue<V> q;
            if (!list.TryGetValue(priority, out q))
            {
                q = new Queue<V>();
                list.Add(priority, q);
            }
            q.Enqueue(value);
        }

        public V Dequeue()
        {
            // will throw if there isn’t any first element!
            var pair = list.First();
            var v = pair.Value.Dequeue();
            if (pair.Value.Count == 0) // nothing left of the top priority.
                list.Remove(pair.Key);
            return v;
        }

        public bool IsEmpty
        {
            get { return !list.Any(); }
        }
    }
}
